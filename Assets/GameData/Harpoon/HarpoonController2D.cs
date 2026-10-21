using UnityEngine;
using UnityEngine.EventSystems;
using Game.Combat;
using Game.InputSupport;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items.Durability;
using Game.World;
using Game.World.Lighting;
using Game.Entities;

namespace Game.Harpoon
{
    /// <summary>
    /// Harpoon Kind weapon. WeaponController routes Attack to TryFire().
    /// Swing movement is resolved by PlayerCollision.Move in FixedUpdate.
    /// The standard PlayerController stays enabled but skips its movement while attached.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(61000)]
    public sealed class HarpoonController2D : MonoBehaviour
    {
        private enum HookState { Idle, Flying, Attached }

        [Header("References (auto-found)")]
        [SerializeField] private PlayerInventory inventory;
        private HeldItemVisualController heldVisual;
        [SerializeField] private PlayerController playerMotor;
        [SerializeField] private PlayerCollision playerCollision;
        [SerializeField] private PlayerWeaponRigController weaponRig;
        [SerializeField] private PlayerAnimationController playerAnimation;
        [SerializeField] private HarpoonSurfaceProbe2D surfaceProbe;
        [SerializeField] private Camera playerCamera;

        [Header("Rope visual")]
        [SerializeField] private Color ropeColor = new Color(0.67f, 0.37f, 0.26f, 1f);
        [SerializeField] private Color hookColor = new Color(0.92f, 0.12f, 0.17f, 1f);
        [SerializeField, Range(0.02f, 0.18f)] private float ropeWidth = 0.08f;
        [SerializeField] private int ropeSortingOrder = 130;
        [Tooltip("Optional pixel-art hook sprite; by default loaded from Resources/Harpoon/harpoon_hook.png")]
        [SerializeField] private Sprite hookSpriteOverride;
        [SerializeField] private float ropeLightRefreshInterval = .12f;
        // Unity Gradient supports a maximum of 8 color keys. Sampling remains
        // evenly spaced along the line, including the anchor/hook endpoint.
        private const int RopeLightSamples = 8;
        private readonly GradientColorKey[] ropeLightKeys = new GradientColorKey[RopeLightSamples];
        private readonly GradientAlphaKey[] ropeAlphaKeys =
            { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
        private readonly Gradient ropeLightingGradient = new Gradient();
        private Color hookLighting = Color.white;
        private float nextRopeLightUpdate;
        private bool ownsHookSprite;

        private HookState state;
        private WeaponMetadata metadata;
        private ItemStack activeStack;
        private bool removeBrokenAfterUse;
        private Vector2 position;
        private Vector2 velocity;
        private Vector2 launchOrigin;
        private Vector2 anchor;
        private UnityEngine.Vector2Int anchorCell;
        private EntityActor anchorEntity;
        private Vector2 anchorEntityOffset;
        private float ropeLength;
        private float traveled;
        private float flightTime;
        private float nextLaunchTime;
        private LineRenderer rope;
        private SpriteRenderer hook;
        private Material ropeMaterial;
        private Texture2D hookTexture;
        private Sprite hookSprite;
        private bool facingOverrideActive;

        public bool IsAttached { get { return state == HookState.Attached; } }
        public bool IsFlying { get { return state == HookState.Flying; } }
        public bool IsRopeVisible { get { return state != HookState.Idle; } }
        public Vector2 AnchorPoint { get { return anchor; } }
        public float RopeLength { get { return ropeLength; } }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            heldVisual = GetComponent<HeldItemVisualController>();
            if (playerMotor == null) playerMotor = GetComponent<PlayerController>();
            if (playerCollision == null) playerCollision = GetComponent<PlayerCollision>();
            if (weaponRig == null) weaponRig = GetComponent<PlayerWeaponRigController>();
            if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimationController>();
            if (surfaceProbe == null) surfaceProbe = GetComponent<HarpoonSurfaceProbe2D>();
            if (surfaceProbe == null) surfaceProbe = gameObject.AddComponent<HarpoonSurfaceProbe2D>();
            if (playerCamera == null) playerCamera = Camera.main;
            CreateVisuals();
        }

        private void OnDisable() { Cancel(); }

