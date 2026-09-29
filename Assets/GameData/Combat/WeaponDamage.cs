using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Game.Entities;

namespace Game.Combat
{
    public readonly struct WeaponDamageInfo
    {
        public readonly float Damage;
        public readonly float Knockback;
        public readonly Vector2 Direction;
        public readonly Vector2 HitPoint;
        public readonly GameObject Source;

        public WeaponDamageInfo(
            float damage,
            float knockback,
            Vector2 direction,
            Vector2 hitPoint,
            GameObject source)
        {
            Damage = damage;
            Knockback = knockback;
            Direction = direction;
            HitPoint = hitPoint;
            Source = source;
        }
    }

    public interface IWeaponDamageable
    {
        void ReceiveWeaponDamage(
            WeaponDamageInfo info
        );
    }

    /// <summary>
    /// Optional target-side filter. Useful for factions/friendly-fire without
    /// teaching every weapon/projectile about entity types.
    /// </summary>
    public interface IWeaponDamageGate
    {
        bool CanReceiveWeaponDamage(
            WeaponDamageInfo info
        );
    }

    public static class WeaponDamageUtility
    {
        private static readonly string[] MethodNames =
        {
            "TakeDamage",
            "ApplyDamage",
            "Damage"
        };

        private static readonly Dictionary<
            Type,
            MethodInfo
        > Cache =
            new Dictionary<
                Type,
                MethodInfo
            >();


        public static int OverlapCircleIncludingTriggers(
            Vector2 center,
            float radius,
            Collider2D[] results,
            LayerMask mask)
        {
            if (results == null || results.Length == 0)
                return 0;

            bool previous = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            try
            {
                return Physics2D.OverlapCircleNonAlloc(
                    center,
                    Mathf.Max(0.001f, radius),
                    results,
                    mask);
            }
            finally
            {
                Physics2D.queriesHitTriggers = previous;
            }
        }

        public static RaycastHit2D[] RaycastAllIncludingTriggers(
            Vector2 origin,
            Vector2 direction,
            float distance,
            LayerMask mask)
        {
            bool previous = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            try
            {
                return Physics2D.RaycastAll(
                    origin,
                    direction,
                    distance,
                    mask);
            }
            finally
            {
                Physics2D.queriesHitTriggers = previous;
            }
        }

        public static int GetTargetKey(
            Collider2D collider)
        {
            if (collider == null)
                return 0;

            if (collider.attachedRigidbody != null)
            {
                return collider
                    .attachedRigidbody
                    .gameObject
                    .GetInstanceID();
            }

            return collider
                .transform
                .root
                .gameObject
                .GetInstanceID();
        }

        public static bool TryDealDamage(
            Collider2D collider,
            WeaponDamageInfo info)
        {
            if (collider == null)
                return false;

            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<
                    MonoBehaviour
                >(
                    true
                );

            for (int i = 0; i < behaviours.Length; i++)
            {
                IWeaponDamageGate gate =
                    behaviours[i] as IWeaponDamageGate;

                if (gate != null &&
                    !gate.CanReceiveWeaponDamage(info))
                {
                    return false;
                }
            }

            for (int i = 0;
                 i < behaviours.Length;
                 i++)
            {
                if (behaviours[i] is
                    IWeaponDamageable damageable)
                {
                    damageable.ReceiveWeaponDamage(
                        info
                    );

                    ApplyKnockback(
                        collider,
                        info
                    );

                    return true;
                }
            }

            for (int i = 0;
                 i < behaviours.Length;
                 i++)
            {
                MonoBehaviour behaviour =
                    behaviours[i];

                if (behaviour == null)
                    continue;

                MethodInfo method =
                    FindFloatDamageMethod(
                        behaviour.GetType()
                    );

                if (method == null)
                    continue;

                method.Invoke(
                    behaviour,
                    new object[]
                    {
                        info.Damage
                    }
                );

                ApplyKnockback(
                    collider,
                    info
                );

                return true;
            }

            return false;
        }

