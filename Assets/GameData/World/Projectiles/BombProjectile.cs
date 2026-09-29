using System.Collections.Generic;
using UnityEngine;
using Game.Combat;
using Game.World.Explosions;

namespace Game.World.Projectiles
{
    [DisallowMultipleComponent]
    public sealed class BombProjectile : MonoBehaviour
    {
        private static readonly Stack<BombProjectile> Pool = new Stack<BombProjectile>();
        private static readonly HashSet<int> DamagedTargets = new HashSet<int>();

        private SpriteRenderer renderer2D;
        private Vector2 velocity;
        private float fuse;
        private float gravity;
        private float bounce;
        private float entityDamage;
        private float knockback;
        private int width;
        private int height;
        private bool destroyBlocks;
        private bool destroyBackground;
        private bool destroyFurniture;
        private Transform owner;
        private bool alive;

        public static BombProjectile Spawn(
            Vector2 position,
            Vector2 initialVelocity,
            WeaponMetadata weapon,
            Transform owner,
            float damageMultiplier = 1f,
            float flatDamageBonus = 0f)
        {
            BombProjectile bomb = Pool.Count > 0 ? Pool.Pop() : CreateNew();
            bomb.gameObject.SetActive(true);
            bomb.Activate(position, initialVelocity, weapon, owner, damageMultiplier, flatDamageBonus);
            return bomb;
        }

        private static BombProjectile CreateNew()
        {
            GameObject go = new GameObject("BombProjectile");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            BombProjectile result = go.AddComponent<BombProjectile>();
            result.renderer2D = sr;
            return result;
        }

        private void Awake()
        {
            if (renderer2D == null)
                renderer2D = GetComponent<SpriteRenderer>();
        }

        private void Activate(
            Vector2 position,
            Vector2 initialVelocity,
            WeaponMetadata weapon,
            Transform ownerTransform,
            float damageMultiplier,
            float flatDamageBonus)
        {
            alive = true;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            velocity = initialVelocity;
            owner = ownerTransform;

            fuse = Mathf.Max(0.05f, weapon.BombFuseTime);
            gravity = Mathf.Max(0f, weapon.BombGravity);
            bounce = Mathf.Clamp01(weapon.BombBounce);
            width = Mathf.Max(1, weapon.BombExplosionWidth);
            height = Mathf.Max(1, weapon.BombExplosionHeight);
            destroyBlocks = weapon.BombDestroyBlocks;
            destroyBackground = weapon.BombDestroyBackground;
            destroyFurniture = weapon.BombDestroyFurniture;
            entityDamage = (weapon.Damage + Mathf.Max(0f, flatDamageBonus)) * Mathf.Max(0f, damageMultiplier);
            knockback = Mathf.Max(0f, weapon.BombKnockback);

            string spriteItem = string.IsNullOrWhiteSpace(weapon.ProjectileItem)
                ? string.Empty
                : weapon.ProjectileItem;
            renderer2D.sprite = ItemSpriteResolver.Resolve(spriteItem);
            renderer2D.sortingLayerName = weapon.ProjectileSortingLayer;
            renderer2D.sortingOrder = weapon.ProjectileOrder;

            if (renderer2D.sprite != null)
            {
                float longest = Mathf.Max(renderer2D.sprite.bounds.size.x, renderer2D.sprite.bounds.size.y);
                if (longest <= 0.0001f) longest = 1f;
                transform.localScale = Vector3.one * (Mathf.Max(0.2f, weapon.ProjectileWorldSize) / longest);
            }
            else
            {
                transform.localScale = Vector3.one * 0.35f;
            }
        }

        private void FixedUpdate()
        {
            if (!alive)
                return;

            float dt = Time.fixedDeltaTime;
            fuse -= dt;
            if (fuse <= 0f)
            {
                ExplodeNow();
                return;
            }

            velocity += Vector2.down * gravity * dt;
            Vector2 start = transform.position;
            Vector2 movement = velocity * dt;
            float distance = movement.magnitude;

            if (distance > 0.0001f)
            {
                Vector2 direction = movement / distance;
                RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, distance + 0.08f, Physics2D.DefaultRaycastLayers);

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D collider = hits[i].collider;
                    if (collider == null)
                        continue;

                    if (owner != null && (collider.transform == owner || collider.transform.IsChildOf(owner)))
                        continue;

                    // Creatures/player do not physically stop the bomb. It damages them only on explosion.
                    if (collider.GetComponentInParent<Game.Entities.EntityActor>() != null ||
                        collider.GetComponentInParent<Game.PlayerStats.PlayerStats>() != null)
                    {
                        continue;
                    }

                    Vector2 normal = hits[i].normal.sqrMagnitude > 0.001f ? hits[i].normal : Vector2.up;
                    transform.position = hits[i].point + normal * 0.04f;
                    velocity = Vector2.Reflect(velocity, normal) * bounce;
                    if (Mathf.Abs(velocity.y) < 0.5f && normal.y > 0.4f)
                        velocity.y = 0f;
                    return;
                }
            }

            transform.position = start + movement;
            if (velocity.sqrMagnitude > 0.05f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void ExplodeNow()
        {
            Vector2 center = transform.position;

            if (destroyBlocks)
            {
                ExplosionSystem.ExplodeBox(
                    center,
                    width,
                    height,
                    true,
                    destroyBackground,
                    destroyFurniture);
            }
            else
            {
                ExplosionPixelVfx.Spawn(center, Mathf.Max(width, height) * 0.55f);
            }

            DamageEntities(center);
            Despawn();
        }

        private void DamageEntities(Vector2 center)
        {
            DamagedTargets.Clear();
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(width, height), 0f, Physics2D.DefaultRaycastLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i];
                if (collider == null)
                    continue;

                if (owner != null && (collider.transform == owner || collider.transform.IsChildOf(owner)))
                    continue;

                int key = WeaponDamageUtility.GetTargetKey(collider);
                if (key != 0 && !DamagedTargets.Add(key))
                    continue;

                Vector2 direction = ((Vector2)collider.bounds.center - center);
                if (direction.sqrMagnitude < 0.0001f)
                    direction = Vector2.up;

                WeaponDamageInfo info = new WeaponDamageInfo(
                    entityDamage,
                    knockback,
                    direction.normalized,
                    collider.bounds.ClosestPoint(center),
                    owner != null ? owner.gameObject : null);

                WeaponDamageUtility.TryDealDamage(collider, info);
            }
        }

        private void Despawn()
        {
            alive = false;
            owner = null;
            velocity = Vector2.zero;
            if (renderer2D != null)
                renderer2D.sprite = null;
            gameObject.SetActive(false);
            Pool.Push(this);
        }
    }
}
