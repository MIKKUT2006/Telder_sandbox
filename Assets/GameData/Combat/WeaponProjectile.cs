using System.Collections.Generic;
using UnityEngine;

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

        public static WeaponProjectile Spawn(
            Vector2 position,
            Vector2 initialVelocity,
            WeaponMetadata weapon,
            Transform owner,
            LayerMask worldMask,
            LayerMask targetMask,
            float power = 1f)
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
                power
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
            float power)
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
                weapon.Damage *
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

                RaycastHit2D[] hits =
                    Physics2D.RaycastAll(
                        start,
                        direction,
                        distance,
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
                            collider.transform ==
                                owner ||
                            collider.transform.IsChildOf(
                                owner
                            )
                        ))
                    {
                        continue;
                    }

                    transform.position =
                        hits[i].point;

                    WeaponDamageInfo info =
                        new WeaponDamageInfo(
                            damage,
                            knockback,
                            direction,
                            hits[i].point,
                            owner != null
                                ? owner.gameObject
                                : null
                        );

                    WeaponDamageUtility.TryDealDamage(
                        collider,
                        info
                    );

                    Despawn();
                    return;
                }
            }

            transform.position =
                start +
                movement;

            UpdateRotation();
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

        private void Despawn()
        {
            if (!alive)
                return;

            alive = false;
            owner = null;
            velocity = Vector2.zero;

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
