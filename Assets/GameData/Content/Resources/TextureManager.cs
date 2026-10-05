using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Resources
{
    public static class TextureManager
    {
        private static readonly Dictionary<string, Texture2D> textures =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        public static void LoadBlockTexture(
            string name,
            string path)
        {
            string key = NormalizeName(name);
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!File.Exists(path))
            {
                Debug.LogWarning("Texture not found: " + path);
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            if (!texture.LoadImage(data, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("Failed to load texture: " + path);
                return;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            if (textures.TryGetValue(key, out Texture2D old) && old != null && old != texture)
                UnityEngine.Object.Destroy(old);

            textures[key] = texture;
        }

        public static bool TryGet(
            string name,
            out Texture2D texture)
        {
            texture = null;
            string key = NormalizeName(name);
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return textures.TryGetValue(key, out texture) && texture != null;
        }

        public static Texture2D Get(string name)
        {
            if (TryGet(name, out Texture2D texture))
                return texture;

            Debug.LogWarning("Texture missing: " + name);
            return null;
        }

        public static void Clear()
        {
            foreach (KeyValuePair<string, Texture2D> pair in textures)
            {
                if (pair.Value != null)
                    UnityEngine.Object.Destroy(pair.Value);
            }

            textures.Clear();
            Game.World.Rendering.BlockVisualResolver.ClearCaches();
            Game.World.Rendering.BlockRenderer.ClearCache();
        }

        private static string NormalizeName(string value)
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
