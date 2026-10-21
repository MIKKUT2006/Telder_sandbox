using System.Collections.Generic;
using UnityEngine;
using Game.Entities;
using Game.World;
using Game.World.Items;
using Game.GameplaySystems.Interactions;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponProjectile :
        MonoBehaviour
    {
        private static readonly Stack<
            WeaponProjectile
        > Pool =
            new Stack<
                WeaponProjectile
            >();

        private SpriteRenderer renderer2D;

        private Vector2 velocity;

        private float gravity;

        private float lifetime;

        private float damage;

        private float knockback;

        private bool rotateToVelocity;

        private LayerMask collisionMask;

        private Transform owner;

        private bool alive;
        private bool isArrow;
        private Vector2 trailPosition;
        private string recoverableArrowId;

        public static WeaponProjectile Spawn(
            Vector2 position,
            Vector2 initialVelocity,
            WeaponMetadata weapon,
            Transform owner,
            LayerMask worldMask,
            LayerMask targetMask,
            float power = 1f,
            float damageMultiplier = 1f,
            float flatDamageBonus = 0f)
        {
            WeaponProjectile projectile =
                Pool.Count > 0
                    ? Pool.Pop()
                    : CreateNew();

            if (projectile == null)
                projectile = CreateNew();

            projectile.gameObject.SetActive(
                true
            );

            projectile.Activate(
                position,
                initialVelocity,
                weapon,
                owner,
                worldMask,
                targetMask,
                power,
                damageMultiplier,
                flatDamageBonus
            );

            return projectile;
        }

        private static WeaponProjectile CreateNew()
        {
            GameObject go =
                new GameObject(
                    "WeaponProjectile"
                );

            SpriteRenderer renderer =
                go.AddComponent<
                    SpriteRenderer
                >();

            WeaponProjectile projectile =
                go.AddComponent<
                    WeaponProjectile
                >();

            projectile.renderer2D =
                renderer;

            return projectile;
        }

        private void Awake()
        {
            if (renderer2D == null)
            {
                renderer2D =
                    GetComponent<
                        SpriteRenderer
                    >();
            }
        }

        private void Activate(
            Vector2 position,
            Vector2 initialVelocity,
            WeaponMetadata weapon,
            Transform ownerTransform,
            LayerMask worldMask,
            LayerMask targetMask,
            float power,
            float damageMultiplier,
            float flatDamageBonus)
        {
            alive = true;

            transform.position =
                position;

            velocity =
                initialVelocity;

            gravity =
                weapon.ProjectileGravity;

            lifetime =
                weapon.ProjectileLifetime;

            float p =
                Mathf.Clamp01(
                    power
                );

            damage =
                (weapon.Damage + Mathf.Max(0f, flatDamageBonus)) *
                Game.Gameplay.Effects.PlayerFoodEffects.Strength(owner) *
                Mathf.Max(0f, damageMultiplier) *
                weapon.ProjectileDamageMultiplier *
                p;

            knockback =
                weapon.Knockback *
                Mathf.Lerp(
                    0.45f,
                    1f,
                    p
                );

            rotateToVelocity =
                weapon.RotateProjectileToVelocity;

            owner =
                ownerTransform;

            collisionMask =
                worldMask |
                targetMask;

            string projectileItem =
                string.IsNullOrWhiteSpace(
                    weapon.ProjectileItem)
                    ? weapon.AmmoItem
                    : weapon.ProjectileItem;

            isArrow = weapon.GetKind() == WeaponKind.Bow;
            trailPosition = position;
            recoverableArrowId = isArrow
                ? (!string.IsNullOrWhiteSpace(weapon.AmmoItem) ? weapon.AmmoItem : projectileItem)
                : null;

            renderer2D.sprite =
                ItemSpriteResolver.Resolve(
                    projectileItem
                );

            renderer2D.sortingLayerName =
                weapon.ProjectileSortingLayer;

            renderer2D.sortingOrder =
                weapon.ProjectileOrder;

            ApplyWorldSize(
                weapon
            );

            UpdateRotation();
        }

        private void FixedUpdate()
        {
            if (!alive)
                return;

            float dt =
                Time.fixedDeltaTime;

            lifetime -=
                dt;

            if (lifetime <= 0f)
            {
                Despawn();
                return;
            }

            if (gravity != 0f)
            {
                velocity +=
                    Vector2.down *
                    gravity *
                    dt;
            }

            Vector2 start =
                transform.position;

            Vector2 movement =
                velocity *
                dt;

            float distance =
                movement.magnitude;

            if (distance > 0.00001f)
            {
                Vector2 direction =
                    movement /
                    distance;

                Vector2 endPosition = start + movement;

                // EntityActor hit detection is intentionally independent from
                // collisionMask/targetMask. Older PlayerWeaponController components
                // may have a serialized mask that predates the enemy system, so a
                // Physics2D-only projectile could fly through the enemy hitbox.
                WeaponDamageInfo entityProbe =
                    new WeaponDamageInfo(
                        damage,
                        knockback,
                        direction,
                        endPosition,
                        owner != null ? owner.gameObject : null
                    );

                EntityActor entityTarget;
                Vector2 entityHitPoint;
                float entityHitT;
                bool hasEntityHit =
                    WeaponDamageUtility.TryGetFirstActiveEntityHitOnSegment(
                        start,
                        endPosition,
                        entityProbe,
                        out entityTarget,
                        out entityHitPoint,
                        out entityHitT
                    );

                // Still respect solid world geometry in front of the entity. If an
                // arrow crosses both a wall and an enemy during one FixedUpdate, the
                // wall must win when it is closer.
                float physicalDistance = hasEntityHit
                    ? Mathf.Min(distance, distance * entityHitT + 0.001f)
                    : distance;

                // Collision geometry is generated asynchronously. Check the tile
                // world itself, not only Unity colliders / serialized layer masks.
                // This also catches closed furniture doors.
                if (TryHitTerrain(start, direction, physicalDistance, out Vector2 wallHit))
                {
                    transform.position = wallHit;
                    EmbedArrowOrDespawn(wallHit, direction);
                    return;
                }

                RaycastHit2D[] hits =
                    WeaponDamageUtility.RaycastAllIncludingTriggers(
                        start,
                        direction,
                        physicalDistance,
                        collisionMask
                    );

                for (int i = 0;
                     i < hits.Length;
                     i++)
                {
                    Collider2D collider =
                        hits[i].collider;

                    if (collider == null)
                        continue;

                    if (owner != null &&
                        (
                            collider.transform == owner ||
                            collider.transform.IsChildOf(owner)
                        ))
                    {
                        continue;
                    }

                    // EntityActor is handled by the logical swept hit test above.
                    // This prevents layer masks and trigger settings from deciding
                    // whether an arrow can damage an enemy.
                    if (collider.GetComponentInParent<EntityActor>() != null)
                        continue;

                    WeaponDamageInfo probeInfo =
                        new WeaponDamageInfo(
                            damage,
                            knockback,
                            direction,
                            hits[i].point,
                            owner != null ? owner.gameObject : null
                        );

                    if (WeaponDamageUtility.IsExplicitlyIgnored(
                            collider,
                            probeInfo))
                    {
                        continue;
                    }

                    transform.position =
                        hits[i].point;

                    bool dealtDamage =
                        WeaponDamageUtility.TryDealDamage(
                            collider,
                            probeInfo
                        );

                    // A real damage target or solid collider consumes the arrow.
                    // Harmless triggers (pickups, sensors, etc.) do not.
                    if (dealtDamage || !collider.isTrigger)
                    {
                        if (!dealtDamage && !collider.isTrigger)
                            EmbedArrowOrDespawn(hits[i].point, direction);
                        else
                            Despawn();
                        return;
                    }
                }

                if (hasEntityHit && entityTarget != null)
                {
                    transform.position = entityHitPoint;

                    WeaponDamageInfo entityInfo =
                        new WeaponDamageInfo(
                            damage,
                            knockback,
                            direction,
                            entityHitPoint,
                            owner != null ? owner.gameObject : null
                        );

                    if (entityTarget.CanReceiveWeaponDamage(entityInfo))
                    {
                        entityTarget.ReceiveWeaponDamage(entityInfo);
                        Despawn();
                        return;
                    }
                }
            }

            transform.position =
                start +
                movement;

            RecordArrowTravel();
            UpdateRotation();
        }

        private bool TryHitTerrain(Vector2 start, Vector2 direction,
            float maxDistance, out Vector2 point)
        {
            point = default(Vector2);
            WorldManager manager = WorldManager.Instance;
            if (manager == null || manager.GetWorldCollision() == null ||
                manager.GetLoader() == null || manager.GetSettings() == null)
                return false;

            int steps = Mathf.Max(1, Mathf.CeilToInt(maxDistance / 0.075f));
            float length = maxDistance / steps;
            int chunkSize = Mathf.Max(1, manager.GetSettings().ChunkSize);
            var blocks = manager.GetWorldCollision();
            var loader = manager.GetLoader();
            for (int i = 1; i <= steps; i++)
            {
                Vector2 sample = start + direction * (length * i);
                int x = Mathf.FloorToInt(sample.x);
                int y = Mathf.FloorToInt(sample.y);
                if (!loader.IsChunkLoaded(Mathf.FloorToInt(sample.x / chunkSize),
                    Mathf.FloorToInt(sample.y / chunkSize)))
                    continue; // Never mistake not-yet-generated terrain for a wall.

                if (blocks.IsSolid(x, y) ||
                    InteractiveBlockRuntime.IntersectsClosedFurniture(
                        new Rect(sample.x - 0.01f, sample.y - 0.01f, 0.02f, 0.02f)))
                {
                    // Put the arrow tip just inside the solid tile so it can
                    // verify its support on later physics ticks.
                    point = sample;
                    return true;
                }
            }
            return false;
        }

        private void EmbedArrowOrDespawn(Vector2 point, Vector2 flightDirection)
        {
            if (isArrow && !string.IsNullOrWhiteSpace(recoverableArrowId))
            {
                ItemDropSpawner spawner = ItemDropSpawner.EnsureInstance();
                if (spawner != null)
                {
                    float angle = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
                    spawner.SpawnEmbedded(recoverableArrowId, point,
                        Quaternion.Euler(0f, 0f, angle));
                }
            }
            Despawn();
        }

        private void ApplyWorldSize(
            WeaponMetadata weapon)
        {
            Sprite sprite =
                renderer2D.sprite;

            if (sprite == null)
            {
                transform.localScale =
                    Vector3.one *
                    weapon.ProjectileScale;

                return;
            }

            float longest =
                Mathf.Max(
                    sprite.bounds.size.x,
                    sprite.bounds.size.y
                );

            if (longest <= 0.00001f)
                longest = 1f;

            float scale =
                weapon.ProjectileWorldSize /
                longest *
                weapon.ProjectileScale;

            transform.localScale =
                Vector3.one *
                scale;
        }

        private void UpdateRotation()
        {
            if (!rotateToVelocity ||
                velocity.sqrMagnitude <
                    0.000001f)
            {
                return;
            }

            float angle =
                Mathf.Atan2(
                    velocity.y,
                    velocity.x
                ) *
                Mathf.Rad2Deg;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }

        private void RecordArrowTravel()
        {
            if (isArrow && renderer2D != null)
                PixelArrowTrail.Record(trailPosition, transform.position, renderer2D.sortingLayerName, renderer2D.sortingOrder);
            trailPosition = transform.position;
        }

        private void Despawn()
        {
            if (!alive)
                return;

            RecordArrowTravel();
            alive = false;
            owner = null;
            velocity = Vector2.zero;
            isArrow = false;
            recoverableArrowId = null;

            if (renderer2D != null)
                renderer2D.sprite = null;

            gameObject.SetActive(
                false
            );

            Pool.Push(
                this
            );
        }
    }
}
