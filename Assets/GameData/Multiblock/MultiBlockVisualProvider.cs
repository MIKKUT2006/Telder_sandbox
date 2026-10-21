using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Blocks;
using Game.Resources;
using Game.World.Rendering;

namespace Game.GameplaySystems.Multiblock
{
    /// <summary>
    /// Loads full-size visuals for furniture-style multi-blocks.
    ///
    /// Multi-block textures live primarily in:
    /// Assets/GameData/ResourcePacks/Default/textures/multiblocks/
    ///
    /// Unlike the old implementation, this provider also resolves runtime
    /// States/Animation, so a 1x2 furnace can switch from furnace.png to
    /// furnace_burning.png without falling back to the normal 16x16 renderer.
    /// For backwards compatibility a state/frame texture already loaded by
    /// TextureManager from textures/blocks is also accepted.
    /// </summary>
    public static class MultiBlockVisualProvider
    {
        private const float PixelsPerUnit = 16f;

        private static readonly Dictionary<string, Sprite> spriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Texture2D> textureCache =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> missingMultiBlockTextures =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            spriteCache.Clear();
            textureCache.Clear();
            missingMultiBlockTextures.Clear();
        }

        public static bool IsMultiBlock(BlockDefinition definition)
        {
            if (definition == null || definition.MultiBlock == null)
                return false;

            return
                Mathf.Max(1, definition.MultiBlock.Width) > 1 ||
                Mathf.Max(1, definition.MultiBlock.Height) > 1;
        }

        /// <summary>
        /// Legacy/default multi-block sprite (no runtime state lookup).
        /// </summary>
        public static Sprite GetSprite(string blockId, BlockDefinition definition)
        {
            if (!IsMultiBlock(definition))
                return null;

            string textureName = GetBaseTextureName(definition);
            if (string.IsNullOrWhiteSpace(textureName))
                return null;

            Sprite sprite = GetSpriteForTexture(blockId, definition, textureName);
            if (sprite != null)
                return sprite;

            return GetRegularTextureSprite(blockId, definition, textureName);
        }

        /// <summary>
        /// Resolves the currently visible full-size sprite for a multi-block,
        /// including runtime state and animation.
        /// </summary>
        public static Sprite ResolveSprite(
            string blockId,
            BlockDefinition definition,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (!IsMultiBlock(definition))
                return null;

            string state = BlockVisualStateRuntime.GetState(worldX, worldY, layer);
            BlockVisualStateDefinition stateVisual = FindState(definition, state);

            string baseTexture = GetBaseTextureName(definition);
            string stateTexture =
                stateVisual != null && !string.IsNullOrWhiteSpace(stateVisual.Texture)
                    ? NormalizeTextureName(stateVisual.Texture)
                    : null;

            BlockAnimationDefinition animation =
                stateVisual != null && HasFrames(stateVisual.Animation)
                    ? stateVisual.Animation
                    : definition.Animation;

            // 1) Animated frame for the active state/base visual.
            if (HasFrames(animation))
            {
                string frame = NormalizeTextureName(
                    animation.Frames[GetFrameIndex(animation, worldX, worldY)]
                );

                Sprite animated = GetSpriteForTexture(blockId, definition, frame);
                if (animated != null)
                    return animated;

                animated = GetRegularTextureSprite(blockId, definition, frame);
                if (animated != null)
                    return animated;
            }

            // 2) Static texture explicitly configured by the active state.
            if (!string.IsNullOrWhiteSpace(stateTexture))
            {
                Sprite stateSprite = GetSpriteForTexture(blockId, definition, stateTexture);
                if (stateSprite != null)
                    return stateSprite;

                stateSprite = GetRegularTextureSprite(blockId, definition, stateTexture);
                if (stateSprite != null)
                    return stateSprite;
            }

            // 3) Normal multi-block texture.
            if (!string.IsNullOrWhiteSpace(baseTexture))
            {
                Sprite baseSprite = GetSpriteForTexture(blockId, definition, baseTexture);
                if (baseSprite != null)
                    return baseSprite;

                baseSprite = GetRegularTextureSprite(blockId, definition, baseTexture);
                if (baseSprite != null)
                    return baseSprite;
            }

            return null;
        }

        /// <summary>
        /// Lightweight key used by the furniture renderer to know when an
        /// animation frame or runtime state changed.
        /// </summary>
        public static string ResolveTextureName(
            BlockDefinition definition,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (!IsMultiBlock(definition))
                return null;

            string state = BlockVisualStateRuntime.GetState(worldX, worldY, layer);
            BlockVisualStateDefinition stateVisual = FindState(definition, state);

            BlockAnimationDefinition animation =
                stateVisual != null && HasFrames(stateVisual.Animation)
                    ? stateVisual.Animation
                    : definition.Animation;

            if (HasFrames(animation))
            {
                return NormalizeTextureName(
                    animation.Frames[GetFrameIndex(animation, worldX, worldY)]
                );
            }

            if (stateVisual != null && !string.IsNullOrWhiteSpace(stateVisual.Texture))
                return NormalizeTextureName(stateVisual.Texture);

            return GetBaseTextureName(definition);
        }

