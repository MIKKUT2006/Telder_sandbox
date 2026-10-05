using UnityEngine;
using Game.World;
using Game.World.Collision;
using Game.World.Fluids;
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
    [Tooltip("Distance below the top of the player collider used as the breathing point.")]
    [SerializeField] private float headProbeInset = 0.10f;

    private PlayerCollision playerCollision;
    private WorldCollision worldCollision;
    private PlayerStatsComponent playerStats;

    private float verticalVelocity;
    private float horizontalInput;
    private bool jumpHeld;
    private bool inLiquid;
    private bool headSubmerged;
    private bool initialized;

    public bool IsInLiquid => inLiquid;
    public bool IsHeadSubmerged => headSubmerged;

    private void Start()
    {
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

        worldCollision = worldManager.GetWorldCollision();
        if (worldCollision == null)
        {
            Debug.LogError("PLAYER CONTROLLER: WorldCollision is null.");
            return;
        }

        playerCollision.Initialize(worldCollision);
        playerCollision.ForceGroundCheck();
        initialized = true;

        Debug.Log("PLAYER CONTROLLER: INITIALIZED.");
    }

    private void Update()
    {
        if (!initialized)
            return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpHeld = Input.GetKey(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        UpdateLiquidContact();

        float speedMultiplier = inLiquid ? swimHorizontalMultiplier : 1f;
        float horizontalMovement =
            horizontalInput * moveSpeed * speedMultiplier * Time.fixedDeltaTime;

        ApplyGravity();

        Vector2 movement = new Vector2(
            horizontalMovement,
            verticalVelocity * Time.fixedDeltaTime
        );

        playerCollision.Move(movement);

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

    private void ApplyGravity()
    {
        if (inLiquid)
        {
            float targetVerticalSpeed = jumpHeld
                ? swimUpSpeed
                : -swimSinkSpeed;

            verticalVelocity = Mathf.MoveTowards(
                verticalVelocity,
                targetVerticalSpeed,
                Mathf.Max(0.1f, swimAcceleration) * Time.fixedDeltaTime
            );

            verticalVelocity = Mathf.Max(verticalVelocity, -maxLiquidFallSpeed);
            return;
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
        if (inLiquid)
        {
            verticalVelocity = Mathf.Max(verticalVelocity, swimUpSpeed * 0.65f);
            return;
        }

        if (!playerCollision.IsGrounded)
            return;

        verticalVelocity = jumpForce;
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
