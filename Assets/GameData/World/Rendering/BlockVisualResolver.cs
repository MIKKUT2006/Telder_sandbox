using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Blocks;
using Game.Resources;

namespace Game.World.Rendering
{
    /// <summary>
    /// Resolves the currently visible texture for a block from:
    /// 1) runtime state ("burning", "open", ...),
    /// 2) optional animation frames,
    /// 3) the normal BlockDefinition.Texture fallback.
    /// </summary>
    public static class BlockVisualResolver
    {
        private static readonly Dictionary<string, Sprite> spriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static bool IsDynamic(ushort blockId)
        {
            if (blockId == 0 || !BlockDatabase.Contains(blockId))
                return false;

            return IsDynamic(BlockDatabase.Get(blockId));
        }

        public static bool IsDynamic(BlockDefinition block)
        {
            if (block == null)
                return false;

            if (HasFrames(block.Animation))
                return true;

            return block.States != null && block.States.Count > 0;
        }

        public static string ResolveTextureName(
            BlockDefinition block,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (block == null)
                return null;

            string state = BlockVisualStateRuntime.GetState(worldX, worldY, layer);
            BlockVisualStateDefinition stateVisual = FindState(block, state);

            string staticTexture =
                stateVisual != null && !string.IsNullOrWhiteSpace(stateVisual.Texture)
                    ? stateVisual.Texture
                    : block.Texture;

            BlockAnimationDefinition animation =
                stateVisual != null && HasFrames(stateVisual.Animation)
                    ? stateVisual.Animation
                    : block.Animation;

            string fallbackTexture = NormalizeTextureName(staticTexture);

            if (!HasFrames(animation))
            {
                if (
                    !string.IsNullOrWhiteSpace(fallbackTexture) &&
                    TextureManager.TryGet(fallbackTexture, out Texture2D availableStaticTexture)
                )
                {
                    return fallbackTexture;
                }

                return NormalizeTextureName(block.Texture);
            }

            int frameIndex = GetFrameIndex(animation, worldX, worldY);
            string frame = NormalizeTextureName(animation.Frames[frameIndex]);

            // Missing optional frames are treated as the static texture. This is
            // useful while art is incomplete and also avoids redrawing the chunk
            // every animation tick for files that do not exist yet.
            if (
                !string.IsNullOrWhiteSpace(frame) &&
                TextureManager.TryGet(frame, out Texture2D availableFrameTexture)
            )
            {
                return frame;
            }

            if (
                !string.IsNullOrWhiteSpace(fallbackTexture) &&
                TextureManager.TryGet(fallbackTexture, out Texture2D availableFallbackTexture)
            )
            {
                return fallbackTexture;
            }

            return NormalizeTextureName(block.Texture);
        }

        public static Sprite ResolveSprite(
            BlockDefinition block,
            int worldX,
            int worldY,
            BlockVisualLayer layer)
        {
            if (block == null)
                return null;

            string textureName = ResolveTextureName(block, worldX, worldY, layer);
            if (string.IsNullOrWhiteSpace(textureName))
                return null;

            if (spriteCache.TryGetValue(textureName, out Sprite cached) && cached != null)
                return cached;

            if (!TextureManager.TryGet(textureName, out Texture2D texture) || texture == null)
            {
                // A missing optional state/animation frame must not make the block disappear.
                string fallback = NormalizeTextureName(block.Texture);
                if (
                    string.IsNullOrWhiteSpace(fallback) ||
                    !TextureManager.TryGet(fallback, out texture) ||
                    texture == null
                )
                {
                    return null;
                }

                textureName = fallback;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect
            );

            sprite.name = "BlockVisual_" + textureName;
            spriteCache[textureName] = sprite;
            return sprite;
        }

        public static void ClearCaches()
        {
            foreach (KeyValuePair<string, Sprite> pair in spriteCache)
            {
                if (pair.Value != null)
                    UnityEngine.Object.Destroy(pair.Value);
            }
            spriteCache.Clear();
        }

        private static BlockVisualStateDefinition FindState(
            BlockDefinition block,
            string state)
        {
            if (
                block == null ||
                block.States == null ||
                block.States.Count == 0 ||
                string.IsNullOrWhiteSpace(state)
            )
            {
                return null;
            }

            for (int i = 0; i < block.States.Count; i++)
            {
                BlockVisualStateDefinition candidate = block.States[i];
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

            int sequenceIndex = Mathf.FloorToInt(phase);
            sequenceIndex %= sequenceLength;
            if (sequenceIndex < 0)
                sequenceIndex += sequenceLength;

            if (animation.PingPong && sequenceIndex >= count)
                return sequenceLength - sequenceIndex;

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