        private static Sprite GetSpriteForTexture(
            string blockId,
            BlockDefinition definition,
            string textureName)
        {
            if (!IsMultiBlock(definition) || string.IsNullOrWhiteSpace(textureName))
                return null;

            textureName = NormalizeTextureName(textureName);
            if (string.IsNullOrWhiteSpace(textureName))
                return null;

            string cacheKey = "multiblock|" + blockId + "|" + textureName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            string path = Path.Combine(
                Game.Content.DataPaths.GameDataFolder,
                "ResourcePacks",
                "Default",
                "textures",
                "multiblocks",
                textureName + ".png"
            );

            if (missingMultiBlockTextures.Contains(path))
                return null;

            if (!File.Exists(path))
            {
                // State/animation textures are optional and may intentionally live
                // in textures/blocks for old content. Do not spam the Console.
                missingMultiBlockTextures.Add(path);
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
            texture.name = "MultiBlockTexture_" + blockId + "_" + textureName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning(
                    "MULTIBLOCK VISUAL: LoadImage failed for " + blockId + " / " + textureName
                );
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            WarnIfUnexpectedSize(blockId, definition, textureName, texture.width, texture.height);

            Sprite sprite = CreateSprite(texture, blockId, textureName);
            textureCache[cacheKey] = texture;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Compatibility path: permits state images to remain in textures/blocks.
        /// This is useful for existing packs, but new multi-block state textures
        /// should be placed in textures/multiblocks.
        /// </summary>
        private static Sprite GetRegularTextureSprite(
            string blockId,
            BlockDefinition definition,
            string textureName)
        {
            textureName = NormalizeTextureName(textureName);
            if (string.IsNullOrWhiteSpace(textureName))
                return null;

            string cacheKey = "regular|" + blockId + "|" + textureName;
            if (spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            if (!TextureManager.TryGet(textureName, out Texture2D texture) || texture == null)
                return null;

            WarnIfUnexpectedSize(blockId, definition, textureName, texture.width, texture.height);

            Sprite sprite = CreateSprite(texture, blockId, textureName);
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite CreateSprite(
            Texture2D texture,
            string blockId,
            string textureName)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            sprite.name = "MultiBlockSprite_" + blockId + "_" + textureName;
            return sprite;
        }

        private static void WarnIfUnexpectedSize(
            string blockId,
            BlockDefinition definition,
            string textureName,
            int actualWidth,
            int actualHeight)
        {
            int width = Mathf.Max(1, definition.MultiBlock.Width);
            int height = Mathf.Max(1, definition.MultiBlock.Height);
            int expectedWidth = width * 16;
            int expectedHeight = height * 16;

            if (actualWidth == expectedWidth && actualHeight == expectedHeight)
                return;

            Debug.LogWarning(
                "MULTIBLOCK VISUAL: " + blockId + " texture '" + textureName + "' is " +
                actualWidth + "x" + actualHeight + ". For a " + width + "x" + height +
                " multi-block the recommended size is " + expectedWidth + "x" + expectedHeight + "."
            );
        }

        /// <summary>
        /// Returns the 16x16 anchor cell from the normal full multi-block texture.
        /// This is only a compatibility fallback for legacy foreground cells and
        /// block-break particles; normal multi-blocks use one full SpriteRenderer.
        /// </summary>
        public static bool TryGetAnchorCellPixels(
            string blockId,
            BlockDefinition definition,
            out Color32[] pixels)
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
                startY + 16 > texture.height)
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

        private static string GetBaseTextureName(BlockDefinition definition)
        {
            if (definition == null)
                return null;

            if (
                definition.MultiBlock != null &&
                !string.IsNullOrWhiteSpace(definition.MultiBlock.Texture)
            )
            {
                return NormalizeTextureName(definition.MultiBlock.Texture);
            }

            return NormalizeTextureName(definition.Texture);
        }

        private static BlockVisualStateDefinition FindState(
            BlockDefinition definition,
            string state)
        {
            if (
                definition == null ||
                definition.States == null ||
                definition.States.Count == 0 ||
                string.IsNullOrWhiteSpace(state)
            )
            {
                return null;
            }

            for (int i = 0; i < definition.States.Count; i++)
            {
                BlockVisualStateDefinition candidate = definition.States[i];
                if (
                    candidate != null &&
                    !string.IsNullOrWhiteSpace(candidate.State) &&
                    string.Equals(candidate.State, state, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool HasFrames(BlockAnimationDefinition animation)
        {
            return
                animation != null &&
                animation.Frames != null &&
                animation.Frames.Count > 0;
        }

        private static int GetFrameIndex(
            BlockAnimationDefinition animation,
            int worldX,
            int worldY)
        {
            int count = animation.Frames.Count;
            if (count <= 1)
                return 0;

            float fps = Mathf.Max(0.01f, animation.FPS);
            int sequenceLength =
                animation.PingPong && count > 1
                    ? count * 2 - 2
                    : count;

            float phase = Time.time * fps;
            if (animation.RandomStart)
            {
                uint hash = StableHash(worldX, worldY);
                phase += hash % (uint)sequenceLength;
            }

            int sequenceIndex = Mathf.FloorToInt(phase) % sequenceLength;
            if (sequenceIndex < 0)
                sequenceIndex += sequenceLength;

            if (animation.PingPong && sequenceIndex >= count)
                sequenceIndex = sequenceLength - sequenceIndex;

            return Mathf.Clamp(sequenceIndex, 0, count - 1);
        }

        private static uint StableHash(int x, int y)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)x) * 16777619u;
                h = (h ^ (uint)y) * 16777619u;
                return h;
            }
        }

        private static string NormalizeTextureName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

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
