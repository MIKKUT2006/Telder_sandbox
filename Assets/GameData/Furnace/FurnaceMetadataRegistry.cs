using System;
using Game.Items;
using Game.GameplaySystems.Multiblock;

namespace Game.GameplaySystems.Furnace
{
    internal static class FurnaceMetadataRegistry
    {
        public static bool IsFurnace(string blockId)
        {
            return MultiBlockMetadataRegistry.HasTag(blockId, "furnace");
        }

        public static bool TryFuel(string id, out float seconds)
        {
            seconds = 0f;
            if (!ItemRegistry.TryGet(id, out ItemDefinition item) || item == null || item.FuelBurnTime <= 0f)
                return false;

            bool tagged = false;
            if (item.Tags != null)
            {
                for (int i = 0; i < item.Tags.Length; i++)
                {
                    if (string.Equals(item.Tags[i], "fuel", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(item.Tags[i], "burnable", StringComparison.OrdinalIgnoreCase))
                    {
                        tagged = true;
                        break;
                    }
                }
            }

            if (!tagged)
                return false;

            seconds = item.FuelBurnTime;
            return true;
        }

        public static bool TryRecipe(string id, out string result, out int count, out float time)
        {
            result = null;
            count = 1;
            time = 5f;

            if (!ItemRegistry.TryGet(id, out ItemDefinition item) || item == null || string.IsNullOrWhiteSpace(item.SmeltResult))
                return false;

            result = item.SmeltResult;
            count = Math.Max(1, item.SmeltResultCount);
            time = Math.Max(0.05f, item.SmeltTime);
            return true;
        }

        public static int GetMaxStack(string id)
        {
            return Math.Max(1, ItemRegistry.GetMaxStack(id));
        }

        public static string GetDisplayName(string id)
        {
            if (ItemRegistry.TryGet(id, out ItemDefinition item) && item != null && !string.IsNullOrWhiteSpace(item.Name))
                return item.Name;
            return id;
        }
    }
}
