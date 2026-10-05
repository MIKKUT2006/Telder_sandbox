using System;
using Game.Blocks;
using Game.Content;

namespace Game.GameplaySystems.Multiblock
{
    public sealed class MultiBlockMetadata
    {
        public int Width = 1;
        public int Height = 1;
        public int AnchorX;
        public int AnchorY;
        public string Kind;
        public bool RequireFloor;
    }

    internal static class MultiBlockMetadataRegistry
    {
        public static bool TryGet(string id, out MultiBlockMetadata meta)
        {
            meta = null;
            if (!TryGetBlock(id, out BlockDefinition block) || block.MultiBlock == null)
                return false;

            int width = Math.Max(1, block.MultiBlock.Width);
            int height = Math.Max(1, block.MultiBlock.Height);
            if (width <= 1 && height <= 1)
                return false;

            int anchorX = Math.Max(0, Math.Min(width - 1, block.MultiBlock.AnchorX));
            int anchorY = Math.Max(0, Math.Min(height - 1, block.MultiBlock.AnchorY));

            meta = new MultiBlockMetadata
            {
                Width = width,
                Height = height,
                AnchorX = anchorX,
                AnchorY = anchorY,
                Kind = block.MultiBlock.Kind,
                RequireFloor = block.MultiBlock.RequireFloor
            };
            return true;
        }

        public static bool HasTag(string id, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !TryGetBlock(id, out BlockDefinition block) || block.Tags == null)
                return false;

            for (int i = 0; i < block.Tags.Count; i++)
            {
                if (string.Equals(block.Tags[i], tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool TryGetBlock(string id, out BlockDefinition block)
        {
            block = null;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            ContentID contentId = ContentID.Parse(id);
            if (!BlockRegistry.Contains(contentId))
                return false;

            block = BlockRegistry.Get(contentId);
            return block != null;
        }
    }
}