        private void OnDestroy()
        {
            if (rope != null) Destroy(rope.gameObject);
            if (hook != null) Destroy(hook.gameObject);
            if (ropeMaterial != null) Destroy(ropeMaterial);
            if (ownsHookSprite && hookSprite != null) Destroy(hookSprite);
            if (hookTexture != null) Destroy(hookTexture);
        }

        public bool TryFire()
        {
            if (state != HookState.Idle || Time.time < nextLaunchTime || Time.timeScale < 0.001f)
                return false;
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return false;
            if (Game.UI.Settings.SettingsTabsController.IsOpen) return false;
            if (!GameInput.UsingGamepadAim && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject()) return false;

            if (inventory == null || playerCollision == null || surfaceProbe == null ||
                !surfaceProbe.IsReady || playerMotor == null) return false;

            string id = inventory.GetSelectedItemId();
            WeaponMetadata selected;
            if (!WeaponMetadataRegistry.TryGet(id, out selected) || selected == null ||
                selected.GetKind() != WeaponKind.Harpoon) return false;

            ItemStack stack = inventory.GetSelectedStack();
            if (stack == null || stack.IsEmpty) return false;
            int useCost = Mathf.Max(1, selected.HarpoonDurabilityCost);
            if (!DurabilitySystem.HasDurability(stack) ||
                DurabilitySystem.GetCurrentDurability(stack) < useCost) return false;

            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return false;
            Vector2 from = GetHandWorld();
            Vector2 target = GameInput.UsingGamepadAim ? GameInput.WorldAimPosition : ScreenAimWorld();
            Vector2 direction = target - from;
            if (direction.sqrMagnitude < 0.0001f) return false;

            metadata = selected;
            direction = direction.normalized;
            direction = (direction + Vector2.up * Mathf.Clamp(metadata.HarpoonArcLift, 0f, 0.8f)).normalized;

            state = HookState.Flying;
            launchOrigin = from;
            position = from;
            velocity = direction * Mathf.Max(1f, metadata.HarpoonThrowSpeed);
            flightTime = 0f;
            traveled = 0f;
            activeStack = stack;
            removeBrokenAfterUse = DurabilitySystem.Damage(stack, useCost);
            inventory.NotifyExternalChange();
            Game.Audio.TelderAudioSystem.PlayAt("harpoon.fire", transform.position, GetInstanceID());
            nextLaunchTime = Time.time + 0.25f;
            if (heldVisual == null) heldVisual = GetComponent<HeldItemVisualController>();
            if (heldVisual != null)
                heldVisual.SetHarpoonDeployed(true, metadata.HarpoonDeployedTexture);
            return true;
        }

        private Vector2 ScreenAimWorld()
        {
            Vector3 pointer = GameInput.PointerPosition;
            pointer.z = Mathf.Abs(playerCamera.transform.position.z - transform.position.z);
            return playerCamera.ScreenToWorldPoint(pointer);
        }

        private void Update()
        {
            if (state == HookState.Idle) return;
            // Changing the hotbar item cancels the harpoon; the spent final shot
            // is allowed to finish even when its durability reached zero.
            if (inventory == null || inventory.GetSelectedStack() != activeStack ||
                inventory.GetSelectedItemId() != activeStack.ItemId)
            {
                Cancel();
                return;
            }

            if (state == HookState.Attached && GameInput.Pressed(GameAction.Jump))
            {
                // PlayerController.Update also routes Jump here; repeated calls
                // cannot detach twice because DetachWithJump checks the state.
                DetachWithJump();
                return;
            }
            UpdateFacingOverride();
        }

