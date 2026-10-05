using System;
using System.Collections.Generic;
using System.IO;
using Game.Blocks;
using Game.Content;

namespace Game.Resources
{
    public static class BlockTextureLoader
    {
        public static void Load()
        {
            string folder = Path.Combine(
                DataPaths.ResourcePacksFolder,
                "Default",
                "textures",
                "blocks"
            );

            Dictionary<string, string> files = BuildTextureIndex(folder);
            HashSet<string> loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BlockDefinition block in BlockRegistry.GetAll())
            {
                if (block == null)
                    continue;

                bool isMultiBlock =
                    block.MultiBlock != null &&
                    (block.MultiBlock.Width > 1 || block.MultiBlock.Height > 1);

                // The normal Texture of a multi-block may intentionally exist only
                // as a full-size image in textures/multiblocks. Do not warn for it.
                LoadReference(
                    block.Texture,
                    folder,
                    files,
                    loaded,
                    !isMultiBlock
                );

                LoadAnimation(block.Animation, folder, files, loaded);

                if (block.States == null)
                    continue;

                for (int i = 0; i < block.States.Count; i++)
                {
                    BlockVisualStateDefinition state = block.States[i];
                    if (state == null)
                        continue;

                    LoadReference(state.Texture, folder, files, loaded, false);
                    LoadAnimation(state.Animation, folder, files, loaded);
                }
            }
        }

        private static void LoadAnimation(
            BlockAnimationDefinition animation,
            string folder,
            Dictionary<string, string> files,
            HashSet<string> loaded)
        {
            if (animation == null || animation.Frames == null)
                return;

            for (int i = 0; i < animation.Frames.Count; i++)
                LoadReference(animation.Frames[i], folder, files, loaded, false);
        }

        private static void LoadReference(
            string textureName,
            string folder,
            Dictionary<string, string> files,
            HashSet<string> loaded,
            bool warnIfMissing)
        {
            string normalized = Normalize(textureName);
            if (string.IsNullOrWhiteSpace(normalized) || loaded.Contains(normalized))
                return;

            if (!files.TryGetValue(normalized, out string path))
            {
                if (warnIfMissing)
                {
                    UnityEngine.Debug.LogWarning(
                        "Texture not found: " + Path.Combine(folder, normalized + ".png")
                    );
                }
                return;
            }

            TextureManager.LoadBlockTexture(normalized, path);
            loaded.Add(normalized);
        }

        private static Dictionary<string, string> BuildTextureIndex(string folder)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(folder))
                return result;

            string[] paths = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.Equals(
                    Path.GetExtension(paths[i]),
                    ".png",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    continue;
                }

                string key = Path.GetFileNameWithoutExtension(paths[i]);
                if (!string.IsNullOrWhiteSpace(key) && !result.ContainsKey(key))
                    result.Add(key, paths[i]);
            }

            return result;
        }

        private static string Normalize(string value)
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
