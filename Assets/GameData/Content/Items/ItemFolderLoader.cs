using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Blocks;
using Game.Content;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Loads the complete ItemDefinition from JSON.
    ///
    /// IMPORTANT:
    /// The old loader only copied ID/Name/Texture/Type/MaxStack into the
    /// registry. As a result every runtime system that reads ItemRegistry
    /// (including the furnace) saw Tags == null, FuelBurnTime == 0 and
    /// SmeltResult == null even though those fields existed in the JSON file.
    /// </summary>
    public static class ItemFolderLoader
    {
        [Serializable]
        private sealed class ItemJson
        {
            public string ID;
            public string Name;
            public string Texture;
            public string Type;
            public int MaxStack;

            // Food / generic item tags.
            public string[] Tags;
            public float EatTime;
            public float HungerRestore;
            public float HealthRestore;

            // Furnace.
            public float FuelBurnTime;
            public string SmeltResult;
            public int SmeltResultCount;
            public float SmeltTime;

            // Crafting.
            public List<CraftIngredientDefinition> CraftIngredients;
            public bool CraftWithoutWorkbench;
            public int CraftResultCount;
        }

        public static void LoadDefaultFolder()
        {
            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );

            LoadFolder(folder);
        }

        public static void LoadFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                Debug.LogError("ITEM LOADER: Folder path is empty.");
                return;
            }

            if (!Directory.Exists(folder))
            {
                Debug.LogWarning("ITEM LOADER: Folder not found: " + folder);
                return;
            }

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            foreach (string file in files)
                LoadFile(file);

            Debug.Log(
                "ITEM LOADER: Registry count = " +
                ItemRegistry.GetAll().Count()
            );
        }

        private static void LoadFile(string file)
        {
            try
            {
                string json = File.ReadAllText(file);
                ItemJson data = JsonUtility.FromJson<ItemJson>(json);

                if (data == null || string.IsNullOrWhiteSpace(data.ID))
                {
                    Debug.LogWarning("ITEM LOADER: Invalid item JSON: " + file);
                    return;
                }

                ContentID contentID;
                try
                {
                    contentID = ContentID.Parse(data.ID);
                }
                catch
                {
                    Debug.LogWarning(
                        "ITEM LOADER: Invalid ContentID '" +
                        data.ID +
                        "' in " +
                        file
                    );
                    return;
                }

                ItemType itemType = ParseItemType(data.Type, data.ID);

                ItemDefinition definition =
                    new ItemDefinition
                    {
                        ID = contentID,
                        Name =
                            string.IsNullOrWhiteSpace(data.Name)
                                ? data.ID
                                : data.Name,
                        Texture = data.Texture,
                        Type = itemType,
                        MaxStack =
                            data.MaxStack > 0
                                ? data.MaxStack
                                : 100,

                        Tags =
                            data.Tags != null
                                ? data.Tags
                                : Array.Empty<string>(),

                        EatTime =
                            data.EatTime > 0f
                                ? data.EatTime
                                : 1f,
                        HungerRestore = data.HungerRestore,
                        HealthRestore = data.HealthRestore,

                        FuelBurnTime =
                            Mathf.Max(0f, data.FuelBurnTime),
                        SmeltResult =
                            string.IsNullOrWhiteSpace(data.SmeltResult)
                                ? null
                                : data.SmeltResult.Trim(),
                        SmeltResultCount =
                            data.SmeltResultCount > 0
                                ? data.SmeltResultCount
                                : 1,
                        SmeltTime =
                            data.SmeltTime > 0f
                                ? Mathf.Max(0.05f, data.SmeltTime)
                                : 5f,

                        CraftIngredients =
                            data.CraftIngredients != null
                                ? data.CraftIngredients
                                : new List<CraftIngredientDefinition>(),
                        CraftWithoutWorkbench =
                            data.CraftWithoutWorkbench,
                        CraftResultCount =
                            data.CraftResultCount > 0
                                ? data.CraftResultCount
                                : 1
                    };

                ItemRegistry.Register(definition);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "ITEM LOADER: Failed to load " +
                    file +
                    "\n" +
                    exception
                );
            }
        }

        private static ItemType ParseItemType(
            string rawType,
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(rawType))
                return ItemType.Misc;

            string normalized = rawType.Trim();

            // Several early content files used the generic value "items".
            // It is not an ItemType enum value and previously silently fell
            // back to enum zero (Block). Treat it as a normal material/item.
            if (string.Equals(
                    normalized,
                    "item",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    normalized,
                    "items",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.Material;
            }

            if (Enum.TryParse(normalized, true, out ItemType itemType))
                return itemType;

            Debug.LogWarning(
                "ITEM LOADER: Unknown ItemType '" +
                rawType +
                "' for " +
                itemId +
                ". Using Misc."
            );

            return ItemType.Misc;
        }
    }
}
