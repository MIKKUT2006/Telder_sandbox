using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Game.Items;
using Game.Content;
using Game.Gameplay.Effects;
using Game.PlayerStats;

namespace Game.Gameplay.Cooking
{
    // Self-contained canonical IDs preserve recipe variants through every existing
    // inventory/chest/drop/network save path, without a separate recipe database.
    public static class StewRecipe
    {
        public const string Prefix = "game:stew_v1_";
        public static bool IsStew(string id) => id != null && (id.StartsWith(Prefix, StringComparison.Ordinal) || id == "game:stew");
        public static string Create(FoodMetadataRegistry.FoodMetadata a, FoodMetadataRegistry.FoodMetadata b, FoodMetadataRegistry.FoodMetadata c)
        {
            if (a == null || b == null || c == null)
                return null;
            float hunger = .5f * (a.HungerRestore + b.HungerRestore + c.HungerRestore);
            float health = .5f * (a.HealthRestore + b.HealthRestore + c.HealthRestore);
            var effects = new SortedDictionary<int, FoodEffectDefinition>();
            foreach (var food in new[]
            {
                a,
                b,
                c
            }

            )
                if (food.Effects != null)
                    foreach (var e in food.Effects)
                    {
                        if (e == null || !e.TryType(out var type) || e.Duration <= 0 || float.IsNaN(e.Duration))
                            continue;
                        int key = (int)type;
                        if (!effects.TryGetValue(key, out var merged))
                            effects[key] = merged = new FoodEffectDefinition
                            {
                                Type = type.ToString(),
                                Duration = 0,
                                Level = 1
                            };
                        merged.Level = Mathf.Max(merged.Level, Mathf.Clamp(e.Level, 1, 10));
                        merged.Duration += .6f * Mathf.Clamp(e.Duration, 0, 86400);
                    }

            var id = new StringBuilder(Prefix).Append(Quantize(hunger)).Append('_').Append(Quantize(health));
            foreach (var pair in effects)
                id.Append('_').Append(pair.Key).Append('l').Append(pair.Value.Level).Append('t').Append(Mathf.Max(1, Quantize(pair.Value.Duration)));
            return id.ToString();
        }

        private static int Quantize(float value) => float.IsNaN(value) ? 0 : Mathf.RoundToInt(Mathf.Clamp(value, 0, 86400) * 100);
        public static bool TryRead(string id, out FoodMetadataRegistry.FoodMetadata food)
        {
            food = null;
            if (id == null || id.Length > 256 || !id.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            string[] parts = id.Substring(Prefix.Length).Split('_');
            if (parts.Length < 2 || parts.Length > 8 || !Number(parts[0], out int hunger) || !Number(parts[1], out int health))
                return false;
            var effects = new List<FoodEffectDefinition>();
            int previous = -1;
            for (int i = 2; i < parts.Length; i++)
            {
                int l = parts[i].IndexOf('l'), t = parts[i].IndexOf('t');
                if (l <= 0 || t <= l || !Number(parts[i].Substring(0, l), out int type) || type <= previous || type > 5 || !Number(parts[i].Substring(l + 1, t - l - 1), out int level) || level < 1 || level > 10 || !Number(parts[i].Substring(t + 1), out int duration) || duration <= 0)
                    return false;
                previous = type;
                effects.Add(new FoodEffectDefinition { Type = ((FoodEffectType)type).ToString(), Level = level, Duration = duration / 100f });
            }

            food = new FoodMetadataRegistry.FoodMetadata
            {
                ID = id,
                EatTime = 5,
                HungerRestore = hunger / 100f,
                HealthRestore = health / 100f,
                Effects = effects.ToArray()
            };
            return true;
        }

        private static bool Number(string s, out int n)
        {
            n = 0;
            return !string.IsNullOrEmpty(s) && (s.Length == 1 || s[0] != '0') && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out n) && n >= 0 && n <= 8640000;
        }

        public static bool TryItem(ContentID id, out ItemDefinition item)
        {
            item = null;
            if (!TryRead(id.ToString(), out var food))
                return false;
            item = new ItemDefinition
            {
                ID = id,
                Name = "Похлёбка",
                Texture = "stew",
                MaxStack = 15,
                Type = ItemType.Food,
                Tags = new[]
                {
                    "eat"
                },
                EatTime = 5,
                HungerRestore = food.HungerRestore,
                HealthRestore = food.HealthRestore,
                Effects = food.Effects
            };
            return true;
        }
    }
}
