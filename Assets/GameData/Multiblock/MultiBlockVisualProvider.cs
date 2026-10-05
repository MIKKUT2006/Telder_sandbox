using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Blocks;

namespace Game.GameplaySystems.Multiblock
{
    /// <summary>
    /// Loads the full visual sprite for furniture-style multi-blocks.
    /// Regular block textures are intentionally limited to 16x16 by BlockRenderer,
    /// so multi-block visuals live in ResourcePacks/Default/textures/multiblocks.
    /// </summary>
    public static class MultiBlockVisualProvider
    {
        private const float PixelsPerUnit = 16f;

        private static readonly Dictionary<string, Sprite> spriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Texture2D> textureCache =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            spriteCache.Clear();
            textureCache.Clear();
        }

        public static Sprite GetSprite(string blockId, BlockDefinition definition)
        {
            if (definition == null || definition.MultiBlock == null)
                return null;

            int width = Mathf.Max(1, definition.MultiBlock.Width);
            int height = Mathf.Max(1, definition.MultiBlock.Height);
            if (width <= 1 && height <= 1)
                return null;

            string textureName = definition.MultiBlock.Texture;
            if (string.IsNullOrWhiteSpace(textureName))
                return null;

            textureName = NormalizeTextureName(textureName);
            string cacheKey = blockId + "|" + textureName;

            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            string path = Path.Combine(
                Application.dataPath,
                "GameData",
                "ResourcePacks",
                "Default",
                "textures",
                "multiblocks",
                textureName + ".png"
            );

            if (!File.Exists(path))
            {
                Debug.LogWarning(
                    "MULTIBLOCK VISUAL: Texture not found for " + blockId + "\n" + path
                );
                return null;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "MULTIBLOCK VISUAL: Failed to read texture for " + blockId + "\n" + e
                );
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = "MultiBlockTexture_" + blockId;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("MULTIBLOCK VISUAL: LoadImage failed for " + blockId);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            int expectedWidth = width * 16;
            int expectedHeight = height * 16;
            if (texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogWarning(
                    "MULTIBLOCK VISUAL: " + blockId + " uses a " +
                    texture.width + "x" + texture.height + " texture. " +
                    "For a " + width + "x" + height + " multi-block the recommended size is " +
                    expectedWidth + "x" + expectedHeight + "."
                );
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            sprite.name = "MultiBlockSprite_" + blockId;
            textureCache[cacheKey] = texture;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }


        /// <summary>
        /// Returns the 16x16 anchor cell from the full multi-block texture.
        /// This is only a compatibility fallback for legacy foreground cells
        /// and block-break particles; normal multi-blocks are rendered as one
        /// full SpriteRenderer and never enter BlockRenderer.
        /// </summary>
        public static bool TryGetAnchorCellPixels(
            string blockId,
            BlockDefinition definition,
            out Color32[] pixels
        )
        {
            pixels = null;

            if (definition == null || definition.MultiBlock == null)
                return false;

            Sprite sprite = GetSprite(blockId, definition);
            if (sprite == null || sprite.texture == null)
                return false;

            Texture2D texture = sprite.texture;
            int anchorX = Mathf.Clamp(
                definition.MultiBlock.AnchorX,
                0,
                Mathf.Max(0, definition.MultiBlock.Width - 1)
            );
            int anchorY = Mathf.Clamp(
                definition.MultiBlock.AnchorY,
                0,
                Mathf.Max(0, definition.MultiBlock.Height - 1)
            );

            int startX = anchorX * 16;
            int startY = anchorY * 16;

            if (
                startX < 0 || startY < 0 ||
                startX + 16 > texture.width ||
                startY + 16 > texture.height
            )
            {
                return false;
            }

            try
            {
                Color32[] all = texture.GetPixels32();
                Color32[] result = new Color32[16 * 16];

                for (int y = 0; y < 16; y++)
                {
                    Array.Copy(
                        all,
                        (startY + y) * texture.width + startX,
                        result,
                        y * 16,
                        16
                    );
                }

                pixels = result;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "MULTIBLOCK VISUAL: Failed to read anchor pixels for " +
                    blockId + "\n" + e.Message
                );
                return false;
            }
        }

        public static Vector2 GetAnchorToCenterOffset(BlockDefinition definition)
        {
            if (definition == null || definition.MultiBlock == null)
                return Vector2.zero;

            int width = Mathf.Max(1, definition.MultiBlock.Width);
            int height = Mathf.Max(1, definition.MultiBlock.Height);
            int anchorX = Mathf.Clamp(definition.MultiBlock.AnchorX, 0, width - 1);
            int anchorY = Mathf.Clamp(definition.MultiBlock.AnchorY, 0, height - 1);

            return new Vector2(
                (width - 1) * 0.5f - anchorX,
                (height - 1) * 0.5f - anchorY
            );
        }

        private static string NormalizeTextureName(string value)
        {
            string result = value.Trim().Replace('\\', '/');
            int slash = result.LastIndexOf('/');
            if (slash >= 0 && slash < result.Length - 1)
                result = result.Substring(slash + 1);

            if (result.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                result = result.Substring(0, result.Length - 4);

            return result;
        }
    }
}