        private void FixedUpdate()
        {
            if (state == HookState.Idle) return;
            if (Time.timeScale < 0.001f ||
                (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                Game.UI.Settings.SettingsTabsController.IsOpen) return;

            if (state == HookState.Flying) StepFlight(Time.fixedDeltaTime);
            else if (state == HookState.Attached) StepSwing(Time.fixedDeltaTime);
        }

        private void StepFlight(float dt)
        {
            // Moving away while the hook is airborne cannot extend the rope
            // indefinitely; the projectile vanishes once out of player range.
            if (Vector2.Distance(transform.position, position) >
                Mathf.Max(1f, metadata.HarpoonMaxRange))
            {
                Cancel();
                return;
            }
            Vector2 next = position + velocity * dt + Vector2.down *
                           (0.5f * metadata.HarpoonThrowGravity * dt * dt);
            velocity += Vector2.down * metadata.HarpoonThrowGravity * dt;
            flightTime += dt;
            traveled += (next - position).magnitude;

            Vector2 impact;
            UnityEngine.Vector2Int cell;
            float maxDistance = Mathf.Max(1f, metadata.HarpoonMaxRange);
            // Never attach to a tile beyond the maximum hook range.
            if (Vector2.Distance(launchOrigin, next) > maxDistance)
                next = launchOrigin + (next - launchOrigin).normalized * maxDistance;
            bool hitBlock = surfaceProbe.TryTrace(position, next, out impact, out cell);
            bool hitCreature = TryTraceCreature(position, next, out EntityActor creature, out Vector2 creatureImpact);
            // Prefer whichever valid collision happened first along the hook segment.
            if (hitCreature && hitBlock &&
                (creatureImpact - position).sqrMagnitude >= (impact - position).sqrMagnitude)
                hitCreature = false;
            if (hitBlock || hitCreature)
            {
                anchorEntity = hitCreature ? creature : null;
                anchor = hitCreature ? creatureImpact : impact;
                anchorEntityOffset = hitCreature ? anchor - (Vector2)creature.transform.position : Vector2.zero;
                anchorCell = cell;
                position = anchor;
                float minimum = Mathf.Max(0.8f, metadata.HarpoonMinRopeLength);
                ropeLength = Mathf.Clamp(Vector2.Distance(anchor, transform.position),
                    minimum, Mathf.Max(minimum, metadata.HarpoonMaxRange));
                state = HookState.Attached;
                velocity = new Vector2(playerMotor.GetHorizontalInput() * 5f,
                                       playerMotor.GetVerticalVelocity());
                playerMotor.OnHarpoonAttached();
                return;
            }
            position = next;
            if (flightTime >= metadata.HarpoonMaxFlightTime ||
                traveled >= metadata.HarpoonMaxRange ||
                Vector2.Distance(launchOrigin, position) >= metadata.HarpoonMaxRange)
            {
                Cancel();
            }
        }

        private static bool TryTraceCreature(Vector2 from, Vector2 to,
            out EntityActor entity, out Vector2 impact)
        {
            entity = null;
            impact = to;
            float closestT = 2f;
            var actors = EntityActor.ActiveActors;
            Vector2 d = to - from;
            for (int i = 0; i < actors.Count; i++)
            {
                EntityActor actor = actors[i];
                if (actor == null || !actor.IsDamageTargetActive) continue;
                Rect bounds = actor.GetDamageBounds();
                float tNear = 0f, tFar = 1f;
                bool intersects = true;
                for (int axis = 0; axis < 2; axis++)
                {
                    float origin = axis == 0 ? from.x : from.y;
                    float dir = axis == 0 ? d.x : d.y;
                    float min = axis == 0 ? bounds.xMin : bounds.yMin;
                    float max = axis == 0 ? bounds.xMax : bounds.yMax;
                    if (Mathf.Abs(dir) < 0.000001f)
                    {
                        if (origin < min || origin > max) { intersects = false; break; }
                        continue;
                    }
                    float a = (min - origin) / dir, b = (max - origin) / dir;
                    if (a > b) { float tmp = a; a = b; b = tmp; }
                    tNear = Mathf.Max(tNear, a);
                    tFar = Mathf.Min(tFar, b);
                    if (tNear > tFar) { intersects = false; break; }
                }
                if (!intersects || tNear >= closestT) continue;
                closestT = tNear;
                entity = actor;
                impact = from + d * tNear;
            }
            return entity != null;
        }

        private void StepSwing(float dt)
        {
            if (anchorEntity != null)
            {
                if (!anchorEntity.IsDamageTargetActive)
                {
                    Release(false);
                    return;
                }
                anchor = (Vector2)anchorEntity.transform.position + anchorEntityOffset;
            }
            else if (!surfaceProbe.IsSolid(anchorCell))
            {
                Release(false);
                return;
            }
            if (Vector2.Distance(transform.position, anchor) > Mathf.Max(1f, metadata.HarpoonMaxRange) * 1.35f)
            {
                Release(false);
                return;
            }

            float reel = Mathf.Clamp(
                (GameInput.Held(GameAction.RopeShorten) ? 1f : 0f) -
                (GameInput.Held(GameAction.RopeLengthen) ? 1f : 0f) +
                GameInput.Vertical, -1f, 1f);
            // W shortens the line; S lengthens it.
            float minLength = Mathf.Max(0.8f, metadata.HarpoonMinRopeLength);
            float maxLength = Mathf.Max(minLength, metadata.HarpoonMaxRange);
            if (Mathf.Abs(reel) > 0.1f)
                Game.Audio.TelderAudioSystem.PlayAt(reel > 0f ? "harpoon.reel.in" : "harpoon.reel.out",
                    transform.position, GetInstanceID());
            ropeLength = Mathf.Clamp(ropeLength - reel * metadata.HarpoonWinchSpeed * dt,
                minLength, maxLength);

            Vector2 current = transform.position;
            Vector2 radius = current - anchor;
            float distance = radius.magnitude;
            Vector2 radial = distance > 0.001f ? radius / distance : Vector2.down;
            float g = Mathf.Max(0f, metadata.HarpoonSwingGravity);
            velocity += Vector2.down * (g * dt);

            // Project left/right input on the tangent, preserving pendulum momentum.
            Vector2 inputForce = Vector2.right * GameInput.Horizontal;
            Vector2 tangentialForce = inputForce - radial * Vector2.Dot(inputForce, radial);
            velocity += tangentialForce * metadata.HarpoonSwingAcceleration * dt;
            velocity *= Mathf.Clamp01(1f - metadata.HarpoonSwingDrag * dt);
            velocity = Vector2.ClampMagnitude(velocity, metadata.HarpoonMaxSwingSpeed);

            // Rope goes slack toward the anchor, but never stretches past its length.
            if (distance >= ropeLength - 0.02f)
            {
                float movingOutward = Vector2.Dot(velocity, radial);
                if (movingOutward > 0f) velocity -= radial * movingOutward;
            }

            Vector2 target = current + velocity * dt;
            Vector2 towardTarget = target - anchor;
            if (towardTarget.magnitude > ropeLength)
                target = anchor + towardTarget.normalized * ropeLength;

            Vector2 before = transform.position;
            playerCollision.Move(target - before);
            Vector2 actual = (Vector2)transform.position - before;
            if (dt > 0.00001f) velocity = actual / dt;
        }

        public bool DetachWithJump()
        {
            if (state != HookState.Attached) return false;
            Vector2 outward = (Vector2)transform.position - anchor;
            outward = outward.sqrMagnitude < 0.0001f ? Vector2.up : outward.normalized;
            Vector2 transferred = velocity + Vector2.up * metadata.HarpoonJumpUpBoost +
                                  Vector2.right * outward.x * metadata.HarpoonJumpOutBoost;
            ReleaseWithVelocity(transferred);
            return true;
        }

        private void Release(bool jump)
        {
            if (state == HookState.Attached)
            {
                if (jump) DetachWithJump();
                else ReleaseWithVelocity(velocity);
            }
            else Cancel();
        }

        private void ReleaseWithVelocity(Vector2 outgoing)
        {
            velocity = outgoing;
            Cancel();
        }

        public void Cancel()
        {
            if (state == HookState.Idle) return;
            if (state == HookState.Attached && playerMotor != null)
                playerMotor.OnHarpoonReleased(velocity);
            state = HookState.Idle;
            anchorEntity = null;
            anchorEntityOffset = Vector2.zero;
            if (heldVisual != null) heldVisual.SetHarpoonDeployed(false);
            if (facingOverrideActive && playerAnimation != null)
                playerAnimation.ClearExternalFacingOverride();
            facingOverrideActive = false;
            // The last shot may be used to completion; only then discard the broken item.
            if (removeBrokenAfterUse && activeStack != null && !activeStack.IsEmpty &&
                DurabilitySystem.IsBroken(activeStack))
            {
                activeStack.Clear();
                if (inventory != null) inventory.NotifyExternalChange();
            }
            activeStack = null;
            removeBrokenAfterUse = false;
            metadata = null;
            if (rope != null) rope.enabled = false;
            if (hook != null) hook.enabled = false;
        }

        private void UpdateFacingOverride()
        {
            if (playerAnimation == null) return;
            Vector2 end = state == HookState.Attached ? anchor : position;
            float deltaX = end.x - transform.position.x;
            bool right = Mathf.Abs(deltaX) > 0.12f ? deltaX > 0f : playerAnimation.IsFacingRight();
            playerAnimation.SetExternalFacingOverride(true, right);
            facingOverrideActive = true;
        }

        private Vector2 GetHandWorld()
        {
            if (weaponRig != null)
            {
                Transform hand = weaponRig.GetRuntimeHandPoint();
                if (hand != null) return hand.position;
            }
            return (Vector2)transform.position + Vector2.up * 0.3f;
        }

        private void LateUpdate()
        {
            bool visible = state != HookState.Idle;
            if (rope != null) rope.enabled = visible;
            if (hook != null) hook.enabled = visible;
            if (!visible) return;

            Vector2 target = state == HookState.Attached ? anchor : position;
            if (weaponRig != null) weaponRig.AimHarpoonRope(target);
            Vector2 from = GetHandWorld();
            UpdateRopeLighting(from, target);
            if (rope != null)
            {
                rope.SetPosition(0, new Vector3(from.x, from.y, 0f));
                rope.SetPosition(1, new Vector3(target.x, target.y, 0f));
            }
            if (hook != null)
            {
                hook.color = hookLighting;
                hook.transform.position = new Vector3(target.x, target.y, 0f);
                Vector2 line = target - from;
                if (line.sqrMagnitude > 0.0001f)
                    hook.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg);
            }
        }

