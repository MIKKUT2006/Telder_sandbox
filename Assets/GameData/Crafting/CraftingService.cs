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
        // ВНУТРЕННЕЕ ПРЕДСТАВЛЕНИЕ ТРЕБОВАНИЯ
        // =====================================================

        private sealed class Requirement
        {
            public string ItemId;
            public string Tag;
            public int Count;
            public bool IsTag => string.IsNullOrWhiteSpace(ItemId);
        }


        // =====================================================
        // ВИДИМЫЕ РЕЦЕПТЫ (без изменений)
        // =====================================================

        public static List<CraftRecipe> GetVisibleRecipes(bool hasWorkbench)
        {
            Dictionary<string, CraftRecipe> uniqueRecipes =
                new Dictionary<string, CraftRecipe>(
                    StringComparer.OrdinalIgnoreCase);

            AddBlockRecipes(uniqueRecipes, hasWorkbench);
            AddItemRecipes(uniqueRecipes, hasWorkbench);

            List<CraftRecipe> result =
                new List<CraftRecipe>(uniqueRecipes.Values);

            result.Sort((a, b) => string.Compare(
                a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return result;
        }


        // =====================================================
        // BLOCK RECIPES (без изменений, кроме CopyIngredients)
        // =====================================================

        private static void AddBlockRecipes(
            Dictionary<string, CraftRecipe> result, bool hasWorkbench)
        {
            foreach (BlockDefinition block in BlockRegistry.GetAll())
            {
                if (block == null
                    || string.IsNullOrWhiteSpace(block.ID)
                    || block.CraftIngredients == null
                    || block.CraftIngredients.Count == 0)
                    continue;

                if (!hasWorkbench && !block.CraftWithoutWorkbench)
                    continue;

                if (!ItemRegistry.Contains(block.ID))
                    continue;

                CraftRecipe recipe = new CraftRecipe
                {
                    ResultItemId = block.ID,
                    Name = string.IsNullOrWhiteSpace(block.Name)
                        ? block.ID : block.Name,
                    ResultCount = block.CraftResultCount > 0
                        ? block.CraftResultCount : 1,
                    CraftWithoutWorkbench = block.CraftWithoutWorkbench,
                    Ingredients = CopyIngredients(block.CraftIngredients)
                };

                result[recipe.ResultItemId] = recipe;
            }
        }


        // =====================================================
        // ITEM RECIPES (без изменений)
        // =====================================================

        private static void AddItemRecipes(
            Dictionary<string, CraftRecipe> result, bool hasWorkbench)
        {
            ItemCraftingMetadataRegistry.EnsureLoaded();

            foreach (CraftRecipe recipe
                in ItemCraftingMetadataRegistry.GetAll())
            {
                if (recipe == null
                    || string.IsNullOrWhiteSpace(recipe.ResultItemId)
                    || recipe.Ingredients == null
                    || recipe.Ingredients.Count == 0)
                    continue;

                if (!hasWorkbench && !recipe.CraftWithoutWorkbench)
                    continue;

                result[recipe.ResultItemId] = recipe;
            }
        }


        // =====================================================
        // CAN CRAFT
        // =====================================================

        public static bool CanCraft(
            PlayerInventory inventory, CraftRecipe recipe)
        {
            if (!TryBuildPlan(inventory, recipe, out _))
                return false;

            int resultCount =
                recipe.ResultCount > 0 ? recipe.ResultCount : 1;

            if (!inventory.CanAddItem(recipe.ResultItemId, resultCount))
                return false;

            return true;
        }


        // =====================================================
        // CRAFT
        // =====================================================

        public static bool Craft(
            PlayerInventory inventory, CraftRecipe recipe)
        {
            if (!TryBuildPlan(inventory, recipe, out Dictionary<string, int> plan))
                return false;

            int resultCount =
                recipe.ResultCount > 0 ? recipe.ResultCount : 1;

            if (!inventory.CanAddItem(recipe.ResultItemId, resultCount))
                return false;

            foreach (KeyValuePair<string, int> pair in plan)
            {
                if (!inventory.RemoveItem(pair.Key, pair.Value))
                    return false;
            }

            int remaining =
                inventory.AddItem(recipe.ResultItemId, resultCount);

            if (remaining == 0)
            {
                Game.Audio.TelderAudioSystem.Play("ui.craft");
                Game.Achievements.AchievementRuntime.NotifyCraft(
                    recipe.ResultItemId, resultCount);
                return true;
            }

            return false;
        }


        // =====================================================
        // ПЛАН УДАЛЕНИЯ
        // =====================================================

        /// <summary>
        /// Строит план: сколько какого itemId нужно удалить из инвентаря,
        /// чтобы удовлетворить рецепт. Возвращает false, если ресурсов не хватает.
        /// </summary>
        private static bool TryBuildPlan(
            PlayerInventory inventory,
            CraftRecipe recipe,
            out Dictionary<string, int> plan)
        {
            plan = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (inventory == null
                || recipe == null
                || string.IsNullOrWhiteSpace(recipe.ResultItemId)
                || recipe.Ingredients == null
                || recipe.Ingredients.Count == 0)
                return false;

            List<Requirement> requirements = BuildRequirements(recipe);

            if (requirements.Count == 0)
                return false;

            // Снимок инвентаря: itemId -> количество.
            // Требуется, чтобы корректно распределять один и тот же
            // предмет между "конкретным" и "по тегу" требованиями.
            Dictionary<string, int> available =
                inventory.GetItemCountsSnapshot();

            // 1) Сначала — конкретные ItemId.
            for (int i = 0; i < requirements.Count; i++)
            {
                Requirement req = requirements[i];
                if (req.IsTag) continue;

                int have = available.TryGetValue(req.ItemId, out int h) ? h : 0;
                int already = plan.TryGetValue(req.ItemId, out int p) ? p : 0;
                int free = have - already;

                if (free < req.Count)
                    return false;

                plan[req.ItemId] = already + req.Count;
            }

            // 2) Затем — по тегам.
            for (int i = 0; i < requirements.Count; i++)
            {
                Requirement req = requirements[i];
                if (!req.IsTag) continue;

                int needed = req.Count;

                foreach (string itemId
                    in ItemTagRegistry.GetItemsWithTag(req.Tag))
                {
                    if (needed <= 0) break;

                    int have = available.TryGetValue(itemId, out int h) ? h : 0;
                    int already = plan.TryGetValue(itemId, out int p) ? p : 0;
                    int free = have - already;

                    if (free <= 0) continue;

                    int take = Math.Min(free, needed);
                    plan[itemId] = already + take;
                    needed -= take;
                }

                if (needed > 0)
                    return false;
            }

            return true;
        }


        // =====================================================
        // REQUIREMENTS
        // =====================================================

        private static List<Requirement> BuildRequirements(CraftRecipe recipe)
        {
            List<Requirement> result = new List<Requirement>();

            if (recipe == null || recipe.Ingredients == null)
                return result;

            Dictionary<string, Requirement> merged =
                new Dictionary<string, Requirement>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                CraftIngredientDefinition ingredient = recipe.Ingredients[i];

                if (ingredient == null) continue;

                bool hasId = !string.IsNullOrWhiteSpace(ingredient.ItemId);
                bool hasTag = !string.IsNullOrWhiteSpace(ingredient.Tag);

                if (!hasId && !hasTag) continue;

                string key = hasId
                    ? "id:" + ingredient.ItemId.Trim()
                    : "tag:" + ingredient.Tag.Trim();

                int count = ingredient.Count > 0 ? ingredient.Count : 1;

                if (merged.TryGetValue(key, out Requirement existing))
                {
                    existing.Count += count;
                }
                else
                {
                    merged[key] = new Requirement
                    {
                        ItemId = hasId ? ingredient.ItemId.Trim() : null,
                        Tag = hasId ? null : ingredient.Tag.Trim(),
                        Count = count
                    };
                }
            }

            result.AddRange(merged.Values);
            return result;
        }


        private static List<CraftIngredientDefinition>
            CopyIngredients(List<CraftIngredientDefinition> source)
        {
            List<CraftIngredientDefinition> result =
                new List<CraftIngredientDefinition>();

            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                CraftIngredientDefinition ingredient = source[i];

                if (ingredient == null) continue;

                bool hasId = !string.IsNullOrWhiteSpace(ingredient.ItemId);
                bool hasTag = !string.IsNullOrWhiteSpace(ingredient.Tag);

                if (!hasId && !hasTag) continue;

                result.Add(new CraftIngredientDefinition
                {
                    ItemId = hasId ? ingredient.ItemId.Trim() : null,
                    Tag = !hasId && hasTag ? ingredient.Tag.Trim() : null,
                    Count = ingredient.Count > 0 ? ingredient.Count : 1
                });
            }

            return result;
        }
    }
}