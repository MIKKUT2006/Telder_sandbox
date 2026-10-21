using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.PlayerStats
{
    public static class FoodMetadataRegistry
    {
        // =====================================================
        // DATA
        // =====================================================

        public sealed class FoodMetadata
        {
            public string ID;
            public float EatTime;
            public float HungerRestore;
            public float HealthRestore;
            public Game.Gameplay.Effects.FoodEffectDefinition[] Effects;
        }


        [Serializable]
        private class ItemFoodJson
        {
            public string ID;

            public string[] Tags;

            public float EatTime;

            public float HungerRestore;

            public float HealthRestore;
            public Game.Gameplay.Effects.FoodEffectDefinition[] Effects;
        }


        private static readonly Dictionary<string, FoodMetadata> Data =
            new Dictionary<string, FoodMetadata>(
                StringComparer.OrdinalIgnoreCase
            );


        private static bool loaded;


        // =====================================================
        // RESET
        // =====================================================

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration
        )]
        private static void ResetRuntime()
        {
            loaded = false;
            Data.Clear();
        }


        // =====================================================
        // PUBLIC API
        // =====================================================

        public static void EnsureLoaded()
        {
            if (loaded)
                return;

            Reload();
        }


        public static void Reload()
        {
            loaded = true;
            Data.Clear();


            string folder =
                Path.Combine(
                    Game.Content.DataPaths.GameDataFolder,
                    "Items"
                );


            if (!Directory.Exists(folder))
            {
                Debug.LogWarning(
                    "FOOD METADATA: item folder not found: " +
                    folder
                );

                return;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            for (int i = 0; i < files.Length; i++)
            {
                TryLoadFile(
                    files[i]
                );
            }


            Debug.Log(
                "FOOD METADATA: loaded " +
                Data.Count +
                " food item(s)."
            );
        }


        public static bool TryGet(
            string itemId,
            out FoodMetadata metadata)
        {
            EnsureLoaded();


            metadata = null;


            if (string.IsNullOrWhiteSpace(itemId))
                return false;


            if (Data.TryGetValue(itemId.Trim(), out metadata)) return true;
            if (!Game.Gameplay.Cooking.StewRecipe.TryRead(itemId, out metadata)) return false;
            Data[itemId] = metadata;
            return true;
        }


        public static string Describe(string itemId)
        {
            if (!TryGet(itemId, out var food)) return null;
            var text = new System.Text.StringBuilder();
            text.Append("Насыщение +").Append(food.HungerRestore.ToString("0.##"))
                .Append(" · здоровье +").Append(food.HealthRestore.ToString("0.##"))
                .Append("\nПоедание: ").Append(food.EatTime > 0 ? food.EatTime.ToString("0.##") : "1").Append(" с");
            if (food.Effects != null) foreach (var effect in food.Effects)
            {
                if (effect == null || !effect.TryType(out var type) || effect.Duration <= 0) continue;
                string[] names = { "Заморозка", "Отравление", "Ускорение", "Высокий прыжок", "Сила", "Защита" };
                text.Append("\n").Append(names[(int)type]).Append(" ").Append(Mathf.Clamp(effect.Level, 1, 10))
                    .Append(": ").Append(effect.Duration.ToString("0.##")).Append(" с");
            }
            return text.ToString();
        }

        public static bool IsFood(
            string itemId)
        {
            return TryGet(
                itemId,
                out _
            );
        }


        // =====================================================
        // LOADING
        // =====================================================

        private static void TryLoadFile(
            string path)
        {
            try
            {
                string json =
                    File.ReadAllText(
                        path
                    );


                if (string.IsNullOrWhiteSpace(json))
                    return;


                ItemFoodJson data =
                    JsonUtility.FromJson<ItemFoodJson>(
                        json
                    );


                if (data == null ||
                    string.IsNullOrWhiteSpace(data.ID))
                {
                    return;
                }


                if (!HasTag(
                    data.Tags,
                    "eat"
                ))
                {
                    return;
                }


                FoodMetadata metadata =
                    new FoodMetadata
                    {
                        ID =
                            data.ID.Trim(),

                        EatTime =
                            data.EatTime,

                        HungerRestore =
                            data.HungerRestore,

                        HealthRestore =
                            data.HealthRestore,

                        Effects = data.Effects
                    };


                Data[metadata.ID] =
                    metadata;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "FOOD METADATA: failed to load file:\n" +
                    path +
                    "\n" +
                    exception
                );
            }
        }


        private static bool HasTag(
            string[] tags,
            string wantedTag)
        {
            if (tags == null ||
                tags.Length == 0)
            {
                return false;
            }


            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(
                    tags[i],
                    wantedTag,
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    return true;
                }
            }


            return false;
        }
    }
}
