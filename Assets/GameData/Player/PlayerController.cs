using Game.InputSupport;
using UnityEngine;
using Game.World;
using Game.World.Collision;
using Game.World.Fluids;
using Game.GameplaySystems.Interactions;
using PlayerStatsComponent = Game.PlayerStats.PlayerStats;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Swimming")]
    [Tooltip("Horizontal speed multiplier while the body is in liquid.")]
    [SerializeField] private float swimHorizontalMultiplier = 0.72f;
    [Tooltip("Upward speed approached while Jump is held in liquid.")]
    [SerializeField] private float swimUpSpeed = 4.2f;
    [Tooltip("Slow sinking speed when Jump is not held.")]
    [SerializeField] private float swimSinkSpeed = 1.15f;
    [SerializeField] private float swimAcceleration = 13f;
    [SerializeField] private float maxLiquidFallSpeed = 3.5f;
    [Tooltip("Exit speed when the head is clear and jump is held. Allows climbing onto adjacent blocks.")]
    [SerializeField] private float swimExitJumpSpeed = 9f;
    [SerializeField] private float swimExitGrace = 0.20f;
    private float swimExitGraceUntil;

    [Tooltip("Distance below the top of the player collider used as the breathing point.")]
    [SerializeField] private float headProbeInset = 0.10f;

    [Header("Ladder")]
    [Tooltip("Upward speed while Space is held on a ladder.")]
    [SerializeField] private float ladderClimbSpeed = 4f;
    [Tooltip("Downward sliding speed after Space is released.")]
    [SerializeField] private float ladderSlideSpeed = 6f;
    [SerializeField] private float ladderAcceleration = 12f;

    private PlayerCollision playerCollision;
    private WorldCollision worldCollision;
    private World world;
    private PlayerStatsComponent playerStats;

    private float verticalVelocity;
    private float horizontalInput;
    private bool jumpHeld;
    private bool inLiquid;
    private bool headSubmerged;
    private bool onLadder;
    private bool initialized;
    private Game.Harpoon.HarpoonController2D harpoon;
    private float grappleHorizontalMomentum;
    private float combatDashRemaining, combatDashSpeed, combatDashDirection;

    public bool TryStartCombatDash(float direction, float distance, float duration)
    {
        if (!initialized || playerCollision == null || (harpoon != null && harpoon.IsAttached)) return false;
        combatDashRemaining = Mathf.Clamp(distance, 0, 2);
        combatDashSpeed = combatDashRemaining / Mathf.Max(.06f, duration);
        combatDashDirection = direction >= 0 ? 1f : -1f;
        grappleHorizontalMomentum = 0;
        return combatDashRemaining > 0;
    }

    public void CancelCombatDash() { combatDashRemaining = combatDashSpeed = 0; }

    private bool DashAreaLoaded(float displacement)
    {
        if (world == null) return false;
        Vector2 size = playerCollision.GetColliderSize();
        int minX = Mathf.FloorToInt((transform.position.x + Mathf.Min(0, displacement) - size.x * .5f) / Chunk.SizeX);
        int maxX = Mathf.FloorToInt((transform.position.x + Mathf.Max(0, displacement) + size.x * .5f) / Chunk.SizeX);
        int minY = Mathf.FloorToInt((transform.position.y - size.y * .5f) / Chunk.SizeY);
        int maxY = Mathf.FloorToInt((transform.position.y + size.y * .5f + .6f) / Chunk.SizeY);
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                if (world.GetChunk(x, y) == null) return false;
        return true;
    }

    public bool IsInLiquid => inLiquid;
    public bool IsHeadSubmerged => headSubmerged;
    public bool IsOnLadder => onLadder;
    [SerializeField] private float ladderFastDescendSpeed = 14f;

    private bool fastLadderDescending;
    private bool mobileDescendHeld;

    // Called only after WorldManager has streamed in the spawn surface.
    // The movement motor is custom, so resetting Rigidbody2D is not enough.
    public void ResetForSurfaceSpawn()
    {
        CancelCombatDash();
        verticalVelocity = 0f;
        grappleHorizontalMomentum = 0f;
        horizontalInput = 0f;
        jumpHeld = false;
        inLiquid = false;
        headSubmerged = false;
        onLadder = false;
        swimExitGraceUntil = 0f;
        if (playerCollision != null) playerCollision.ForceGroundCheck();
    }

    private void Start()
    {
        if (GetComponent<Game.World.Effects.PlayerFootstepDust>() == null)
            gameObject.AddComponent<Game.World.Effects.PlayerFootstepDust>();
        if (GetComponent<PlayerWorldLighting>() == null)
            gameObject.AddComponent<PlayerWorldLighting>();
        if (GetComponent<Game.Audio.TelderPlayerMovementAudio>() == null)
            gameObject.AddComponent<Game.Audio.TelderPlayerMovementAudio>();
        playerCollision = GetComponent<PlayerCollision>();
        playerStats = GetComponent<PlayerStatsComponent>();

        if (playerCollision == null)
        {
            Debug.LogError("PLAYER CONTROLLER: PlayerCollision not found.");
            return;
        }

        WorldManager worldManager = WorldManager.Instance;
        if (worldManager == null)
        {
            Debug.LogError("PLAYER CONTROLLER: WorldManager is null.");
            return;
        }

        world = worldManager.GetWorld();
        if (world == null)
        {
            Debug.LogError("PLAYER CONTROLLER: World is null.");
            return;
        }

        worldCollision = worldManager.GetWorldCollision();
        if (worldCollision == null)
        {
            Debug.LogError("PLAYER CONTROLLER: WorldCollision is null.");
            return;
        }

        playerCollision.Initialize(worldCollision);
        playerCollision.ForceGroundCheck();
        initialized = true;

        // Enable harpoon on every spawned 2D player without scene edits.
        harpoon = GetComponent<Game.Harpoon.HarpoonController2D>();
        if (harpoon == null)
            harpoon = gameObject.AddComponent<Game.Harpoon.HarpoonController2D>();

        Debug.Log("PLAYER CONTROLLER: INITIALIZED.");
    }

    private void Update()
    {
        if (!initialized)
            return;
        if (WorldManager.Instance != null && !WorldManager.Instance.InitialSpawnReady)
        {
            horizontalInput = 0f;
            jumpHeld = false;
            return;
        }

        if (Time.timeScale < 0.001f || (Game.Inventory.UI.InventoryUI.Instance != null && Game.Inventory.UI.InventoryUI.Instance.IsOpen) ||
            Game.UI.Settings.SettingsTabsController.IsOpen)
        {
            horizontalInput = 0f;
            jumpHeld = false;
            return;
        }
        horizontalInput = GameInput.Horizontal;
        jumpHeld = GameInput.Held(GameAction.Jump);

        if (GameInput.Pressed(GameAction.Jump))
            Jump();


        bool touchDescend = GameInput.MobileEnabled && GameInput.Vertical < -0.7f;
        if (Input.GetKeyDown(KeyCode.S) || (touchDescend && !mobileDescendHeld))
        {
            UpdateLadderContact();

            if (onLadder && !inLiquid)
            {
                fastLadderDescending = true;
                verticalVelocity = -Mathf.Max(2f, ladderFastDescendSpeed);
            }
            else
            {
                fastLadderDescending = false;

                playerCollision.DropThroughPlatforms();

                verticalVelocity = Mathf.Min(
                    verticalVelocity,
                    -Mathf.Max(2f, ladderSlideSpeed)
                );
            }
        }

        if (Input.GetKeyUp(KeyCode.S) || (!touchDescend && mobileDescendHeld))
        {
            fastLadderDescending = false;
        }
        mobileDescendHeld = touchDescend;

    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;
        // No gravity/collision movement in chunks that haven't been created yet.
        if (WorldManager.Instance != null && !WorldManager.Instance.InitialSpawnReady)
        {
            verticalVelocity = 0f;
            return;
        }

        if (Game.UI.Settings.SettingsTabsController.IsOpen ||
            (Game.Inventory.UI.InventoryUI.Instance != null && Game.Inventory.UI.InventoryUI.Instance.IsOpen))
        { CancelCombatDash(); return; }
        bool wasInLiquid = inLiquid;
        UpdateLiquidContact();
        if (wasInLiquid && !inLiquid && jumpHeld)
            swimExitGraceUntil = Time.time + Mathf.Max(0f, swimExitGrace);
        UpdateLadderContact();

        if (!onLadder)
        {
            fastLadderDescending = false;
        }
        // HarpoonController drives movement through the same PlayerCollision.
        // Never run the ordinary motor while attached to a rope.
        if (harpoon != null && harpoon.IsAttached)
        {
            verticalVelocity = 0f;
            CancelCombatDash();
            return;
        }

        float speedMultiplier = (inLiquid ? swimHorizontalMultiplier : 1f) * Game.Gameplay.Effects.PlayerFoodEffects.Speed(this);
        float horizontalMovement =
            (horizontalInput * moveSpeed * speedMultiplier + grappleHorizontalMomentum) *
            Time.deltaTime;
        grappleHorizontalMomentum = Mathf.MoveTowards(grappleHorizontalMomentum, 0f,
            (Mathf.Abs(horizontalInput) > 0.05f ? 8f : 3f) * Time.deltaTime);

        bool dashing = combatDashRemaining > .0001f;
        float dashStep = 0;
        if (dashing)
        {
            dashStep = Mathf.Min(combatDashRemaining, combatDashSpeed * Time.fixedDeltaTime);
            horizontalMovement = combatDashDirection * dashStep;
            if (!DashAreaLoaded(horizontalMovement)) { CancelCombatDash(); horizontalMovement = 0; dashing = false; }
        }
        ApplyGravity();

        Vector2 movement = new Vector2(
            horizontalMovement,
            verticalVelocity * Time.deltaTime
        );

        float startX = transform.position.x;
        playerCollision.Move(movement);
        if (dashing)
        {
            float moved = Mathf.Abs(transform.position.x - startX);
            combatDashRemaining = Mathf.Max(0, combatDashRemaining - dashStep);
            if (moved + .01f < dashStep) CancelCombatDash();
        }

        if (!inLiquid && playerCollision.IsGrounded && verticalVelocity < 0f)
            verticalVelocity = 0f;
    }

    private void UpdateLiquidContact()
    {
        Vector2 size = playerCollision.GetColliderSize();
        float halfHeight = size.y * 0.5f;

        // Body probes: center and a point near the legs. This allows the player to
        // begin swimming once a meaningful part of the body is underwater while
        // ignoring tiny 1/8-flow layers touching only the feet.
        Vector2 centerProbe = new Vector2(transform.position.x, transform.position.y);
        Vector2 lowerProbe = new Vector2(
            transform.position.x,
            transform.position.y - halfHeight * 0.48f
        );

        inLiquid =
            LiquidRuntime.IsPointSubmerged(centerProbe) ||
            LiquidRuntime.IsPointSubmerged(lowerProbe);

        Vector2 headProbe = new Vector2(
            transform.position.x,
            transform.position.y + halfHeight - Mathf.Max(0.02f, headProbeInset)
        );

        headSubmerged = LiquidRuntime.IsPointSubmerged(headProbe);

        if (playerStats != null)
            playerStats.SetHeadSubmerged(headSubmerged);
    }

    private void UpdateLadderContact()
    {
        if (playerCollision == null || world == null)
        {
            onLadder = false;
            return;
        }

        Vector2 size = playerCollision.GetColliderSize();
        float width = Mathf.Max(0.05f, size.x - 0.08f);
        float height = Mathf.Max(0.05f, size.y - 0.08f);

        Rect bounds = new Rect(
            transform.position.x - width * 0.5f,
            transform.position.y - height * 0.5f,
            width,
            height
        );

        onLadder =
            InteractiveBlockRuntime.IsTouchingLadder(
                world,
                bounds
            );
    }


    private void ApplyGravity()
    {
        if (onLadder && !inLiquid)
        {
            float targetVerticalSpeed;

            if (fastLadderDescending && Input.GetKey(KeyCode.S))
            {
                targetVerticalSpeed = -Mathf.Max(2f, ladderFastDescendSpeed);
            }
            else
            {
                fastLadderDescending = false;

                targetVerticalSpeed = jumpHeld
                    ? Mathf.Max(0.1f, ladderClimbSpeed)
                    : -Mathf.Max(0f, ladderSlideSpeed);
            }

            verticalVelocity = Mathf.MoveTowards(
                verticalVelocity,
                targetVerticalSpeed,
                Mathf.Max(0.1f, ladderAcceleration * 2f) * Time.fixedDeltaTime
            );

            return;
        }

        if (inLiquid)
        {
            // When the head emerges, keep upward momentum instead of clamping
            // it back to the slow submerged swim speed.
            float targetVerticalSpeed = jumpHeld
                ? (headSubmerged ? swimUpSpeed : Mathf.Max(swimUpSpeed, swimExitJumpSpeed))
                : -swimSinkSpeed;

            verticalVelocity = Mathf.MoveTowards(
                verticalVelocity,
                targetVerticalSpeed,
                Mathf.Max(0.1f, swimAcceleration) * Time.fixedDeltaTime
            );

            verticalVelocity = Mathf.Max(verticalVelocity, -maxLiquidFallSpeed);
            return;
        }

        if (jumpHeld && Time.time < swimExitGraceUntil)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, Mathf.Max(jumpForce, swimExitJumpSpeed));
            swimExitGraceUntil = 0f;
        }

        if (playerCollision.IsGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = 0f;
            return;
        }

        verticalVelocity -= gravity * Time.fixedDeltaTime;
        if (verticalVelocity < -maxFallSpeed)
            verticalVelocity = -maxFallSpeed;
    }

    private void Jump()
    {
        if (harpoon != null && harpoon.DetachWithJump())
            return;

        // FixedUpdate normally owns ladder contact, but check immediately on the
        // first Space press so climbing responds on the same rendered frame.
        if (!onLadder)
            UpdateLadderContact();

        if (onLadder && !inLiquid)
        {
            verticalVelocity = Mathf.Max(
                verticalVelocity,
                Mathf.Max(0.1f, ladderClimbSpeed) * 0.65f
            );
            return;
        }

        if (inLiquid)
        {
            verticalVelocity = Mathf.Max(verticalVelocity,
                headSubmerged ? swimUpSpeed * 0.65f : Mathf.Max(jumpForce, swimExitJumpSpeed));
            return;
        }

        if (!playerCollision.IsGrounded)
            return;

        verticalVelocity = jumpForce * Game.Gameplay.Effects.PlayerFoodEffects.Jump(this);
    }

    public void OnHarpoonAttached()
    {
        verticalVelocity = 0f;
        grappleHorizontalMomentum = 0f;
    }

    public void OnHarpoonReleased(Vector2 inheritedVelocity)
    {
        verticalVelocity = Mathf.Clamp(inheritedVelocity.y, -maxFallSpeed,
            Mathf.Max(jumpForce * 2f, 18f));
        // Ordinary input remains responsive, but momentum carries forward.
        grappleHorizontalMomentum = Mathf.Clamp(inheritedVelocity.x,
            -25f, 25f);
    }

    public void OnWorldBlockChanged()
    {
        if (!initialized || playerCollision == null)
            return;

        playerCollision.RefreshAfterWorldChange();

        if (!playerCollision.IsGrounded && verticalVelocity >= 0f)
            verticalVelocity = -0.01f;
    }

    public PlayerCollision GetPlayerCollision()
    {
        return playerCollision;
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }

    public float GetHorizontalInput()
    {
        return horizontalInput;
    }
}
