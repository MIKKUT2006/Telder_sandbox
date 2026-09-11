
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Structures.EditorRuntime
{
    public static class StructureEditorContentScanner
    {
        [Serializable]
        private class BlockJson
        {
            public string ID;
            public string Name;
            public List<string> Tags;
            public bool closed;
            public bool Closed;
        }

        [Serializable]
        private class BiomeJson
        {
            public string ID;
            public string Id;
            public string Name;
            public string DisplayName;
        }

        public sealed class BlockInfo
        {
            public string ID;
            public string Name;
            public bool IsChest;
            public bool closed;
            public bool Closed;
        }

        public static List<BlockInfo> LoadBlocks()
        {
            List<BlockInfo> result =
                new List<BlockInfo>();

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Blocks"
                );

            if (!Directory.Exists(folder))
                return result;

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    BlockJson block =
                        JsonUtility.FromJson<BlockJson>(
                            File.ReadAllText(files[i])
                        );

                    if (block == null ||
                        string.IsNullOrWhiteSpace(block.ID))
                        continue;

                    bool isChest = false;

                    if (block.Tags != null)
                    {
                        for (int t = 0;
                             t < block.Tags.Count;
                             t++)
                        {
                            if (string.Equals(
                                block.Tags[t],
                                "chest",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                isChest = true;
                                break;
                            }
                        }
                    }

                    result.Add(
                        new BlockInfo
                        {
                            ID = block.ID,
                            Name =
                                string.IsNullOrWhiteSpace(block.Name)
                                    ? block.ID
                                    : block.Name,
                            IsChest = isChest,
                            Closed = block.closed || block.Closed
                        }
                    );
                }
                catch
                {
                }
            }

            result.Sort(
                (a, b) =>
                    string.Compare(
                        a.ID,
                        b.ID,
                        StringComparison.OrdinalIgnoreCase
                    )
            );

            return result;
        }

        public static List<string> LoadBiomeIds()
        {
            List<string> result =
                new List<string>();

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Biomes"
                );

            if (!Directory.Exists(folder))
                return result;

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            for (int i = 0; i < files.Length; i++)
            {
                string fallback =
                    Path.GetFileNameWithoutExtension(
                        files[i]
                    );

                try
                {
                    BiomeJson biome =
                        JsonUtility.FromJson<BiomeJson>(
                            File.ReadAllText(files[i])
                        );

                    string id =
                        biome != null &&
                        !string.IsNullOrWhiteSpace(biome.ID)
                            ? biome.ID
                            : (
                                biome != null &&
                                !string.IsNullOrWhiteSpace(biome.Id)
                                    ? biome.Id
                                    : fallback
                            );

                    if (!result.Contains(id))
                        result.Add(id);
                }
                catch
                {
                    if (!result.Contains(fallback))
                        result.Add(fallback);
                }
            }

            result.Sort();
            return result;
        }
    }
}