        private void UpdateRopeLighting(Vector2 from, Vector2 target)
        {
            if (Time.unscaledTime < nextRopeLightUpdate || rope == null) return;
            nextRopeLightUpdate = Time.unscaledTime + Mathf.Max(.05f, ropeLightRefreshInterval);
            WorldManager manager = WorldManager.Instance;
            Game.World.World world = manager != null ? manager.GetWorld() : null;
            if (world == null) return;
            for (int i = 0; i < RopeLightSamples; i++)
            {
                float t = i / (float)(RopeLightSamples - 1);
                Vector2 p = Vector2.Lerp(from, target, t);
                Color light = WorldVisualLightSampler.Sample(world,
                    Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                Color tint = new Color(ropeColor.r * light.r,
                    ropeColor.g * light.g, ropeColor.b * light.b, 1f);
                ropeLightKeys[i] = new GradientColorKey(tint, t);
                if (i == RopeLightSamples - 1)
                    hookLighting = light;
            }
            ropeLightingGradient.SetKeys(ropeLightKeys, ropeAlphaKeys);
            rope.colorGradient = ropeLightingGradient;
        }

        private void CreateVisuals()
        {
            var ropeGO = new GameObject("HarpoonRope_Runtime");
            rope = ropeGO.AddComponent<LineRenderer>();
            rope.positionCount = 2;
            rope.useWorldSpace = true;
            rope.startWidth = rope.endWidth = ropeWidth;
            rope.startColor = rope.endColor = ropeColor;
            rope.numCapVertices = rope.numCornerVertices = 0;
            rope.sortingOrder = ropeSortingOrder;
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader != null)
            {
                ropeMaterial = new Material(spriteShader);
                rope.sharedMaterial = ropeMaterial;
            }
            rope.enabled = false;

            hookSprite = hookSpriteOverride != null ? hookSpriteOverride :
                UnityEngine.Resources.Load<Sprite>("Harpoon/harpoon_hook");
            if (hookSprite == null)
            {
                // Compatible fallback if the resource has not been imported yet.
                ownsHookSprite = true;
                hookTexture = new Texture2D(13, 13, TextureFormat.RGBA32, false);
                hookTexture.filterMode = FilterMode.Point;
                hookTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < 13; y++)
                for (int x = 0; x < 13; x++)
                {
                    float d = Mathf.Sqrt((x - 6f) * (x - 6f) + (y - 6f) * (y - 6f));
                    bool ring = d >= 4f && d < 5.9f;
                    hookTexture.SetPixel(x, y, ring ? hookColor : Color.clear);
                }
                hookTexture.Apply(false, false);
                hookSprite = Sprite.Create(hookTexture, new Rect(0, 0, 13, 13),
                    new Vector2(.5f, .5f), 26f, 0, SpriteMeshType.FullRect);
            }
            GameObject hookGO = new GameObject("HarpoonHook_Runtime");
            hook = hookGO.AddComponent<SpriteRenderer>();
            hook.sprite = hookSprite;
            hook.sortingOrder = ropeSortingOrder + 1;
            hook.enabled = false;
        }
    }
}
