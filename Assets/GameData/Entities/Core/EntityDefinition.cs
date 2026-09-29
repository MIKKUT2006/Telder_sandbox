using System;
using UnityEngine;

namespace Game.Entities
{
    public enum EntityDisposition
    {
        Passive,
        Neutral,
        Aggressive
    }

    public enum EntityMovementKind
    {
        Ground,
        Flying,
        Jumping
    }

    [Serializable]
    public sealed class EntityStarScaling
    {
        [Range(1, 5)] public int MinStars = 1;
        [Range(1, 5)] public int MaxStars = 5;

        // Relative spawn weights for 1..5 stars.
        public float[] StarWeights = { 55f, 25f, 12f, 6f, 2f };

        // Multipliers use: 1 + (stars - 1) * value.
        [Min(0f)] public float HealthPerExtraStar = 0.35f;
        [Min(0f)] public float DamagePerExtraStar = 0.20f;
        [Min(0f)] public float LootPerExtraStar = 0.25f;

        public int RollStars()
        {
            int min = Mathf.Clamp(MinStars, 1, 5);
            int max = Mathf.Clamp(MaxStars, min, 5);

            float total = 0f;
            for (int star = min; star <= max; star++)
                total += Mathf.Max(0f, GetWeight(star));

            if (total <= 0.0001f)
                return UnityEngine.Random.Range(min, max + 1);

            float roll = UnityEngine.Random.value * total;
            for (int star = min; star <= max; star++)
            {
                roll -= Mathf.Max(0f, GetWeight(star));
                if (roll <= 0f)
                    return star;
            }

            return max;
        }

        public float HealthMultiplier(int stars)
        {
            return 1f + (Mathf.Clamp(stars, 1, 5) - 1) * Mathf.Max(0f, HealthPerExtraStar);
        }

        public float DamageMultiplier(int stars)
        {
            return 1f + (Mathf.Clamp(stars, 1, 5) - 1) * Mathf.Max(0f, DamagePerExtraStar);
        }

        public float LootMultiplier(int stars)
        {
            return 1f + (Mathf.Clamp(stars, 1, 5) - 1) * Mathf.Max(0f, LootPerExtraStar);
        }

        private float GetWeight(int stars)
        {
            int index = stars - 1;
            if (StarWeights == null || index < 0 || index >= StarWeights.Length)
                return 1f;

            return StarWeights[index];
        }
    }

    [Serializable]
    public sealed class EntitySpawnDefinition
    {
        public bool Enabled = true;
        [Min(0)] public int MaxAlive = 8;
        [Min(0.1f)] public float Weight = 1f;

        [Range(0f, 1f)] public float MinLight = 0f;
        [Range(0f, 1f)] public float MaxLight = 1f;

        [Min(0f)] public float MinDistanceFromPlayer = 12f;
        [Min(1f)] public float MaxDistanceFromPlayer = 38f;

        [Min(1)] public int RequiredWidth = 1;
        [Min(1)] public int RequiredHeight = 2;

        public string[] Biomes;
    }

    [Serializable]
    public sealed class EntityMovementDefinition
    {
        public string Type = "Ground";
        [Min(0f)] public float Speed = 2.5f;
        [Min(0f)] public float Acceleration = 18f;
        [Min(0f)] public float Gravity = 25f;
        [Min(0f)] public float MaxFallSpeed = 20f;
        [Min(0f)] public float JumpForce = 8f;
        [Min(0)] public int MaxStepUpCells = 1;
        [Min(0)] public int MaxJumpUpCells = 2;
        [Min(0)] public int MaxDropCells = 5;
        [Min(0.1f)] public float JumpingHopInterval = 0.85f;

        public EntityMovementKind GetKind()
        {
            EntityMovementKind parsed;
            return Enum.TryParse(Type, true, out parsed)
                ? parsed
                : EntityMovementKind.Ground;
        }
    }

