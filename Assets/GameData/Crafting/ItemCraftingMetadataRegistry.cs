using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Blocks;
using Game.Items;


namespace Game.Crafting
{
    public static class ItemCraftingMetadataRegistry
    {
        // =====================================================
        // JSON DTO
        // =====================================================

        [Serializable]
        private class ItemCraftJson
        {
            public string ID;

            public string Name;

            public List<CraftIngredientDefinition>
                CraftIngredients;

            public bool CraftWithoutWorkbench;

            public int CraftResultCount = 1;
        }


        // =====================================================
        // STORAGE
        // =====================================================

        private static readonly Dictionary<string, CraftRecipe>
            Recipes =
                new Dictionary<string, CraftRecipe>(
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
            Recipes.Clear();
        }


        // =====================================================
        // PUBLIC
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
            Recipes.Clear();


            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            if (!Directory.Exists(folder))
            {
                Debug.LogWarning(
                    "ITEM CRAFTING: Items folder not found: " +
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


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                TryLoadFile(
                    files[i]
                );
            }


            Debug.Log(
                "ITEM CRAFTING: loaded " +
                Recipes.Count +
                " item recipe(s)."
            );
        }


        public static IEnumerable<CraftRecipe>
            GetAll()
        {
            EnsureLoaded();

            return Recipes.Values;
        }


        public static bool TryGet(
            string itemId,
            out CraftRecipe recipe
        )
        {
            EnsureLoaded();


            if (string.IsNullOrWhiteSpace(itemId))
            {
                recipe = null;
                return false;
            }


            return Recipes.TryGetValue(
                itemId.Trim(),
                out recipe
            );
        }


        // =====================================================
        // LOAD FILE
        // =====================================================

        private static void TryLoadFile(
            string path
        )
        {
            try
            {
                string json =
                    File.ReadAllText(
                        path
                    );


                if (string.IsNullOrWhiteSpace(json))
                    return;


                ItemCraftJson data =
                    JsonUtility.FromJson<ItemCraftJson>(
                        json
                    );


                if (
                    data == null
                    ||
                    string.IsNullOrWhiteSpace(
                        data.ID
                    )
                )
                {
                    return;
                }


                if (
                    data.CraftIngredients == null
                    ||
                    data.CraftIngredients.Count == 0
                )
                {
                    return;
                }


                string itemId =
                    data.ID.Trim();


                // The result must actually be registered as an item.
                // This prevents stale/invalid JSON from appearing in the UI.
                if (
                    !ItemRegistry.Contains(
                        itemId
                    )
                )
                {
                    Debug.LogWarning(
                        "ITEM CRAFTING: recipe skipped because item is not registered: " +
                        itemId +
                        "\nFile: " +
                        path
                    );

                    return;
                }


                CraftRecipe recipe =
                    new CraftRecipe
                    {
                        ResultItemId =
                            itemId,

                        Name =
                            string.IsNullOrWhiteSpace(
                                data.Name
                            )
                                ? itemId
                                : data.Name,

                        ResultCount =
                            data.CraftResultCount > 0
                                ? data.CraftResultCount
                                : 1,

                        CraftWithoutWorkbench =
                            data.CraftWithoutWorkbench,

                        Ingredients =
                            CopyIngredients(
                                data.CraftIngredients
                            )
                    };


                Recipes[itemId] =
                    recipe;


                Debug.Log(
                    "ITEM CRAFTING: registered recipe " +
                    itemId +
                    " | ingredients = " +
                    recipe.Ingredients.Count +
                    " | without workbench = " +
                    recipe.CraftWithoutWorkbench
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "ITEM CRAFTING: failed to read:\n" +
                    path +
                    "\n" +
                    exception
                );
            }
        }


        private static List<CraftIngredientDefinition>
            CopyIngredients(
                List<CraftIngredientDefinition> source
            )
        {
            List<CraftIngredientDefinition> result =
                new List<CraftIngredientDefinition>();


            if (source == null)
                return result;


            for (
                int i = 0;
                i < source.Count;
                i++
            )
            {
                CraftIngredientDefinition ingredient =
                    source[i];


                if (
                    ingredient == null
                    ||
                    string.IsNullOrWhiteSpace(
                        ingredient.ItemId
                    )
                )
                {
                    continue;
                }


                result.Add(
                    new CraftIngredientDefinition
                    {
                        ItemId =
                            ingredient.ItemId,

                        Count =
                            ingredient.Count > 0
                                ? ingredient.Count
                                : 1
                    }
                );
            }


            return result;
        }
    }
}
