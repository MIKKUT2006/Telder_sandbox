
using System.Collections.Generic;

using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Items;


namespace Game.Crafting
{

    public static class CraftingService
    {

        public static List<BlockDefinition>
            GetVisibleRecipes(
                bool hasWorkbench
            )
        {

            List<BlockDefinition> result =
                new List<BlockDefinition>();


            foreach (
                BlockDefinition block
                in BlockRegistry.GetAll()
            )
            {

                if (
                    block ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        block.ID
                    )
                    ||
                    block.CraftIngredients ==
                    null
                    ||
                    block.CraftIngredients.Count ==
                    0
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


                result.Add(
                    block
                );

            }


            result.Sort(
                (
                    a,
                    b
                ) =>
                    string.Compare(
                        a.Name,
                        b.Name,
                        System.StringComparison.OrdinalIgnoreCase
                    )
            );


            return result;

        }


        public static bool CanCraft(
            PlayerInventory inventory,
            BlockDefinition recipe
        )
        {

            if (
                inventory ==
                null
                ||
                recipe ==
                null
                ||
                recipe.CraftIngredients ==
                null
                ||
                recipe.CraftIngredients.Count ==
                0
            )
            {

                return false;

            }


            int resultCount =
                recipe.CraftResultCount >
                0
                    ? recipe.CraftResultCount
                    : 1;


            if (
                !inventory.CanAddItem(
                    recipe.ID,
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


        public static bool Craft(
            PlayerInventory inventory,
            BlockDefinition recipe
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
                recipe.CraftResultCount >
                0
                    ? recipe.CraftResultCount
                    : 1;


            int remaining =
                inventory.AddItem(
                    recipe.ID,
                    resultCount
                );


            return
                remaining ==
                0;

        }

        private static Dictionary<string, int>
            BuildRequirements(
                BlockDefinition recipe
            )
        {

            Dictionary<string, int> result =
                new Dictionary<string, int>();


            if (
                recipe ==
                null
                ||
                recipe.CraftIngredients ==
                null
            )
            {

                return result;

            }


            for (
                int i = 0;
                i < recipe.CraftIngredients.Count;
                i++
            )
            {

                CraftIngredientDefinition ingredient =
                    recipe.CraftIngredients[i];


                if (
                    ingredient ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        ingredient.ItemId
                    )
                )
                {

                    continue;

                }


                int count =
                    ingredient.Count >
                    0
                        ? ingredient.Count
                        : 1;


                if (
                    result.TryGetValue(
                        ingredient.ItemId,
                        out int existing
                    )
                )
                {

                    result[
                        ingredient.ItemId
                    ] =
                        existing +
                        count;

                }
                else
                {

                    result[
                        ingredient.ItemId
                    ] =
                        count;

                }

            }


            return result;

        }


    }

}