    [Serializable]
    public sealed class EntityCombatDefinition
    {
        [Min(0f)] public float VisionRange = 12f;
        [Min(0f)] public float LoseTargetRange = 18f;
        [Min(0f)] public float BaseDamage = 8f;
        [Min(0.1f)] public float AttackRange = 1.45f;
        [Min(0.01f)] public float AttackCooldown = 1f;
        [Min(0f)] public float AggroAfterHitSeconds = 12f;
        public bool RequireLineOfSight = true;
    }

    [Serializable]
    public sealed class EntityEquipmentEntry
    {
        public string ItemId;
        [Range(0f, 1f)] public float Chance = 1f;
    }

    [Serializable]
    public sealed class EntityLootEntry
    {
        public string ItemId;
        [Min(0)] public int Min = 1;
        [Min(0)] public int Max = 1;
        [Range(0f, 1f)] public float Chance = 1f;
    }

    [Serializable]
    public sealed class EntityVisualDefinition
    {
        // ----- Sprite-frame mode -----
        // SpriteResource: Unity Resources path.
        // SpriteFile: path relative to Assets/GameData, e.g. ResourcePacks/Default/textures/Entity/slime.png.
        public string SpriteResource;
        public string SpriteFile;
        public string SpriteItem;
        public string Color = "#FFFFFF";
        public string SortingLayer = "Default";
        public int SortingOrder = 80;
        [Min(0.01f)] public float WorldWidth = 0.9f;
        public float OffsetX = 0f;
        public float OffsetY = 0f;

        public string[] IdleSprites;
        public string[] WalkSprites;
        public string[] AttackSprites;
        public string[] HurtSprites;
        public string[] DeathSprites;
        [Min(0.01f)] public float FramesPerSecond = 8f;

        // ----- Prefab / skeletal-animation mode -----
        // Put the prefab anywhere under a Resources folder, for example:
        // Assets/GameData/Resources/Entities/Skeleton.prefab
        // and set PrefabResource to "Entities/Skeleton".
        // The prefab may contain SpriteSkin/bones + Animator.
        public string PrefabResource;
        [Min(0.01f)] public float PrefabScale = 1f;
        public bool PrefabFacesRight = true;

        // Optional transform path inside the prefab. The held-item sprite becomes
        // a child of this transform, so it can follow a hand bone. Example:
        // "Armature/Body/RightArm/Hand/HeldItemAnchor". If empty, a transform
        // named HeldItemAnchor is searched recursively.
        public string HeldItemAnchorPath;
        public float HeldItemOffsetX = 0.35f;
        public float HeldItemOffsetY = 0.05f;
        [Min(0.01f)] public float HeldItemWorldSize = 0.72f;

        // Animator state names. These are used with Animator.Play/CrossFade, so
        // bone animation, Sprite Skin animation and ordinary Animator clips work
        // through the same entity AI state machine.
        public string AnimatorIdleState = "Idle";
        public string AnimatorWalkState = "Walk";
        public string AnimatorAttackState = "Attack";
        public string AnimatorHurtState = "Hurt";
        public string AnimatorDeathState = "Death";
        [Min(0f)] public float AnimatorCrossFade = 0.06f;
        [Min(0.05f)] public float DeathDespawnDelay = 0.45f;

        // ----- Hit impact -----
        // Small pixel fragments emitted exactly at the weapon/projectile impact point.
        // These defaults work even when the fields are omitted from an entity JSON.
        public bool HitEffectEnabled = true;
        public string HitEffectColor = "#2FC8FF";
        public string HitEffectSecondaryColor = "#DDF9FF";
        [Range(1, 24)] public int HitParticleCount = 8;
        [Min(0.01f)] public float HitParticleLifetime = 0.18f;
        [Min(0.01f)] public float HitParticleSpeed = 3.8f;
        [Min(0.005f)] public float HitParticleSize = 0.075f;
        [Range(0f, 180f)] public float HitParticleSpread = 72f;
    }

    [Serializable]
    public sealed class EntityDefinition
    {
        public string ID;
        public string Name;
        public bool Enabled = true;

