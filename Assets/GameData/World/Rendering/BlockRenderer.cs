using System.Collections.Generic;

using UnityEngine;

using Game.Resources;

namespace Game.World.Rendering
{
    public static class BlockRenderer
    {
        public const int BlockPixelSize = 16;

        private static readonly Dictionary<
            ushort,
            Color32[]
        > pixelCache =
            new Dictionary<
                ushort,
                Color32[]
            >();

        private static readonly Color32[]
            transparentPixels =
            CreateTransparentPixels();

        public static void DrawBlock(
            Texture2D target,
            int x,
            int y,
            ushort blockID
        )
        {
            if (target == null)
                return;

            int pixelX =
                x * BlockPixelSize;

            int pixelY =
                y * BlockPixelSize;

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

            Color32[] pixels =
                GetPixels(blockID);

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

        private static Color32[] GetPixels(
            ushort blockID
        )
        {
            if (
                pixelCache.TryGetValue(
                    blockID,
                    out Color32[] cached)
            )
            {
                return cached;
            }

            var block =
                BlockDatabase.Get(blockID);

            Texture2D texture =
                TextureManager.Get(
                    block.Texture
                );

            if (texture == null)
                return null;

            Color32[] pixels =
                texture.GetPixels32();

            int expected =
                BlockPixelSize *
                BlockPixelSize;

            if (pixels.Length != expected)
            {
                Debug.LogError(
                    "Block texture must be " +
                    BlockPixelSize +
                    "x" +
                    BlockPixelSize +
                    ": " +
                    block.Texture
                );

                return null;
            }

            pixelCache.Add(
                blockID,
                pixels
            );

            return pixels;
        }

        private static Color32[]
            CreateTransparentPixels()
        {
            return new Color32[
                BlockPixelSize *
                BlockPixelSize
            ];
        }

        public static void ClearCache()
        {
            pixelCache.Clear();
        }
    }
}