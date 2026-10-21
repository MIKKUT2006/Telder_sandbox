using System;
using System.Collections.Generic;
using Game.Inventory;
using Game.PlayerStats;
using UnityEngine;

namespace Game.Gameplay.Cooking
{
    [Serializable]
    public sealed class CauldronDefinition
    {
        public float CookSeconds = 60;
    }

    [Serializable]
    public sealed class CauldronState
    {
        public int X, Y;
        public string BlockId = "game:cauldron";
        public ItemStack[] Slots =
        {
            new ItemStack(),
            new ItemStack(),
            new ItemStack(),
            new ItemStack(),
            new ItemStack()
        };
        public float Progress;
        public void Normalize()
        {
            if (Slots == null || Slots.Length != 5)
                Slots = new[]
                {
                    new ItemStack(),
                    new ItemStack(),
                    new ItemStack(),
                    new ItemStack(),
                    new ItemStack()
                };
            for (int i = 0; i < 5; i++)
                if (Slots[i] == null)
                    Slots[i] = new ItemStack();
            Progress = Mathf.Max(0, Progress);
        }
    }

    public static class CauldronRules
    {
        public static string FoodType(string id) => StewRecipe.IsStew(id) ? "game:stew" : id;
        public static bool Accepts(CauldronState state, int slot, string id, CauldronDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id) || slot < 0 || slot > 3)
                return false;
            if (slot == 3)
                return id == "game:cup";
            if (!FoodMetadataRegistry.IsFood(id))
                return false;
            for (int i = 0; i < 3; i++)
                if (i != slot && !state.Slots[i].IsEmpty && FoodType(state.Slots[i].ItemId) == FoodType(id))
                    return false;
            return true;
        }

        public static bool Recipe(CauldronState state, CauldronDefinition definition, out string result)
        {
            result = null;
            if (state?.Slots == null || state.Slots.Length != 5)
                return false;
            for (int i = 0; i < 3; i++)
                if (state.Slots[i] == null || state.Slots[i].IsEmpty || !Accepts(state, i, state.Slots[i].ItemId, definition))
                    return false;
            var cups = state.Slots[3];
            if (cups == null || cups.Count < 3 || !Accepts(state, 3, cups.ItemId, definition))
                return false;
            if (!FoodMetadataRegistry.TryGet(state.Slots[0].ItemId, out var a) || !FoodMetadataRegistry.TryGet(state.Slots[1].ItemId, out var b) || !FoodMetadataRegistry.TryGet(state.Slots[2].ItemId, out var c))
                return false;
            result = StewRecipe.Create(a, b, c);
            return result != null;
        }

        public static bool Tick(CauldronState state, CauldronDefinition definition, float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds))
                return false;
            if (!Recipe(state, definition, out string result))
            {
                state.Progress = 0;
                return false;
            }

            var output = state.Slots[4];
            if (!output.IsEmpty && (output.ItemId != result || output.Count > 12))
                return false;
            float duration = Mathf.Max(1, definition?.CookSeconds ?? 60);
            state.Progress = Mathf.Min(duration, state.Progress + seconds);
            if (state.Progress < duration)
                return false;
            for (int i = 0; i < 3; i++)
                state.Slots[i].Count--;
            state.Slots[3].Count -= 3;
            if (output.IsEmpty)
                output.Set(result, 3);
            else
                output.Count += 3;
            state.Progress = 0;
            return true;
        }
    }
}
