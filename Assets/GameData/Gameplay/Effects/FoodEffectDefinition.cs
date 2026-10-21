using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Effects
{
    public enum FoodEffectType
    {
        Freeze,
        Poison,
        Speed,
        HighJump,
        Strength,
        Defense
    }

    [Serializable]
    public sealed class FoodEffectDefinition
    {
        // String names keep item JSON readable with Unity's JsonUtility.
        public string Type = "Speed";
        public float Duration = 30f;
        public int Level = 1;
        public FoodEffectDefinition Copy(float durationScale = 1f) => new FoodEffectDefinition
        {
            Type = Type,
            Duration = Duration * durationScale,
            Level = Level
        };
        public bool TryType(out FoodEffectType type) => Enum.TryParse(Type, true, out type) && Enum.IsDefined(typeof(FoodEffectType), type);
    }

    [Serializable]
    public sealed class ActiveFoodEffect
    {
        public FoodEffectType Type;
        public int Level;
        public float Remaining;
        public float Total;
        public float Tick;
    }

    // Pure timing/rules shared by movement, combat, food and the HUD.
    [Serializable]
    public sealed class FoodEffectState
    {
        public List<ActiveFoodEffect> Entries = new List<ActiveFoodEffect>();
        public int Level(FoodEffectType type)
        {
            for (int i = 0; i < Entries.Count; i++)
                if (Entries[i].Type == type && Entries[i].Remaining > 0)
                    return Entries[i].Level;
            return 0;
        }

        public void Add(FoodEffectDefinition effect)
        {
            if (effect == null || !effect.TryType(out var type) || float.IsNaN(effect.Duration) || effect.Duration <= 0)
                return;
            int level = Mathf.Clamp(effect.Level, 1, 10);
            float duration = Mathf.Clamp(effect.Duration, .05f, 86400);
            ActiveFoodEffect entry = Entries.Find(e => e.Type == type);
            if (entry == null)
            {
                entry = new ActiveFoodEffect
                {
                    Type = type
                };
                Entries.Add(entry);
            }

            // A weaker dose never shortens or weakens an already active dose.
            entry.Level = Mathf.Max(entry.Level, level);
            entry.Remaining = Mathf.Max(entry.Remaining, duration);
            entry.Total = entry.Remaining;
        }

        public float Advance(float seconds)
        {
            if (float.IsNaN(seconds) || seconds <= 0)
                return 0;
            float poison = 0;
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var e = Entries[i];
                float active = Mathf.Min(seconds, e.Remaining);
                if (e.Type == FoodEffectType.Poison)
                {
                    e.Tick += active;
                    int ticks = Mathf.FloorToInt(e.Tick);
                    e.Tick -= ticks;
                    poison += ticks * Mathf.Clamp(e.Level, 1, 10) * 2f;
                }

                e.Remaining -= seconds;
                if (e.Remaining <= 0)
                    Entries.RemoveAt(i);
            }

            return poison;
        }

        public float Speed => (1f + .2f * Level(FoodEffectType.Speed)) / (1f + .6f * Level(FoodEffectType.Freeze));
        public float Jump => (1f + .25f * Level(FoodEffectType.HighJump)) / (1f + .35f * Level(FoodEffectType.Freeze));
        public float Strength => 1f + .25f * Level(FoodEffectType.Strength);
        public float Defense => 1f / (1f + .2f * Level(FoodEffectType.Defense));
    }
}
