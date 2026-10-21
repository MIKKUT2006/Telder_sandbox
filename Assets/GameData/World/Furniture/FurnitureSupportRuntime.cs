using System;
using Game.Blocks;
using Game.Content;

namespace Game.World.Furniture
{
    /// <summary>Support checks for manually placed and generated furniture.
    /// Unloaded chunks are never treated as empty and no chunks are created here.</summary>
    public static class FurnitureSupportRuntime
    {
        public static bool IsFurnitureOnlyBlock(ushort numericId)
        {
            if (numericId == 0) return false;
            try
            {
                ContentID id = BlockIDRegistry.GetContentID(numericId);
                return BlockRegistry.Contains(id) && BlockRegistry.Get(id).alwaysFurniture;
            }
            catch { return false; }
        }

        public static bool IsFurnitureOnlyBlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            try
            {
                ContentID parsed = ContentID.Parse(id);
                return BlockRegistry.Contains(parsed) && BlockRegistry.Get(parsed).alwaysFurniture;
            }
            catch { return false; }
        }

        public static bool IsHanging(string id)
        {
            return string.Equals(id, "game:vines", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasSupport(int x, int y)
        {
            var furniture = FurnitureLayerManager.Instance;
            return HasSupport(x, y, furniture != null ? furniture.GetFurniture(x, y) : null);
        }

        public static bool HasSupport(int x, int y, string blockId)
        {
            WorldManager manager = WorldManager.Instance;
            World world = manager != null ? manager.GetWorld() : null;
            if (world == null) return false;

            var contentId = ContentID.Parse(blockId ?? "");
            if (BlockRegistry.Contains(contentId) && BlockRegistry.Get(contentId).Plant != null)
                return Game.Gameplay.Plants.PlantRuntime.ValidSoil(BlockRegistry.Get(contentId).Plant,x,y);

            if (IsHanging(blockId))
            {
                var layer = FurnitureLayerManager.Instance;
                for (int up = 1; up <= 9; up++)
                {
                    int sy = y + up;
                    if (world.GetChunk(MathfFloorDiv(x, Chunk.SizeX), MathfFloorDiv(sy, Chunk.SizeY)) == null)
                        return false;
                    if (world.GetBlock(x, sy) != 0) return true;
                    if (layer == null || !IsHanging(layer.GetFurniture(x, sy))) return false;
                }
                return false;
            }
            int below = y - 1;
            // When support isn't loaded yet, placement must wait.
            if (world.GetChunk(MathfFloorDiv(x, Chunk.SizeX),
                               MathfFloorDiv(below, Chunk.SizeY)) == null)
                return false;

            if (world.GetBlock(x, below) != 0)
                return true;

            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;
            return furniture != null && furniture.HasFurniture(x, below);
        }

        // Invoked after foreground removal (mining, explosion, cascade, etc.).
        // Checks only the vertical chain above the edited cell.
        public static void SupportChanged(int x, int y)
        {
            FurnitureLayerManager furniture = FurnitureLayerManager.Instance;
            if (furniture == null) return;
            for (int up = 1; up <= 16; up++)
            {
                int fy = y + up;
                string id = furniture.GetFurniture(x, fy);
                if (string.IsNullOrWhiteSpace(id) || !IsFurnitureOnlyBlock(id) || IsHanging(id)) break;
                if (HasSupport(x, fy)) break;
                if (!furniture.BreakFurniture(x, fy)) break;
            }
            for (int down = 1; down <= 9; down++)
            {
                int fy = y - down;
                if (!IsHanging(furniture.GetFurniture(x, fy)) || HasSupport(x, fy)) break;
                if (!furniture.BreakFurniture(x, fy)) break;
            }
        }

        private static int MathfFloorDiv(int a, int size)
        {
            int q = a / size;
            return a < 0 && a % size != 0 ? q - 1 : q;
        }
    }
}
