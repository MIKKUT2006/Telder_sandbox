using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.Entities.AI;
using Game.Entities.Visual;
using Game.World;
using Game.World.Items;
using Game.World.Projectiles;
using Game.Items;

namespace Game.Entities
{
    [DisallowMultipleComponent]
    public sealed class EntityActor : MonoBehaviour, IWeaponDamageable, IWeaponDamageGate
    {
        private static readonly List<EntityActor> Active =
            new List<EntityActor>(64);

        public static IReadOnlyList<EntityActor> ActiveActors
        {
            get { return Active; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveRegistry()
        {
            Active.Clear();
        }

        private enum BrainState
        {
            Idle,
            Wander,
            Chase,
            Attack,
            Flee,
            Dead
        }

        private EntityDefinition definition;
        private Game.PlayerStats.PlayerStats playerStats;
        private Transform player;
        private EntityMovementController movement;
        private EntityVisualController visual;
        private BoxCollider2D hitCollider;
        private Rigidbody2D physicsBody;

        private BrainState state;
        private float health;
        private float maxHealth;
        private float baseDamage;
        private float lootMultiplier;
        private int stars;

        private string heldItemId;
        private WeaponMetadata heldWeapon;

        private float nextThinkTime;
        private float nextWanderTime;
        private float nextAttackTime;
        private float aggroUntil;
        private float fleeUntil;
        private float attackExecuteAt;
        private float attackEndAt;
        private float hurtUntil;
        private bool attackPending;
        private Vector2 attackDirection;
        private Vector2 wanderTarget;

        public string DefinitionId { get { return definition != null ? definition.ID : string.Empty; } }
        public int Stars { get { return stars; } }
        public float Health { get { return health; } }
        public float MaxHealth { get { return maxHealth; } }
        public bool IsDead { get { return state == BrainState.Dead; } }
        public string HeldItemId { get { return heldItemId; } }

        public int DamageTargetKey
        {
            get { return gameObject.GetInstanceID(); }
        }

        public bool IsDamageTargetActive
        {
            get
            {
                return definition != null &&
                       state != BrainState.Dead &&
                       isActiveAndEnabled &&
                       gameObject.activeInHierarchy;
            }
        }

        public Rect GetDamageBounds()
        {
            if (definition == null)
                return new Rect(transform.position.x, transform.position.y, 0f, 0f);

            Vector2 center = physicsBody != null
                ? physicsBody.position
                : (Vector2)transform.position;

            float width = Mathf.Max(0.05f, definition.ColliderWidth);
            float height = Mathf.Max(0.05f, definition.ColliderHeight);

            return new Rect(
                center.x - width * 0.5f,
                center.y - height * 0.5f,
                width,
                height);
        }

        public event Action<EntityActor> Died;

        private void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void OnDestroy()
        {
            Active.Remove(this);
        }

        public void Initialize(EntityDefinition source, int starCount, Game.PlayerStats.PlayerStats targetPlayer)
        {
            definition = source;
            definition.Normalize();
            stars = Mathf.Clamp(starCount, 1, 5);
            playerStats = targetPlayer;
            player = playerStats != null ? playerStats.transform : null;

            maxHealth = definition.MaxHealth * definition.Stars.HealthMultiplier(stars);
            health = maxHealth;
            baseDamage = definition.Combat.BaseDamage * definition.Stars.DamageMultiplier(stars);
            lootMultiplier = definition.Stars.LootMultiplier(stars);

            WorldManager manager = WorldManager.Instance;
            movement = new EntityMovementController(
                definition,
                manager != null ? manager.GetWorldCollision() : null);

            hitCollider = gameObject.GetComponent<BoxCollider2D>();
            if (hitCollider == null)
                hitCollider = gameObject.AddComponent<BoxCollider2D>();
            hitCollider.size = new Vector2(definition.ColliderWidth, definition.ColliderHeight);
            hitCollider.offset = Vector2.zero;
            hitCollider.isTrigger = true;

            // Keep the runtime hitbox registered as a moving physics object.
            // The entity still uses the custom tile CharacterMotor for movement;
            // this Rigidbody2D exists so sword/arrow physics queries always see
            // the current enemy position instead of a moved static collider.
            physicsBody = gameObject.GetComponent<Rigidbody2D>();
            if (physicsBody == null)
                physicsBody = gameObject.AddComponent<Rigidbody2D>();
            physicsBody.bodyType = RigidbodyType2D.Kinematic;
            physicsBody.gravityScale = 0f;
            physicsBody.simulated = true;
            physicsBody.freezeRotation = true;
            physicsBody.interpolation = RigidbodyInterpolation2D.None;

            visual = new EntityVisualController(transform, definition, stars);

            if (GetComponent<Game.World.Lighting.WorldLightSpriteReceiver>() == null)
                gameObject.AddComponent<Game.World.Lighting.WorldLightSpriteReceiver>();

            RollEquipment();
            visual.SetHeldItem(heldItemId);

            state = BrainState.Idle;
            nextThinkTime = Time.time + UnityEngine.Random.Range(0f, 0.15f);
            nextWanderTime = Time.time + UnityEngine.Random.Range(0.05f, 0.35f);

            gameObject.name = (string.IsNullOrWhiteSpace(definition.Name) ? definition.ID : definition.Name) + " " + new string('★', stars);
        }

        private void Update()
        {
            if (definition == null)
                return;

            if (state == BrainState.Dead)
            {
                visual.SetState(EntityVisualState.Death);
                visual.Tick();
                return;
            }

            UpdatePendingAttack();

            if (Time.time >= nextThinkTime)
            {
                nextThinkTime = Time.time + 0.10f + UnityEngine.Random.Range(0f, 0.04f);
                Think();
            }

            UpdateVisual();
            visual.Tick();
        }

        private void FixedUpdate()
        {
            if (definition == null || state == BrainState.Dead || movement == null)
                return;

            if (state == BrainState.Attack && Time.time < attackEndAt)
                return;

            Vector2 old = physicsBody != null ? physicsBody.position : (Vector2)transform.position;
            Vector2 next = movement.Tick(old, Time.fixedDeltaTime);

            if (physicsBody != null)
                physicsBody.position = next;
            else
                transform.position = next;

            float dx = next.x - old.x;
            if (Mathf.Abs(dx) > 0.0001f)
                visual.SetFacing(dx);
        }

        private void Think()
        {
            if (player == null || playerStats == null || playerStats.IsDead)
            {
                TryWander();
                return;
            }

            float distance = Vector2.Distance(transform.position, player.position);
            EntityDisposition disposition = definition.GetDisposition();

            if (disposition == EntityDisposition.Passive && Time.time < fleeUntil)
            {
                state = BrainState.Flee;
                Vector2 away = ((Vector2)transform.position - (Vector2)player.position);
                if (away.sqrMagnitude < 0.001f)
                    away = Vector2.right;
                Vector2 target = (Vector2)transform.position + away.normalized * Mathf.Max(4f, definition.WanderRadius);
                movement.SetTarget(transform.position, target);
                return;
            }

            bool retaliating = Time.time < aggroUntil;
            bool canAggroBySight = disposition == EntityDisposition.Aggressive;
            bool seesPlayer = distance <= definition.Combat.VisionRange && HasLineOfSightToPlayer();
            bool shouldAggro = retaliating || (canAggroBySight && seesPlayer);

            if (!shouldAggro)
            {
                TryWander();
                return;
            }

            if (distance > definition.Combat.LoseTargetRange && !retaliating)
            {
                TryWander();
                return;
            }

            float attackRange = GetEffectiveAttackRange();
            bool ranged = IsRangedWeapon();

            if ((ranged && distance <= Mathf.Max(attackRange, definition.Combat.VisionRange)) ||
                (!ranged && distance <= attackRange))
            {
                if (!definition.Combat.RequireLineOfSight || HasLineOfSightToPlayer())
                {
                    TryBeginAttack();
                    return;
                }
            }

            state = BrainState.Chase;
            movement.SetTarget(transform.position, player.position);
        }

        private void TryWander()
        {
            if (state == BrainState.Attack && Time.time < attackEndAt)
                return;

            if (Time.time >= nextWanderTime)
            {
                nextWanderTime = Time.time + UnityEngine.Random.Range(2.0f, 5.5f);
                Vector2 offset;

                if (definition.Movement.GetKind() == EntityMovementKind.Flying)
                {
                    offset = UnityEngine.Random.insideUnitCircle * Mathf.Max(1f, definition.WanderRadius);
                }
                else
                {
                    // Ground/Jumping creatures choose a horizontal wander goal.
                    // A random vertical target made the pathfinder constantly prefer
                    // unnecessary ledges and caused repeated jumping on otherwise flat ground.
                    offset = new Vector2(
                        UnityEngine.Random.Range(-definition.WanderRadius, definition.WanderRadius),
                        0f);
                }

                wanderTarget = (Vector2)transform.position + offset;
                movement.SetTarget(transform.position, wanderTarget, true);
            }

            state = movement.HasPath ? BrainState.Wander : BrainState.Idle;
        }

        private void TryBeginAttack()
        {
            if (Time.time < nextAttackTime || attackPending || player == null)
                return;

            Vector2 direction = (Vector2)player.position - (Vector2)transform.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            attackDirection = direction.normalized;
            visual.SetFacing(attackDirection.x);

            float windup = 0.14f;
            if (heldWeapon != null && heldWeapon.GetKind() == WeaponKind.Bow)
                windup = Mathf.Max(0.08f, heldWeapon.BowChargeTime);
            else if (heldWeapon != null && heldWeapon.GetKind() == WeaponKind.Bomb)
                windup = 0.28f;

            attackExecuteAt = Time.time + windup;
            attackEndAt = attackExecuteAt + 0.18f;
            attackPending = true;
            state = BrainState.Attack;
            movement.ClearPath();
            visual.SetState(EntityVisualState.Attack, true);
        }

        private void UpdatePendingAttack()
        {
            if (!attackPending || Time.time < attackExecuteAt)
                return;

            attackPending = false;
            ExecuteAttack();

            float cooldown = definition.Combat.AttackCooldown;
            if (heldWeapon != null)
                cooldown = Mathf.Max(cooldown, 1f / Mathf.Max(0.01f, heldWeapon.AttackSpeed));
            nextAttackTime = Time.time + cooldown;
        }

        private void ExecuteAttack()
        {
            if (player == null || playerStats == null || playerStats.IsDead)
                return;

            if (heldWeapon == null)
            {
                DealDirectMelee(baseDamage, definition.Combat.AttackRange);
                return;
            }

            string aiMode = string.IsNullOrWhiteSpace(heldWeapon.AIUseMode) ? "Auto" : heldWeapon.AIUseMode.Trim();
            WeaponKind kind = heldWeapon.GetKind();

            if (kind == WeaponKind.Bomb || aiMode.Equals("Throwable", StringComparison.OrdinalIgnoreCase))
            {
                ThrowBomb();
                return;
            }

            if (kind == WeaponKind.Bow || kind == WeaponKind.Gun || aiMode.Equals("Ranged", StringComparison.OrdinalIgnoreCase))
            {
                FireWeaponProjectile();
                return;
            }

            float damage = baseDamage + heldWeapon.Damage * definition.Stars.DamageMultiplier(stars);
            float range = Mathf.Max(definition.Combat.AttackRange, heldWeapon.Range, heldWeapon.SwingReach, heldWeapon.SpearBaseReach);
            DealDirectMelee(damage, range);
        }

        private void DealDirectMelee(float damage, float range)
        {
            if (player == null || playerStats == null)
                return;

            if (Vector2.Distance(transform.position, player.position) > Mathf.Max(0.1f, range) + 0.35f)
                return;

            if (definition.Combat.RequireLineOfSight && !HasLineOfSightToPlayer())
                return;

            playerStats.TakeDamage(Mathf.Max(0f, damage));
        }

        private void FireWeaponProjectile()
        {
            if (heldWeapon == null || player == null)
                return;

            Vector2 start = (Vector2)transform.position + attackDirection * Mathf.Max(0.15f, heldWeapon.MuzzleOffset);
            int count = Mathf.Max(1, heldWeapon.ProjectileCount);
            float speed = Mathf.Max(0.1f, heldWeapon.ProjectileSpeed);

            for (int i = 0; i < count; i++)
            {
                float spread = count <= 1
                    ? 0f
                    : Mathf.Lerp(-heldWeapon.SpreadDegrees * 0.5f, heldWeapon.SpreadDegrees * 0.5f, i / (float)(count - 1));

                Vector2 direction = Rotate(attackDirection, spread);
                WeaponProjectile.Spawn(
                    start,
                    direction * speed,
                    heldWeapon,
                    transform,
                    Physics2D.DefaultRaycastLayers,
                    Physics2D.DefaultRaycastLayers,
                    1f,
                    definition.Stars.DamageMultiplier(stars),
                    definition.Combat.BaseDamage);
            }
        }

        private void ThrowBomb()
        {
            if (heldWeapon == null || player == null)
                return;

            Vector2 origin =
                (Vector2)transform.position +
                Vector2.up * Mathf.Max(0.1f, definition.ColliderHeight * 0.15f);

            float gravity = Mathf.Max(0.01f, heldWeapon.BombGravity);
            float configuredSpeed = Mathf.Max(0.1f, heldWeapon.BombThrowSpeed);
            Vector2 target = (Vector2)player.position;
            Vector2 delta = target - origin;

            // Use a short ballistic travel time instead of merely adding +Y to the
            // aim vector. This makes the enemy actually throw the bomb toward the
            // player at different heights and distances.
            float horizontalDistance = Mathf.Abs(delta.x);
            float travelTime = Mathf.Clamp(
                horizontalDistance / configuredSpeed,
                0.38f,
                1.15f);

            Vector2 initialVelocity = new Vector2(
                delta.x / travelTime,
                (delta.y + 0.5f * gravity * travelTime * travelTime) / travelTime);

            // Keep extremely close targets from producing a nearly vertical toss.
            if (horizontalDistance < 0.35f)
            {
                float side = Mathf.Abs(attackDirection.x) > 0.01f
                    ? Mathf.Sign(attackDirection.x)
                    : 1f;
                initialVelocity.x = side * configuredSpeed * 0.35f;
            }

            Vector2 spawnDirection = initialVelocity.sqrMagnitude > 0.001f
                ? initialVelocity.normalized
                : Vector2.up;

            BombProjectile.Spawn(
                origin + spawnDirection * 0.38f,
                initialVelocity,
                heldWeapon,
                transform,
                definition.Stars.DamageMultiplier(stars),
                definition.Combat.BaseDamage);
        }

        private float GetEffectiveAttackRange()
        {
            if (heldWeapon == null)
                return definition.Combat.AttackRange;

            WeaponKind kind = heldWeapon.GetKind();
            if (kind == WeaponKind.Bow || kind == WeaponKind.Gun || kind == WeaponKind.Bomb ||
                string.Equals(heldWeapon.AIUseMode, "Ranged", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(heldWeapon.AIUseMode, "Throwable", StringComparison.OrdinalIgnoreCase))
            {
                return definition.Combat.VisionRange;
            }

            return Mathf.Max(
                definition.Combat.AttackRange,
                heldWeapon.Range,
                heldWeapon.SwingReach,
                heldWeapon.SpearBaseReach);
        }

        private bool IsRangedWeapon()
        {
            if (heldWeapon == null)
                return false;

            WeaponKind kind = heldWeapon.GetKind();
            return kind == WeaponKind.Bow || kind == WeaponKind.Gun || kind == WeaponKind.Bomb ||
                   string.Equals(heldWeapon.AIUseMode, "Ranged", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(heldWeapon.AIUseMode, "Throwable", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasLineOfSightToPlayer()
        {
            if (player == null)
                return false;

            WorldManager manager = WorldManager.Instance;
            if (manager == null || manager.GetWorldCollision() == null)
                return true;

            Vector2 start = transform.position;
            Vector2 end = player.position;
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.01f)
                return true;

            Vector2 dir = delta / distance;
            int steps = Mathf.CeilToInt(distance / 0.25f);
            for (int i = 1; i < steps; i++)
            {
                Vector2 p = start + dir * (distance * i / steps);
                if (manager.GetWorldCollision().IsSolid(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)))
                    return false;
            }

            return true;
        }

        private void RollEquipment()
        {
            heldItemId = null;
            heldWeapon = null;

            if (definition.Equipment == null || definition.Equipment.Length == 0)
                return;

            for (int i = 0; i < definition.Equipment.Length; i++)
            {
                EntityEquipmentEntry entry = definition.Equipment[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                    continue;

                if (UnityEngine.Random.value > Mathf.Clamp01(entry.Chance))
                    continue;

                heldItemId = entry.ItemId.Trim();
                WeaponMetadataRegistry.TryGet(heldItemId, out heldWeapon);

                // A creature may be configured with a custom dynamite/bomb item
                // whose JSON only says Type = Bomb and has no Weapon block yet.
                // In that case it used to visibly hold the item but attack as if
                // unarmed. Give Bomb items sane throwable defaults automatically.
                if (heldWeapon == null &&
                    ItemRegistry.TryGet(heldItemId, out ItemDefinition itemDefinition) &&
                    itemDefinition != null &&
                    itemDefinition.Type == ItemType.Bomb)
                {
                    heldWeapon = new WeaponMetadata
                    {
                        Kind = "Bomb",
                        AIUseMode = "Throwable",
                        Damage = Mathf.Max(1f, definition.Combat.BaseDamage),
                        AttackSpeed = 0.8f,
                        Range = Mathf.Max(6f, definition.Combat.VisionRange),
                        ProjectileItem = heldItemId,
                        ProjectileWorldSize = 0.55f,
                        BombFuseTime = 3f,
                        BombExplosionWidth = 2,
                        BombExplosionHeight = 2,
                        BombThrowSpeed = 8f,
                        BombGravity = 18f,
                        BombBounce = 0.32f,
                        BombDestroyBlocks = true,
                        BombDestroyBackground = false,
                        BombDestroyFurniture = true,
                        BombKnockback = 5f
                    };
                }

                if (heldWeapon != null &&
                    heldWeapon.GetKind() == WeaponKind.Bomb &&
                    string.IsNullOrWhiteSpace(heldWeapon.ProjectileItem))
                {
                    heldWeapon.ProjectileItem = heldItemId;
                }

                return;
            }
        }

        public bool CanReceiveWeaponDamage(WeaponDamageInfo info)
        {
            if (state == BrainState.Dead)
                return false;

            if (info.Source != null && info.Source.GetComponentInParent<EntityActor>() != null)
                return false;

            return true;
        }

        public void ReceiveWeaponDamage(WeaponDamageInfo info)
        {
            if (!CanReceiveWeaponDamage(info))
                return;

            float healthBefore = health;

            Vector2 direction = info.Direction;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                if (info.Source != null)
                    direction = (Vector2)transform.position - (Vector2)info.Source.transform.position;

                if (direction.sqrMagnitude <= 0.0001f)
                    direction = Vector2.right;
            }

            direction.Normalize();

            // Ground targets get a small upward component so a sword hit reads as
            // impact instead of a barely-visible horizontal slide along the floor.
            if (definition != null &&
                definition.Movement.GetKind() != EntityMovementKind.Flying)
            {
                direction.y = Mathf.Max(direction.y, 0.26f);
                direction.Normalize();
            }

            Vector2 impulse =
                direction * Mathf.Max(0f, info.Knockback) * 1.25f;

            TakeDamage(info.Damage, impulse);

            if (health < healthBefore && definition != null && definition.Visual != null)
            {
                EntityHitImpactEffect.Spawn(
                    info.HitPoint,
                    direction,
                    definition.Visual);
            }
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, Vector2.zero);
        }

        public void TakeDamage(float amount, Vector2 impulse)
        {
            if (amount <= 0f || state == BrainState.Dead)
                return;

            health = Mathf.Max(0f, health - amount);
            if (movement != null && impulse.sqrMagnitude > 0.001f)
                movement.AddImpulse(impulse);

            if (health <= 0f)
            {
                Die();
                return;
            }

            hurtUntil = Time.time + 0.18f;
            visual.SetState(EntityVisualState.Hurt, true);

            EntityDisposition disposition = definition.GetDisposition();
            if (disposition == EntityDisposition.Passive)
            {
                fleeUntil = Time.time + 4.5f;
            }
            else
            {
                aggroUntil = Time.time + Mathf.Max(0.5f, definition.Combat.AggroAfterHitSeconds);
            }
        }

        private void Die()
        {
            if (state == BrainState.Dead)
                return;

            state = BrainState.Dead;
            attackPending = false;
            if (hitCollider != null)
                hitCollider.enabled = false;

            visual.SetState(EntityVisualState.Death, true);
            DropLoot();
            Game.Achievements.AchievementRuntime.NotifyMobKill(DefinitionId);

            if (Died != null)
                Died(this);

            float deathDelay = definition != null && definition.Visual != null
                ? Mathf.Max(0.05f, definition.Visual.DeathDespawnDelay)
                : 0.45f;
            Destroy(gameObject, deathDelay);
        }

        private void DropLoot()
        {
            if (definition.Loot == null || ItemDropSpawner.Instance == null)
                return;

            for (int i = 0; i < definition.Loot.Length; i++)
            {
                EntityLootEntry entry = definition.Loot[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                    continue;

                if (UnityEngine.Random.value > Mathf.Clamp01(entry.Chance))
                    continue;

                int min = Mathf.Max(0, entry.Min);
                int max = Mathf.Max(min, entry.Max);
                int baseCount = UnityEngine.Random.Range(min, max + 1);
                float exact = baseCount * lootMultiplier;
                int count = Mathf.FloorToInt(exact);
                if (UnityEngine.Random.value < exact - count)
                    count++;

                if (count > 0)
                {
                    ItemDropSpawner.Instance.SpawnFromPlayer(
                        entry.ItemId,
                        count,
                        transform.position);
                }
            }
        }

        private void UpdateVisual()
        {
            if (visual == null)
                return;

            if (state == BrainState.Attack && Time.time < attackEndAt)
            {
                visual.SetState(EntityVisualState.Attack);
                float phase = Mathf.InverseLerp(attackExecuteAt - 0.2f, attackEndAt, Time.time);
                visual.AimHeldItem(attackDirection, phase);
                return;
            }

            if (Time.time < hurtUntil)
            {
                visual.SetState(EntityVisualState.Hurt);
                return;
            }

            Vector2 velocity = movement != null ? movement.Velocity : Vector2.zero;
            if (velocity.sqrMagnitude > 0.08f)
                visual.SetState(EntityVisualState.Walk);
            else
                visual.SetState(EntityVisualState.Idle);

            if (player != null && heldWeapon != null && IsRangedWeapon())
            {
                Vector2 aim = (Vector2)player.position - (Vector2)transform.position;
                visual.AimHeldItem(aim, 0f);
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
