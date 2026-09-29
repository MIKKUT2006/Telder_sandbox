using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Game.Combat
{
    public readonly struct PlayerWeaponPoseRequest
    {
        public readonly WeaponMetadata Weapon;

        public readonly WeaponKind Kind;

        public readonly SwordAttackKind SwordAttack;

        public readonly Vector2 AimDirection;

        public readonly float Phase;


        public readonly bool FacingRight;


        public PlayerWeaponPoseRequest(
            WeaponMetadata weapon,
            WeaponKind kind,
            SwordAttackKind swordAttack,
            Vector2 aimDirection,
            float phase,
            bool facingRight
        )
        {
            Weapon = weapon;
            Kind = kind;
            SwordAttack = swordAttack;
            AimDirection = aimDirection;
            Phase = phase;
            FacingRight = facingRight;
        }
    }


    public sealed class PlayerWeaponController :
        MonoBehaviour
    {
        private enum AttackState
        {
            Idle,
            Melee,
            Gun,
            Bomb,
            BowCharging
        }


        [Header("Inventory")]

        [SerializeField]
        private PlayerInventoryWeaponBridge inventory;


        [Header("References")]

        [SerializeField]
        private Camera playerCamera;


        [SerializeField]
        private PlayerWeaponRigController rig;


        [SerializeField]
        private PlayerAnimationController animationController;


        [Header("Collision")]

        [SerializeField]
        private LayerMask targetMask =
            ~0;


        [SerializeField]
        private LayerMask projectileWorldMask =
            ~0;


        [Header("Input")]

        [SerializeField]
        private int attackMouseButton =
            0;


        [SerializeField]
        private bool ignoreAttackOverUI =
            true;


        [Header("Facing Rules")]

        [Tooltip(
            "Sword/Spear attacks are always kept in front of the player. " +
            "If the cursor is behind the player, the aim vector is mirrored to the front side."
        )]
        [SerializeField]
        private bool meleeOnlyInFront =
            true;


        [Tooltip(
            "While drawing a bow, turn the player toward the cursor."
        )]
        [SerializeField]
        private bool bowFacesCursor =
            true;


        private AttackState state =
            AttackState.Idle;


        private WeaponMetadata weapon;


        private WeaponKind kind;


        private string selectedWeaponItemId;


        private SwordAttackKind swordAttack;


        private int swordSequenceIndex;


        private float attackStartTime;


        private float attackDuration;


        private float bowChargeStartTime;


        private float bowCharge01;


        private float nextAttackTime;


        private bool fired;


        private Vector2 aimDirection =
            Vector2.right;


        private bool attackFacingRight =
            true;


        private readonly HashSet<int>
            hitTargets =
                new HashSet<int>();


        private readonly Collider2D[]
            hitBuffer =
                new Collider2D[64];


        public bool IsAttacking =>
            state != AttackState.Idle;


        public bool IsChargingBow =>
            state == AttackState.BowCharging;


        public float BowCharge01 =>
            bowCharge01;


        private void Awake()
        {
            WeaponMetadataRegistry.Initialize();


            if (inventory == null)
            {
                inventory =
                    GetComponent<
                        PlayerInventoryWeaponBridge
                    >();


                if (inventory == null)
                {
                    inventory =
                        gameObject.AddComponent<
                            PlayerInventoryWeaponBridge
                        >();
                }
            }


            if (playerCamera == null)
            {
                playerCamera =
                    Camera.main;
            }


            if (rig == null)
            {
                rig =
                    GetComponent<
                        PlayerWeaponRigController
                    >();


                if (rig == null)
                {
                    rig =
                        gameObject.AddComponent<
                            PlayerWeaponRigController
                        >();
                }
            }


            if (animationController == null)
            {
                animationController =
                    GetComponent<
                        PlayerAnimationController
                    >();


                if (animationController == null)
                {
                    animationController =
                        GetComponentInChildren<
                            PlayerAnimationController
                        >(
                            true
                        );
                }
            }
        }


        private void OnDisable()
        {
            ClearFacingOverride();
        }


        private void Update()
        {
            switch (state)
            {
                case AttackState.Idle:
                    UpdateIdle();
                    break;


                case AttackState.Melee:
                    UpdateMelee();
                    break;


                case AttackState.Gun:
                    UpdateGun();
                    break;


                case AttackState.Bomb:
                    UpdateBomb();
                    break;


                case AttackState.BowCharging:
                    UpdateBow();
                    break;
            }
        }


        public bool TryGetArmPoseRequest(
            out PlayerWeaponPoseRequest request
        )
        {
            if (
                state == AttackState.Idle ||
                weapon == null
            )
            {
                request =
                    default;


                return false;
            }


            float phase =
                state == AttackState.BowCharging
                    ? bowCharge01
                    : NormalizedAttackTime();


            request =
                new PlayerWeaponPoseRequest(
                    weapon,
                    kind,
                    swordAttack,
                    aimDirection,
                    phase,
                    attackFacingRight
                );


            return true;
        }


        public bool IsWeaponSelected()
        {
            if (inventory == null)
                return false;


            return
                WeaponMetadataRegistry.IsWeapon(
                    inventory.GetSelectedItemId()
                );
        }


        // =====================================================
        // START
        // =====================================================

        private void UpdateIdle()
        {
            if (Time.time < nextAttackTime)
                return;


            if (
                !Input.GetMouseButtonDown(
                    attackMouseButton
                )
            )
            {
                return;
            }


            StartAttack();
        }


        private void StartAttack()
        {
            if (
                ignoreAttackOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject()
            )
            {
                return;
            }


            string id =
                inventory != null
                    ? inventory.GetSelectedItemId()
                    : null;


            if (
                !WeaponMetadataRegistry.TryGet(
                    id,
                    out WeaponMetadata selected
                )
            )
            {
                return;
            }


            selected.Normalize();


            WeaponKind selectedKind =
                selected.GetKind();


            if (
                selectedKind ==
                WeaponKind.None
            )
            {
                return;
            }


            if (
                (
                    selectedKind == WeaponKind.Bow ||
                    selectedKind == WeaponKind.Gun
                ) &&
                !CanPayAmmo(
                    selected
                )
            )
            {
                return;
            }


            if (
                selectedKind == WeaponKind.Bomb &&
                (inventory == null ||
                 !inventory.HasItem(id, 1))
            )
            {
                return;
            }


            weapon =
                selected;


            selectedWeaponItemId =
                id;


            kind =
                selectedKind;


            hitTargets.Clear();


            fired =
                false;


            attackFacingRight =
                animationController == null
                    ? true
                    : animationController.IsFacingRight();


            if (
                kind == WeaponKind.Bow
            )
            {
                state =
                    AttackState.BowCharging;


                bowChargeStartTime =
                    Time.time;


                bowCharge01 =
                    0f;


                RefreshBowAimAndFacing();


                return;
            }


            if (
                kind == WeaponKind.Sword ||
                kind == WeaponKind.Spear
            )
            {
                // Freeze facing for this melee attack.
                SetFacingOverride(
                    attackFacingRight
                );


                RefreshMeleeAim();


                attackDuration =
                    1f /
                    Mathf.Max(
                        0.01f,
                        weapon.AttackSpeed
                    );


                attackStartTime =
                    Time.time;


                if (
                    kind == WeaponKind.Sword
                )
                {
                    swordAttack =
                        weapon.GetSwordPatternEntry(
                            swordSequenceIndex
                        );


                    swordSequenceIndex++;
                }


                state =
                    AttackState.Melee;


                return;
            }


            RefreshRawAim();


            attackDuration =
                1f /
                Mathf.Max(
                    0.01f,
                    weapon.AttackSpeed
                );


            attackStartTime =
                Time.time;


            if (
                kind == WeaponKind.Bomb
            )
            {
                state =
                    AttackState.Bomb;


                if (
                    weapon.FireNormalizedTime <=
                    0f
                )
                {
                    ThrowBomb();
                }


                return;
            }


            if (
                kind == WeaponKind.Gun
            )
            {
                state =
                    AttackState.Gun;


                if (
                    weapon.FireNormalizedTime <=
                    0f
                )
                {
                    FireRanged(
                        1f
                    );
                }
            }
        }


        // =====================================================
        // UPDATE
        // =====================================================

        private void UpdateMelee()
        {
            if (weapon == null)
            {
                CancelAttack();
                return;
            }


            // Keep exactly the facing that existed when the swing began.
            SetFacingOverride(
                attackFacingRight
            );


            if (weapon.TrackCursorDuringAttack)
            {
                RefreshMeleeAim();
            }


            float t =
                NormalizedAttackTime();


            if (
                kind == WeaponKind.Sword &&
                swordAttack ==
                SwordAttackKind.Swing
            )
            {
                DamageSwordSwing(
                    t
                );
            }
            else if (
                kind == WeaponKind.Sword &&
                swordAttack ==
                SwordAttackKind.Thrust
            )
            {
                DamageSwordThrust(
                    t
                );
            }
            else if (
                kind == WeaponKind.Spear
            )
            {
                DamageSpear(
                    t
                );
            }


            if (t >= 1f)
            {
                EndAttack();
            }
        }


        private void UpdateGun()
        {
            if (weapon == null)
            {
                CancelAttack();
                return;
            }


            RefreshRawAim();


            float t =
                NormalizedAttackTime();


            if (
                !fired &&
                t >=
                weapon.FireNormalizedTime
            )
            {
                FireRanged(
                    1f
                );
            }


            if (t >= 1f)
            {
                EndAttack();
            }
        }


        private void UpdateBomb()
        {
            if (weapon == null)
            {
                CancelAttack();
                return;
            }


            RefreshRawAim();


            float t =
                NormalizedAttackTime();


            if (
                !fired &&
                t >=
                weapon.FireNormalizedTime
            )
            {
                ThrowBomb();
            }


            if (t >= 1f)
            {
                EndAttack();
            }
        }


        private void UpdateBow()
        {
            if (weapon == null)
            {
                CancelAttack();
                return;
            }


            RefreshBowAimAndFacing();


            bowCharge01 =
                Mathf.Clamp01(
                    (
                        Time.time -
                        bowChargeStartTime
                    ) /
                    Mathf.Max(
                        0.01f,
                        weapon.BowChargeTime
                    )
                );


            if (
                !Input.GetMouseButtonUp(
                    attackMouseButton
                )
            )
            {
                return;
            }


            float power =
                Mathf.Lerp(
                    weapon.BowMinPower,
                    1f,
                    bowCharge01
                );


            FireRanged(
                power
            );


            nextAttackTime =
                Time.time +
                1f /
                Mathf.Max(
                    0.01f,
                    weapon.AttackSpeed
                );


            EndAttack();
        }


        // =====================================================
        // AIM RULES
        // =====================================================

        private void RefreshRawAim()
        {
            Vector2 d =
                ReadMouseDirection();


            if (
                d.sqrMagnitude <
                0.000001f
            )
            {
                d =
                    attackFacingRight
                        ? Vector2.right
                        : Vector2.left;
            }


            aimDirection =
                d.normalized;
        }


        private void RefreshMeleeAim()
        {
            Vector2 d =
                ReadMouseDirection();


            if (!meleeOnlyInFront)
            {
                aimDirection =
                    d.sqrMagnitude >
                    0.000001f
                        ? d.normalized
                        : (
                            attackFacingRight
                                ? Vector2.right
                                : Vector2.left
                        );


                return;
            }


            aimDirection =
                ForceDirectionToFacingSide(
                    d,
                    attackFacingRight
                );
        }


        private void RefreshBowAimAndFacing()
        {
            Vector2 d =
                ReadMouseDirection();


            bool right =
                d.x >=
                0f;


            if (bowFacesCursor)
            {
                SetFacingOverride(
                    right
                );


                attackFacingRight =
                    right;


                // FacingRoot can change immediately.
                // Re-read cursor direction from the now-current shoulder.
                d =
                    ReadMouseDirection();
            }


            if (
                d.sqrMagnitude <
                0.000001f
            )
            {
                d =
                    right
                        ? Vector2.right
                        : Vector2.left;
            }


            aimDirection =
                d.normalized;
        }


        private Vector2 ReadMouseDirection()
        {
            if (playerCamera == null)
            {
                playerCamera =
                    Camera.main;
            }


            if (playerCamera == null)
            {
                return
                    Vector2.right;
            }


            Vector3 mouseWorld =
                playerCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );


            Vector2 origin =
                rig != null
                    ? rig.GetFrontShoulderWorldPosition()
                    : (Vector2)
                      transform.position;


            return
                (Vector2)
                mouseWorld
                -
                origin;
        }


        private static Vector2 ForceDirectionToFacingSide(
            Vector2 raw,
            bool facingRight
        )
        {
            float sign =
                facingRight
                    ? 1f
                    : -1f;


            if (
                raw.sqrMagnitude <
                0.000001f
            )
            {
                return
                    new Vector2(
                        sign,
                        0f
                    );
            }


            // Mirror only the horizontal side.
            // Cursor behind the player therefore becomes the same attack
            // direction in front of the player, while preserving up/down aim.
            float x =
                Mathf.Abs(
                    raw.x
                )
                *
                sign;


            if (
                Mathf.Abs(
                    x
                )
                <
                0.001f
            )
            {
                x =
                    0.001f *
                    sign;
            }


            return
                new Vector2(
                    x,
                    raw.y
                )
                .normalized;
        }


        // =====================================================
        // FACING
        // =====================================================

        private void SetFacingOverride(
            bool right
        )
        {
            if (animationController == null)
                return;


            animationController
                .SetExternalFacingOverride(
                    true,
                    right
                );
        }


        private void ClearFacingOverride()
        {
            if (animationController == null)
                return;


            animationController
                .ClearExternalFacingOverride();
        }


        // =====================================================
        // DAMAGE
        // =====================================================

        private void DamageSwordSwing(
            float t
        )
        {
            float relative =
                Mathf.LerpAngle(
                    weapon.SwordSwingStartAngle,
                    weapon.SwordSwingEndAngle,
                    Smooth01(
                        t
                    )
                );


            // IMPORTANT:
            // Build the swing once in RIGHT-facing space,
            // then mirror the resulting VECTOR for left-facing.
            //
            // This is a true horizontal mirror:
            // (x, y) -> (-x, y)
            //
            // It does not depend on angle wrapping around 180/-180.
            Vector2 direction =
                BuildFacingMirroredSwingDirection(
                    aimDirection,
                    attackFacingRight,
                    relative
                );


            Vector2 origin =
                rig != null
                    ? rig.GetFrontShoulderWorldPosition()
                    : (Vector2)
                      transform.position;


            Vector2 swingTip =
                origin +
                direction *
                weapon.SwingReach;

            WeaponDamageUtility.DamageActiveEntitiesAlongSegment(
                origin,
                swingTip,
                weapon.SwingHitRadius,
                new WeaponDamageInfo(
                    weapon.Damage,
                    weapon.Knockback,
                    direction,
                    swingTip,
                    gameObject
                ),
                hitTargets
            );

            DamageCircle(
                swingTip,
                weapon.SwingHitRadius,
                direction
            );
        }


        private void DamageSwordThrust(
            float t
        )
        {
            float extension =
                ThrustEnvelope(
                    t
                )
                *
                weapon.ThrustDistance;


            Vector2 center =
                GetMuzzleWorld()
                +
                aimDirection
                *
                (
                    weapon.Range +
                    extension
                );


            WeaponDamageUtility.DamageActiveEntitiesAlongSegment(
                GetMuzzleWorld(),
                center,
                weapon.ThrustHitRadius,
                new WeaponDamageInfo(
                    weapon.Damage,
                    weapon.Knockback,
                    aimDirection,
                    center,
                    gameObject
                ),
                hitTargets
            );

            DamageCircle(
                center,
                weapon.ThrustHitRadius,
                aimDirection
            );
        }


        private void DamageSpear(
            float t
        )
        {
            float extension =
                ThrustEnvelope(
                    t
                )
                *
                weapon.SpearThrustDistance;


            Vector2 center =
                GetMuzzleWorld()
                +
                aimDirection
                *
                (
                    weapon.SpearBaseReach +
                    extension
                );


            WeaponDamageUtility.DamageActiveEntitiesAlongSegment(
                GetMuzzleWorld(),
                center,
                weapon.SpearHitRadius,
                new WeaponDamageInfo(
                    weapon.Damage,
                    weapon.Knockback,
                    aimDirection,
                    center,
                    gameObject
                ),
                hitTargets
            );

            DamageCircle(
                center,
                weapon.SpearHitRadius,
                aimDirection
            );
        }


        private void DamageCircle(
            Vector2 center,
            float radius,
            Vector2 direction
        )
        {
            // Entity damage must not depend on the serialized targetMask. Existing
            // PlayerWeaponController components can keep an old mask from before
            // enemies existed, which made swords visually pass through every enemy.
            WeaponDamageInfo entityInfo =
                new WeaponDamageInfo(
                    weapon.Damage,
                    weapon.Knockback,
                    direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right,
                    center,
                    gameObject
                );

            WeaponDamageUtility.DamageActiveEntitiesInCircle(
                center,
                Mathf.Max(0.01f, radius),
                entityInfo,
                hitTargets
            );

            int count =
                WeaponDamageUtility.OverlapCircleIncludingTriggers(
                    center,
                    Mathf.Max(
                        0.01f,
                        radius
                    ),
                    hitBuffer,
                    targetMask
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                Collider2D c =
                    hitBuffer[i];


                if (c == null)
                    continue;


                if (
                    c.transform == transform ||
                    c.transform.IsChildOf(
                        transform
                    )
                )
                {
                    continue;
                }


                int key =
                    WeaponDamageUtility
                        .GetTargetKey(
                            c
                        );


                if (
                    key != 0 &&
                    hitTargets.Contains(
                        key
                    )
                )
                {
                    continue;
                }


                WeaponDamageInfo info =
                    new WeaponDamageInfo(
                        weapon.Damage,
                        weapon.Knockback,
                        direction.normalized,
                        center,
                        gameObject
                    );


                if (
                    WeaponDamageUtility.TryDealDamage(
                        c,
                        info
                    )
                )
                {
                    if (key != 0)
                    {
                        hitTargets.Add(
                            key
                        );
                    }
                }
            }
        }


        // =====================================================
        // PROJECTILES
        // =====================================================

        private bool CanPayAmmo(
            WeaponMetadata data
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    data.AmmoItem
                )
            )
            {
                return true;
            }


            return
                inventory != null &&
                inventory.HasItem(
                    data.AmmoItem,
                    data.AmmoPerShot
                );
        }


        private void FireRanged(
            float power
        )
        {
            if (
                fired ||
                weapon == null
            )
            {
                return;
            }


            if (
                !string.IsNullOrWhiteSpace(
                    weapon.AmmoItem
                )
            )
            {
                if (
                    inventory == null ||
                    !inventory.RemoveItem(
                        weapon.AmmoItem,
                        weapon.AmmoPerShot
                    )
                )
                {
                    CancelAttack();
                    return;
                }
            }


            fired =
                true;


            Vector2 start =
                GetMuzzleWorld()
                +
                aimDirection
                *
                weapon.MuzzleOffset;


            int count =
                Mathf.Max(
                    1,
                    weapon.ProjectileCount
                );


            float speed =
                weapon.ProjectileSpeed
                *
                (
                    kind == WeaponKind.Bow
                        ? Mathf.Max(
                            0.01f,
                            power
                        )
                        : 1f
                );


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                float spread =
                    SpreadOffset(
                        i,
                        count
                    );


                Vector2 direction =
                    Rotate(
                        aimDirection,
                        spread
                    );


                WeaponProjectile.Spawn(
                    start,
                    direction *
                    speed,
                    weapon,
                    transform,
                    projectileWorldMask,
                    targetMask,
                    kind == WeaponKind.Bow
                        ? power
                        : 1f
                );
            }
        }


        private void ThrowBomb()
        {
            if (fired || weapon == null)
                return;


            if (
                inventory == null ||
                string.IsNullOrWhiteSpace(selectedWeaponItemId) ||
                !inventory.RemoveItem(
                    selectedWeaponItemId,
                    1
                )
            )
            {
                CancelAttack();
                return;
            }


            fired =
                true;


            Vector2 direction =
                aimDirection;


            direction.y +=
                0.28f;


            if (direction.sqrMagnitude < 0.0001f)
            {
                direction =
                    attackFacingRight
                        ? Vector2.right
                        : Vector2.left;
            }


            direction.Normalize();


            Game.World.Projectiles.BombProjectile.Spawn(
                GetMuzzleWorld() +
                    direction *
                    Mathf.Max(0.2f, weapon.MuzzleOffset),
                direction *
                    Mathf.Max(0.1f, weapon.BombThrowSpeed),
                weapon,
                transform
            );
        }


        private Vector2 GetMuzzleWorld()
        {
            if (rig != null)
            {
                Transform hp =
                    rig.GetRuntimeHandPoint();


                if (hp != null)
                {
                    return
                        hp.position;
                }
            }


            return
                transform.position;
        }


        private float SpreadOffset(
            int index,
            int count
        )
        {
            if (
                count <= 1 ||
                weapon.SpreadDegrees <=
                0.0001f
            )
            {
                return 0f;
            }


            float half =
                weapon.SpreadDegrees *
                0.5f;


            if (
                string.Equals(
                    weapon.SpreadMode,
                    "Even",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return
                    Mathf.Lerp(
                        -half,
                        half,
                        index /
                        (float)(
                            count - 1
                        )
                    );
            }


            return
                UnityEngine.Random.Range(
                    -half,
                    half
                );
        }


        // =====================================================
        // END
        // =====================================================

        private void EndAttack()
        {
            ClearFacingOverride();


            state =
                AttackState.Idle;


            fired =
                false;


            bowCharge01 =
                0f;


            hitTargets.Clear();


            weapon =
                null;


            selectedWeaponItemId =
                null;
        }


        private void CancelAttack()
        {
            ClearFacingOverride();


            state =
                AttackState.Idle;


            fired =
                false;


            bowCharge01 =
                0f;


            hitTargets.Clear();


            weapon =
                null;


            selectedWeaponItemId =
                null;
        }


        // =====================================================
        // MATH
        // =====================================================

        private float NormalizedAttackTime()
        {
            if (attackDuration <= 0f)
                return 1f;


            return
                Mathf.Clamp01(
                    (
                        Time.time -
                        attackStartTime
                    )
                    /
                    attackDuration
                );
        }



private static Vector2 BuildFacingMirroredSwingDirection(
    Vector2 frontAim,
    bool facingRight,
    float relativeAngle
)
{
    float sign =
        facingRight
            ? 1f
            : -1f;


    Vector2 canonicalAim =
        new Vector2(
            Mathf.Abs(
                frontAim.x
            ),
            frontAim.y
        );


    if (
        canonicalAim.sqrMagnitude <
        0.000001f
    )
    {
        canonicalAim =
            Vector2.right;
    }


    canonicalAim.Normalize();


    Vector2 canonicalSwing =
        Rotate(
            canonicalAim,
            relativeAngle
        );


    Vector2 worldSwing =
        new Vector2(
            canonicalSwing.x *
            sign,
            canonicalSwing.y
        );


    if (
        worldSwing.sqrMagnitude <
        0.000001f
    )
    {
        return
            facingRight
                ? Vector2.right
                : Vector2.left;
    }


    return
        worldSwing.normalized;
}


        private static float DirectionAngle(
            Vector2 direction
        )
        {
            return
                Mathf.Atan2(
                    direction.y,
                    direction.x
                )
                *
                Mathf.Rad2Deg;
        }


        private static Vector2 DirectionFromAngle(
            float degrees
        )
        {
            float r =
                degrees *
                Mathf.Deg2Rad;


            return
                new Vector2(
                    Mathf.Cos(r),
                    Mathf.Sin(r)
                );
        }


        private static Vector2 Rotate(
            Vector2 v,
            float degrees
        )
        {
            float r =
                degrees *
                Mathf.Deg2Rad;


            float c =
                Mathf.Cos(r);


            float s =
                Mathf.Sin(r);


            return
                new Vector2(
                    v.x * c -
                    v.y * s,
                    v.x * s +
                    v.y * c
                );
        }


        private static float Smooth01(
            float t
        )
        {
            t =
                Mathf.Clamp01(
                    t
                );


            return
                t *
                t *
                (
                    3f -
                    2f *
                    t
                );
        }


        private static float ThrustEnvelope(
            float t
        )
        {
            float s =
                Mathf.Sin(
                    Mathf.Clamp01(
                        t
                    )
                    *
                    Mathf.PI
                );


            return
                s * s;
        }
    }
}
