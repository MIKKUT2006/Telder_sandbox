using System;
using UnityEngine;

namespace Game.Combat
{
    public enum WeaponKind
    {
        None,
        Sword,
        Bow,
        Gun,
        Spear
    }

    public enum SwordAttackKind
    {
        Swing,
        Thrust
    }

    [Serializable]
    public sealed class WeaponMetadata
    {
        public string Kind = "Sword";

        public float Damage = 10f;

        public float AttackSpeed = 1.5f;

        public float Knockback = 2f;

        public float Range = 1.2f;

        public float ArmAimOffset = 0f;

        public bool UseBackArm = false;

        public float BackArmAimOffset = 0f;

        public bool TrackCursorDuringAttack = true;

        public bool FistPointsConfigured = false;

        public Vector2 FrontFistPointLocal =
            new Vector2(
                0.28f,
                0f
            );

        public Vector2 BackFistPointLocal =
            new Vector2(
                0.28f,
                0f
            );

        public string[] SwordPattern =
        {
            "Swing",
            "Thrust"
        };

        public float SwordSwingStartAngle = 100f;

        public float SwordSwingEndAngle = -30f;

        public float SwingArc = 130f;

        public float SwingReach = 1.25f;

        public float SwingHitRadius = 0.42f;

        public float ThrustDistance = 0.9f;

        public float ThrustHitRadius = 0.32f;

        public float SpearBaseReach = 1.4f;

        public float SpearThrustDistance = 1.55f;

        public float SpearHitRadius = 0.24f;

        public string AmmoItem = "";

        public int AmmoPerShot = 1;

        public string ProjectileItem = "";

        public int ProjectileCount = 1;

        public float ProjectileSpeed = 15f;

        public float ProjectileGravity = 0f;

        public float ProjectileLifetime = 8f;

        public float SpreadDegrees = 0f;

        public string SpreadMode = "Random";

        public bool RotateProjectileToVelocity = true;

        public float ProjectileWorldSize = 0.7f;

        public float ProjectileScale = 1f;

        public float MuzzleOffset = 0.45f;

        public float ProjectileDamageMultiplier = 1f;

        public float FireNormalizedTime = 0.12f;

        public string ProjectileSortingLayer = "Default";

        public int ProjectileOrder = 100;

        public float BowChargeTime = 0.5f;

        public float BowMinPower = 0.25f;

        public float BowDrawDistance = 0.65f;

        public float BowHeldItemFullDrawOffset = 0f;

        public float BowDrawArmStartAngle = 0f;

        public float BowDrawArmEndAngle = 180f;

        public float BowBackArmRelaxedAngle = -15f;

        public float BowBackArmFullDrawAngle = -75f;


        public WeaponKind GetKind()
        {
            if (
                Enum.TryParse(
                    Kind,
                    true,
                    out WeaponKind value
                )
            )
            {
                return value;
            }


            return
                WeaponKind.None;
        }


        public SwordAttackKind GetSwordPatternEntry(
            int index
        )
        {
            if (
                SwordPattern == null
                ||
                SwordPattern.Length == 0
            )
            {
                return
                    SwordAttackKind.Swing;
            }


            int safe =
                Mathf.Abs(
                    index
                )
                %
                SwordPattern.Length;


            if (
                Enum.TryParse(
                    SwordPattern[safe],
                    true,
                    out SwordAttackKind value
                )
            )
            {
                return value;
            }


            return
                SwordAttackKind.Swing;
        }


        public void Normalize()
        {
            Damage =
                Mathf.Max(
                    0f,
                    Damage
                );


            AttackSpeed =
                Mathf.Max(
                    0.01f,
                    AttackSpeed
                );


            Knockback =
                Mathf.Max(
                    0f,
                    Knockback
                );


            Range =
                Mathf.Max(
                    0f,
                    Range
                );


            SwingReach =
                Mathf.Max(
                    0f,
                    SwingReach
                );


            SwingHitRadius =
                Mathf.Max(
                    0.01f,
                    SwingHitRadius
                );


            ThrustDistance =
                Mathf.Max(
                    0f,
                    ThrustDistance
                );


            ThrustHitRadius =
                Mathf.Max(
                    0.01f,
                    ThrustHitRadius
                );


            SpearBaseReach =
                Mathf.Max(
                    0f,
                    SpearBaseReach
                );


            SpearThrustDistance =
                Mathf.Max(
                    0f,
                    SpearThrustDistance
                );


            SpearHitRadius =
                Mathf.Max(
                    0.01f,
                    SpearHitRadius
                );


            AmmoPerShot =
                Mathf.Max(
                    0,
                    AmmoPerShot
                );


            ProjectileCount =
                Mathf.Max(
                    1,
                    ProjectileCount
                );


            ProjectileSpeed =
                Mathf.Max(
                    0.01f,
                    ProjectileSpeed
                );


            ProjectileGravity =
                Mathf.Max(
                    0f,
                    ProjectileGravity
                );


            ProjectileLifetime =
                Mathf.Max(
                    0.05f,
                    ProjectileLifetime
                );


            SpreadDegrees =
                Mathf.Clamp(
                    SpreadDegrees,
                    0f,
                    180f
                );


            ProjectileWorldSize =
                Mathf.Max(
                    0.01f,
                    ProjectileWorldSize
                );


            ProjectileScale =
                Mathf.Max(
                    0.01f,
                    ProjectileScale
                );


            MuzzleOffset =
                Mathf.Max(
                    0f,
                    MuzzleOffset
                );


            ProjectileDamageMultiplier =
                Mathf.Max(
                    0f,
                    ProjectileDamageMultiplier
                );


            FireNormalizedTime =
                Mathf.Clamp01(
                    FireNormalizedTime
                );


            BowChargeTime =
                Mathf.Max(
                    0.01f,
                    BowChargeTime
                );


            BowDrawDistance =
                Mathf.Max(
                    0f,
                    BowDrawDistance
                );


            BowMinPower =
                Mathf.Clamp01(
                    BowMinPower
                );


            if (
                Mathf.Abs(
                    SwordSwingStartAngle
                )
                <
                0.001f
                &&
                Mathf.Abs(
                    SwordSwingEndAngle
                )
                <
                0.001f
            )
            {
                SwordSwingStartAngle =
                    100f;


                SwordSwingEndAngle =
                    -30f;
            }


            if (
                GetKind() ==
                WeaponKind.Bow
            )
            {
                UseBackArm =
                    true;
            }


            if (
                SwordPattern == null
                ||
                SwordPattern.Length == 0
            )
            {
                SwordPattern =
                    new[]
                    {
                        "Swing",
                        "Thrust"
                    };
            }


            if (
                string.IsNullOrWhiteSpace(
                    SpreadMode
                )
            )
            {
                SpreadMode =
                    "Random";
            }


            if (
                string.IsNullOrWhiteSpace(
                    ProjectileSortingLayer
                )
            )
            {
                ProjectileSortingLayer =
                    "Default";
            }
        }
    }
}