        public string Behavior = "Aggressive";
        public EntityMovementDefinition Movement = new EntityMovementDefinition();
        public EntityCombatDefinition Combat = new EntityCombatDefinition();
        public EntitySpawnDefinition Spawn = new EntitySpawnDefinition();
        public EntityStarScaling Stars = new EntityStarScaling();
        public EntityVisualDefinition Visual = new EntityVisualDefinition();

        [Min(1f)] public float MaxHealth = 50f;
        [Min(0.1f)] public float ColliderWidth = 0.8f;
        [Min(0.1f)] public float ColliderHeight = 1.8f;
        [Min(0f)] public float WanderRadius = 8f;

        public EntityEquipmentEntry[] Equipment;
        public EntityLootEntry[] Loot;

        public EntityDisposition GetDisposition()
        {
            EntityDisposition parsed;
            return Enum.TryParse(Behavior, true, out parsed)
                ? parsed
                : EntityDisposition.Aggressive;
        }

        public void Normalize()
        {
            MaxHealth = Mathf.Max(1f, MaxHealth);
            ColliderWidth = Mathf.Max(0.1f, ColliderWidth);
            ColliderHeight = Mathf.Max(0.1f, ColliderHeight);
            WanderRadius = Mathf.Max(0f, WanderRadius);

            if (Movement == null) Movement = new EntityMovementDefinition();
            if (Combat == null) Combat = new EntityCombatDefinition();
            if (Spawn == null) Spawn = new EntitySpawnDefinition();
            if (Stars == null) Stars = new EntityStarScaling();
            if (Visual == null) Visual = new EntityVisualDefinition();

            Spawn.MinLight = Mathf.Clamp01(Spawn.MinLight);
            Spawn.MaxLight = Mathf.Clamp01(Spawn.MaxLight);
            if (Spawn.MaxLight < Spawn.MinLight)
            {
                float t = Spawn.MinLight;
                Spawn.MinLight = Spawn.MaxLight;
                Spawn.MaxLight = t;
            }

            Spawn.RequiredWidth = Mathf.Max(1, Spawn.RequiredWidth);
            Spawn.RequiredHeight = Mathf.Max(1, Spawn.RequiredHeight);
            Spawn.MaxAlive = Mathf.Max(0, Spawn.MaxAlive);
            Spawn.Weight = Mathf.Max(0.01f, Spawn.Weight);
            Spawn.MinDistanceFromPlayer = Mathf.Max(0f, Spawn.MinDistanceFromPlayer);
            Spawn.MaxDistanceFromPlayer = Mathf.Max(Spawn.MinDistanceFromPlayer + 1f, Spawn.MaxDistanceFromPlayer);

            Combat.VisionRange = Mathf.Max(0f, Combat.VisionRange);
            Combat.LoseTargetRange = Mathf.Max(Combat.VisionRange, Combat.LoseTargetRange);
            Combat.BaseDamage = Mathf.Max(0f, Combat.BaseDamage);
            Combat.AttackRange = Mathf.Max(0.1f, Combat.AttackRange);
            Combat.AttackCooldown = Mathf.Max(0.01f, Combat.AttackCooldown);

            Stars.MinStars = Mathf.Clamp(Stars.MinStars, 1, 5);
            Stars.MaxStars = Mathf.Clamp(Stars.MaxStars, Stars.MinStars, 5);

            Visual.HitParticleCount = Mathf.Clamp(Visual.HitParticleCount, 1, 24);
            Visual.HitParticleLifetime = Mathf.Max(0.03f, Visual.HitParticleLifetime);
            Visual.HitParticleSpeed = Mathf.Max(0.1f, Visual.HitParticleSpeed);
            Visual.HitParticleSize = Mathf.Max(0.01f, Visual.HitParticleSize);
            Visual.HitParticleSpread = Mathf.Clamp(Visual.HitParticleSpread, 0f, 180f);
        }
    }
}
