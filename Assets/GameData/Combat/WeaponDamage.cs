using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
