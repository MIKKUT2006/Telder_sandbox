using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Resources;
using Game.GameplaySystems.Multiblock;

namespace Game.World.Rendering
{
    public static class BlockRenderer
    {
        public const int BlockPixelSize = 16;

        private static readonly Dictionary<string, Color32[]> pixelCache =
            new Dictionary<string, Color32[]>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<ushort, Color32[]> multiBlockAnchorCache =
            new Dictionary<ushort, Color32[]>();

        private static readonly HashSet<string> warnedMissing =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Color32[] transparentPixels =
            CreateTransparentPixels();

        public static void DrawBlock(
            Texture2D target,
            int x,
            int y,
            ushort blockID)
        {
            if (target == null)
                return;

            int pixelX = x * BlockPixelSize;
            int pixelY = y * BlockPixelSize;

            if (blockID == 0)
            {
                target.SetPixels32(
                    pixelX,
                    pixelY,
                    BlockPixelSize,
                    BlockPixelSize,
                    transparentPixels
                );
                return;
            }

            if (!BlockDatabase.Contains(blockID))
                return;

            Color32[] pixels = GetBasePixels(blockID);
            if (pixels == null)
                return;

            target.SetPixels32(
                pixelX,
                pixelY,
                BlockPixelSize,
                BlockPixelSize,
                pixels
            );
        }

        public static void DrawBlock(
            Texture2D target,
            int x,
            int y,
            ushort blockID,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (target == null)
                return;

            int pixelX = x * BlockPixelSize;
            int pixelY = y * BlockPixelSize;

            if (blockID == 0)
            {
                target.SetPixels32(
                    pixelX,
                    pixelY,
                    BlockPixelSize,
                    BlockPixelSize,
                    transparentPixels
                );
                return;
            }

            if (!BlockDatabase.Contains(blockID))
                return;

            Color32[] pixels = GetPixels(blockID, worldX, worldY, layer);
            if (pixels == null)
                return;

            target.SetPixels32(
                pixelX,
                pixelY,
                BlockPixelSize,
                BlockPixelSize,
                pixels
            );
        }

        public static bool IsDynamic(ushort blockID)
        {
            return BlockVisualResolver.IsDynamic(blockID);
        }

        public static string GetVisualKey(
            ushort blockID,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (blockID == 0 || !BlockDatabase.Contains(blockID))
                return null;

            var block = BlockDatabase.Get(blockID);
            return BlockVisualResolver.ResolveTextureName(
                block,
                worldX,
                worldY,
                layer
            );
        }

        private static Color32[] GetBasePixels(ushort blockID)
        {
            var block = BlockDatabase.Get(blockID);
            string baseKey = block.Texture;

            if (!string.IsNullOrWhiteSpace(baseKey))
            {
                if (pixelCache.TryGetValue(baseKey, out Color32[] cached))
                    return cached;

                if (TextureManager.TryGet(baseKey, out Texture2D texture) && texture != null)
                {
                    Color32[] pixels = texture.GetPixels32();
                    if (pixels.Length == BlockPixelSize * BlockPixelSize)
                    {
                        pixelCache[baseKey] = pixels;
                        return pixels;
                    }
                }
            }

            if (
                block.MultiBlock != null &&
                TryGetMultiBlockAnchorPixels(blockID, block, out Color32[] multiBlockPixels)
            )
            {
                return multiBlockPixels;
            }

            WarnOnce(
                "missing-base:" + blockID,
                "Texture missing for block: " + block.ID +
                " (base '" + (baseKey ?? "<null>") + "')"
            );
            return null;
        }


        private static Color32[] GetPixels(
            ushort blockID,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            var block = BlockDatabase.Get(blockID);
            string visualKey = BlockVisualResolver.ResolveTextureName(
                block,
                worldX,
                worldY,
                layer
            );

            if (!string.IsNullOrWhiteSpace(visualKey))
            {
                if (pixelCache.TryGetValue(visualKey, out Color32[] cached))
                    return cached;

                if (TextureManager.TryGet(visualKey, out Texture2D texture) && texture != null)
                {
                    Color32[] pixels = texture.GetPixels32();
                    int expected = BlockPixelSize * BlockPixelSize;

                    if (pixels.Length == expected)
                    {
                        pixelCache[visualKey] = pixels;
                        return pixels;
                    }

                    WarnOnce(
                        "size:" + visualKey,
                        "Block texture must be " +
                        BlockPixelSize + "x" + BlockPixelSize +
                        ": " + visualKey
                    );
                }
            }

            // An optional state/animation frame may be missing while the artist is
            // still creating it. Fall back to the normal texture instead of making
            // the block disappear.
            string baseKey = block.Texture;
            if (!string.IsNullOrWhiteSpace(baseKey))
            {
                if (pixelCache.TryGetValue(baseKey, out Color32[] baseCached))
                    return baseCached;

                if (TextureManager.TryGet(baseKey, out Texture2D baseTexture) && baseTexture != null)
                {
                    Color32[] pixels = baseTexture.GetPixels32();
                    if (pixels.Length == BlockPixelSize * BlockPixelSize)
                    {
                        pixelCache[baseKey] = pixels;
                        return pixels;
                    }
                }
            }

            if (
                block.MultiBlock != null &&
                TryGetMultiBlockAnchorPixels(blockID, block, out Color32[] multiBlockPixels)
            )
            {
                return multiBlockPixels;
            }

            WarnOnce(
                "missing:" + blockID,
                "Texture missing for block: " + block.ID +
                " (requested '" + (visualKey ?? baseKey ?? "<null>") + "')"
            );
            return null;
        }

        private static bool TryGetMultiBlockAnchorPixels(
            ushort blockID,
            Game.Blocks.BlockDefinition block,
            out Color32[] pixels)
        {
            if (multiBlockAnchorCache.TryGetValue(blockID, out pixels))
                return pixels != null;

            if (MultiBlockVisualProvider.TryGetAnchorCellPixels(
                block.ID,
                block,
                out Color32[] resolved))
            {
                multiBlockAnchorCache[blockID] = resolved;
                pixels = resolved;
                return true;
            }

            multiBlockAnchorCache[blockID] = null;
            pixels = null;
            return false;
        }

        private static void WarnOnce(string key, string message)
        {
            if (warnedMissing.Add(key))
                Debug.LogWarning(message);
        }

        private static Color32[] CreateTransparentPixels()
        {
            return new Color32[BlockPixelSize * BlockPixelSize];
        }

        public static void ClearCache()
        {
            pixelCache.Clear();
            multiBlockAnchorCache.Clear();
            warnedMissing.Clear();
        }
    }
}
