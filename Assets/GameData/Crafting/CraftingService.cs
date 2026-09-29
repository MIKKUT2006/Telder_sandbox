using System;
using System.Collections.Generic;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Items;


namespace Game.Crafting
{
    public static class CraftingService
    {
        // =====================================================
        // VISIBLE RECIPES
        // =====================================================

        public static List<CraftRecipe>
            GetVisibleRecipes(
                bool hasWorkbench
            )
        {
            Dictionary<string, CraftRecipe>
                uniqueRecipes =
                    new Dictionary<string, CraftRecipe>(
                        StringComparer.OrdinalIgnoreCase
                    );


            AddBlockRecipes(
                uniqueRecipes,
                hasWorkbench
            );


            AddItemRecipes(
                uniqueRecipes,
                hasWorkbench
            );


            List<CraftRecipe> result =
                new List<CraftRecipe>(
                    uniqueRecipes.Values
                );


            result.Sort(
                (
                    a,
                    b
                ) =>
                    string.Compare(
                        a.Name,
                        b.Name,
                        StringComparison.OrdinalIgnoreCase
                    )
            );


            return result;
        }


        // =====================================================
        // BLOCK RECIPES
        // =====================================================

        private static void AddBlockRecipes(
            Dictionary<string, CraftRecipe> result,
            bool hasWorkbench
        )
        {
            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {
                if (
                    block == null
                    ||
                    string.IsNullOrWhiteSpace(
                        block.ID
                    )
                    ||
                    block.CraftIngredients == null
                    ||
                    block.CraftIngredients.Count == 0
                )
                {
                    continue;
                }


                if (
                    !hasWorkbench
                    &&
                    !block.CraftWithoutWorkbench
                )
                {
                    continue;
                }


                if (
                    !ItemRegistry.Contains(
                        block.ID
                    )
                )
                {
                    continue;
                }


                CraftRecipe recipe =
                    new CraftRecipe
                    {
                        ResultItemId =
                            block.ID,

                        Name =
                            string.IsNullOrWhiteSpace(
                                block.Name
                            )
                                ? block.ID
                                : block.Name,

                        ResultCount =
                            block.CraftResultCount > 0
                                ? block.CraftResultCount
                                : 1,

                        CraftWithoutWorkbench =
                            block.CraftWithoutWorkbench,

                        Ingredients =
                            CopyIngredients(
                                block.CraftIngredients
                            )
                    };


                result[recipe.ResultItemId] =
                    recipe;
            }
        }


        // =====================================================
        // ITEM RECIPES
        // =====================================================

        private static void AddItemRecipes(
            Dictionary<string, CraftRecipe> result,
            bool hasWorkbench
        )
        {
            ItemCraftingMetadataRegistry
                .EnsureLoaded();


            foreach (
                CraftRecipe recipe
                in ItemCraftingMetadataRegistry
                    .GetAll()
            )
            {
                if (
                    recipe == null
                    ||
                    string.IsNullOrWhiteSpace(
                        recipe.ResultItemId
                    )
                    ||
                    recipe.Ingredients == null
                    ||
                    recipe.Ingredients.Count == 0
                )
                {
                    continue;
                }


                if (
                    !hasWorkbench
                    &&
                    !recipe.CraftWithoutWorkbench
                )
                {
                    continue;
                }


                // Item JSON recipe intentionally overrides a block recipe
                // if both somehow produce the same inventory item.
                result[recipe.ResultItemId] =
                    recipe;
            }
        }


        // =====================================================
        // CAN CRAFT
        // =====================================================

        public static bool CanCraft(
            PlayerInventory inventory,
            CraftRecipe recipe
        )
        {
            if (
                inventory == null
                ||
                recipe == null
                ||
                string.IsNullOrWhiteSpace(
                    recipe.ResultItemId
                )
                ||
                recipe.Ingredients == null
                ||
                recipe.Ingredients.Count == 0
            )
            {
                return false;
            }


            int resultCount =
                recipe.ResultCount > 0
                    ? recipe.ResultCount
                    : 1;


            if (
                !inventory.CanAddItem(
                    recipe.ResultItemId,
                    resultCount
                )
            )
            {
                return false;
            }


            Dictionary<string, int> requirements =
                BuildRequirements(
                    recipe
                );


            foreach (
                KeyValuePair<string, int> pair
                in requirements
            )
            {
                if (
                    !inventory.HasItem(
                        pair.Key,
                        pair.Value
                    )
                )
                {
                    return false;
                }
            }


            return true;
        }


        // =====================================================
        // CRAFT
        // =====================================================

        public static bool Craft(
            PlayerInventory inventory,
            CraftRecipe recipe
        )
        {
            if (
                !CanCraft(
                    inventory,
                    recipe
                )
            )
            {
                return false;
            }


            Dictionary<string, int> requirements =
                BuildRequirements(
                    recipe
                );


            foreach (
                KeyValuePair<string, int> pair
                in requirements
            )
            {
                if (
                    !inventory.RemoveItem(
                        pair.Key,
                        pair.Value
                    )
                )
                {
                    return false;
                }
            }


            int resultCount =
                recipe.ResultCount > 0
                    ? recipe.ResultCount
                    : 1;


            int remaining =
                inventory.AddItem(
                    recipe.ResultItemId,
                    resultCount
                );


            if (remaining == 0)
            {
                Game.Achievements.AchievementRuntime.NotifyCraft(recipe.ResultItemId, resultCount);
                return true;
            }

            return false;
        }


        // =====================================================
        // REQUIREMENTS
        // =====================================================

        private static Dictionary<string, int>
            BuildRequirements(
                CraftRecipe recipe
            )
        {
            Dictionary<string, int> result =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );


            if (
                recipe == null
                ||
                recipe.Ingredients == null
            )
            {
                return result;
            }


            for (
                int i = 0;
                i < recipe.Ingredients.Count;
                i++
            )
            {
                CraftIngredientDefinition ingredient =
                    recipe.Ingredients[i];


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


                int count =
                    ingredient.Count > 0
                        ? ingredient.Count
                        : 1;


                if (
                    result.TryGetValue(
                        ingredient.ItemId,
                        out int existing
                    )
                )
                {
                    result[ingredient.ItemId] =
                        existing + count;
                }
                else
                {
                    result[ingredient.ItemId] =
                        count;
                }
            }


            return result;
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


                if (ingredient == null)
                    continue;


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