        public static bool IsExplicitlyIgnored(
            Collider2D collider,
            WeaponDamageInfo info)
        {
            if (collider == null)
                return false;

            MonoBehaviour[] behaviours =
                collider.GetComponentsInParent<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                IWeaponDamageGate gate =
                    behaviours[i] as IWeaponDamageGate;

                if (gate != null &&
                    !gate.CanReceiveWeaponDamage(info))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Direct runtime-entity query that does not depend on Unity LayerMasks,
        /// trigger query settings or transform/physics synchronization. EntityActor
        /// owns its logical damage bounds, so player melee remains reliable even when
        /// the project uses custom physics layers for the world.
        /// </summary>
        public static int DamageActiveEntitiesInCircle(
            Vector2 center,
            float radius,
            WeaponDamageInfo info,
            HashSet<int> alreadyHit = null)
        {
            float r = Mathf.Max(0.001f, radius);
            float r2 = r * r;
            int damaged = 0;

            IReadOnlyList<EntityActor> actors = EntityActor.ActiveActors;
            for (int i = actors.Count - 1; i >= 0; i--)
            {
                EntityActor actor = actors[i];
                if (actor == null || !actor.IsDamageTargetActive)
                    continue;

                int key = actor.DamageTargetKey;
                if (alreadyHit != null && key != 0 && alreadyHit.Contains(key))
                    continue;

                Rect rect = actor.GetDamageBounds();
                float closestX = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
                float closestY = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
                float dx = center.x - closestX;
                float dy = center.y - closestY;

                if (dx * dx + dy * dy > r2)
                    continue;

                Vector2 hitPoint = new Vector2(closestX, closestY);
                WeaponDamageInfo targetInfo = new WeaponDamageInfo(
                    info.Damage,
                    info.Knockback,
                    info.Direction,
                    hitPoint,
                    info.Source);

                if (!actor.CanReceiveWeaponDamage(targetInfo))
                    continue;

                actor.ReceiveWeaponDamage(targetInfo);
                if (alreadyHit != null && key != 0)
                    alreadyHit.Add(key);
                damaged++;
            }

            return damaged;
        }

        public static int DamageActiveEntitiesAlongSegment(
            Vector2 start,
            Vector2 end,
            float radius,
            WeaponDamageInfo info,
            HashSet<int> alreadyHit = null)
        {
            float padding = Mathf.Max(0f, radius);
            int damaged = 0;
            IReadOnlyList<EntityActor> actors = EntityActor.ActiveActors;

            for (int i = actors.Count - 1; i >= 0; i--)
            {
                EntityActor actor = actors[i];
                if (actor == null || !actor.IsDamageTargetActive)
                    continue;

                int key = actor.DamageTargetKey;
                if (alreadyHit != null && key != 0 && alreadyHit.Contains(key))
                    continue;

                if (!actor.CanReceiveWeaponDamage(info))
                    continue;

                Rect rect = actor.GetDamageBounds();
                rect = new Rect(
                    rect.xMin - padding,
                    rect.yMin - padding,
                    rect.width + padding * 2f,
                    rect.height + padding * 2f);

                float t;
                if (!SegmentIntersectsRect(start, end, rect, out t))
                    continue;

                Vector2 hitPoint = Vector2.Lerp(start, end, Mathf.Clamp01(t));
                WeaponDamageInfo targetInfo = new WeaponDamageInfo(
                    info.Damage,
                    info.Knockback,
                    info.Direction,
                    hitPoint,
                    info.Source);

                actor.ReceiveWeaponDamage(targetInfo);
                if (alreadyHit != null && key != 0)
                    alreadyHit.Add(key);
                damaged++;
            }

            return damaged;
        }

        /// <summary>
        /// Finds the first logical EntityActor hit by a swept projectile segment.
        /// This is deliberately independent of Physics2D layer masks. It fixes the
        /// common case where enemy hitboxes live on Default while an older weapon
        /// controller has a serialized targetMask that does not include Default.
        /// </summary>
        public static bool TryGetFirstActiveEntityHitOnSegment(
            Vector2 start,
            Vector2 end,
            WeaponDamageInfo probeInfo,
            out EntityActor actor,
            out Vector2 hitPoint,
            out float normalizedTime,
            float padding = 0.035f)
        {
            actor = null;
            hitPoint = end;
            normalizedTime = 1f;

            float bestT = float.PositiveInfinity;
            IReadOnlyList<EntityActor> actors = EntityActor.ActiveActors;

            for (int i = actors.Count - 1; i >= 0; i--)
            {
                EntityActor candidate = actors[i];
                if (candidate == null || !candidate.IsDamageTargetActive)
                    continue;

                if (!candidate.CanReceiveWeaponDamage(probeInfo))
                    continue;

                Rect rect = candidate.GetDamageBounds();
                float p = Mathf.Max(0f, padding);
                rect = new Rect(
                    rect.xMin - p,
                    rect.yMin - p,
                    rect.width + p * 2f,
                    rect.height + p * 2f);

                float t;
                if (!SegmentIntersectsRect(start, end, rect, out t))
                    continue;

                if (t < bestT)
                {
                    bestT = t;
                    actor = candidate;
                }
            }

            if (actor == null)
                return false;

            normalizedTime = Mathf.Clamp01(bestT);
            hitPoint = Vector2.Lerp(start, end, normalizedTime);
            return true;
        }

        private static bool SegmentIntersectsRect(
            Vector2 start,
            Vector2 end,
            Rect rect,
            out float tHit)
        {
            Vector2 delta = end - start;
            float tMin = 0f;
            float tMax = 1f;

            if (!ClipSegmentAxis(start.x, delta.x, rect.xMin, rect.xMax, ref tMin, ref tMax) ||
                !ClipSegmentAxis(start.y, delta.y, rect.yMin, rect.yMax, ref tMin, ref tMax))
            {
                tHit = 0f;
                return false;
            }

            tHit = Mathf.Clamp01(tMin);
            return true;
        }

        private static bool ClipSegmentAxis(
            float start,
            float delta,
            float min,
            float max,
            ref float tMin,
            ref float tMax)
        {
            if (Mathf.Abs(delta) < 0.000001f)
                return start >= min && start <= max;

            float inv = 1f / delta;
            float a = (min - start) * inv;
            float b = (max - start) * inv;
            if (a > b)
            {
                float temp = a;
                a = b;
                b = temp;
            }

            if (a > tMin)
                tMin = a;
            if (b < tMax)
                tMax = b;

            return tMin <= tMax && tMax >= 0f && tMin <= 1f;
        }

        private static MethodInfo FindFloatDamageMethod(
            Type type)
        {
            if (Cache.TryGetValue(
                    type,
                    out MethodInfo cached))
            {
                return cached;
            }

            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            MethodInfo found =
                null;

            for (int i = 0;
                 i < MethodNames.Length;
                 i++)
            {
                found =
                    type.GetMethod(
                        MethodNames[i],
                        flags,
                        null,
                        new[]
                        {
                            typeof(float)
                        },
                        null
                    );

                if (found != null)
                    break;
            }

            Cache[type] =
                found;

            return found;
        }

        private static void ApplyKnockback(
            Collider2D collider,
            WeaponDamageInfo info)
        {
            if (info.Knockback <= 0f)
                return;

            Rigidbody2D body =
                collider.attachedRigidbody;

            if (body == null ||
                body.bodyType !=
                RigidbodyType2D.Dynamic)
            {
                return;
            }

            body.AddForce(
                info.Direction.normalized *
                info.Knockback,
                ForceMode2D.Impulse
            );
        }
    }
}
