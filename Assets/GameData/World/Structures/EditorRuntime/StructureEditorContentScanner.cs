
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
            public string Texture;
            public List<string> Tags;
            public bool closed;
            public bool Closed;
        }


        [Serializable]
        private class ItemJson
        {
            public string ID;
            public string Name;
            public string Texture;
        }


        [Serializable]
        private class BiomeJson
        {
            public string ID;
            public string Id;
            public string Name;
            public string DisplayName;
            public bool Enabled =
                true;
        }


        public sealed class BlockInfo
        {
            public string ID;
            public string Name;
            public string Texture;

            public bool IsChest;
            public bool Closed;

            // Prebuilt once during content refresh.
            // The palette does not concatenate these strings every frame.
            public string DisplayLabel;
        }


        public sealed class LootContentInfo
        {
            public string ID;
            public string Name;
            public string Texture;

            public bool IsBlock;

            public string DisplayLabel;
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


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                return result;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                try
                {
                    BlockJson block =
                        JsonUtility.FromJson<
                            BlockJson
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        block ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            block.ID
                        )
                    )
                    {
                        continue;
                    }


                    bool isChest =
                        HasTag(
                            block.Tags,
                            "chest"
                        );


                    string name =
                        string.IsNullOrWhiteSpace(
                            block.Name
                        )
                            ? block.ID
                            : block.Name;


                    string display =
                        name +
                        "\n" +
                        block.ID;


                    bool closed =
                        block.closed ||
                        block.Closed;


                    if (
                        isChest
                    )
                    {
                        display +=
                            closed
                                ? "\n[CHEST CLOSED]"
                                : "\n[CHEST]";
                    }


                    result.Add(
                        new BlockInfo
                        {
                            ID =
                                block.ID,

                            Name =
                                name,

                            Texture =
                                block.Texture,

                            IsChest =
                                isChest,

                            Closed =
                                closed,

                            DisplayLabel =
                                display
                        }
                    );
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "STRUCTURE EDITOR: Failed to read block JSON " +
                        files[i] +
                        " | " +
                        exception.Message
                    );
                }
            }


            result.Sort(
                (
                    a,
                    b
                ) =>
                    string.Compare(
                        a.ID,
                        b.ID,
                        StringComparison.OrdinalIgnoreCase
                    )
            );


            return result;
        }


        public static List<LootContentInfo> LoadLootContents(
            IList<BlockInfo> knownBlocks
        )
        {
            Dictionary<
                string,
                LootContentInfo
            > byId =
                new Dictionary<
                    string,
                    LootContentInfo
                >(
                    StringComparer.OrdinalIgnoreCase
                );


            if (
                knownBlocks !=
                null
            )
            {
                for (
                    int i = 0;
                    i < knownBlocks.Count;
                    i++
                )
                {
                    BlockInfo block =
                        knownBlocks[i];


                    if (
                        block ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            block.ID
                        )
                    )
                    {
                        continue;
                    }


                    byId[
                        block.ID
                    ] =
                        new LootContentInfo
                        {
                            ID =
                                block.ID,

                            Name =
                                block.Name,

                            Texture =
                                block.Texture,

                            IsBlock =
                                true,

                            DisplayLabel =
                                "[BLOCK] " +
                                block.Name +
                                "\n" +
                                block.ID
                        };
                }
            }


            string itemFolder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            if (
                Directory.Exists(
                    itemFolder
                )
            )
            {
                string[] files =
                    Directory.GetFiles(
                        itemFolder,
                        "*.json",
                        SearchOption.AllDirectories
                    );


                for (
                    int i = 0;
                    i < files.Length;
                    i++
                )
                {
                    try
                    {
                        ItemJson item =
                            JsonUtility.FromJson<
                                ItemJson
                            >(
                                File.ReadAllText(
                                    files[i]
                                )
                            );


                        if (
                            item ==
                            null
                            ||
                            string.IsNullOrWhiteSpace(
                                item.ID
                            )
                        )
                        {
                            continue;
                        }


                        string name =
                            string.IsNullOrWhiteSpace(
                                item.Name
                            )
                                ? item.ID
                                : item.Name;


                        if (
                            byId.TryGetValue(
                                item.ID,
                                out LootContentInfo existing
                            )
                        )
                        {
                            // If an Item JSON exists for a block, preserve the
                            // block visual source but use the nicer item name.
                            existing.Name =
                                name;


                            existing.DisplayLabel =
                                "[BLOCK] " +
                                name +
                                "\n" +
                                item.ID;


                            continue;
                        }


                        byId.Add(
                            item.ID,

                            new LootContentInfo
                            {
                                ID =
                                    item.ID,

                                Name =
                                    name,

                                Texture =
                                    item.Texture,

                                IsBlock =
                                    false,

                                DisplayLabel =
                                    "[ITEM] " +
                                    name +
                                    "\n" +
                                    item.ID
                            }
                        );
                    }
                    catch (
                        Exception exception
                    )
                    {
                        Debug.LogWarning(
                            "STRUCTURE EDITOR: Failed to read item JSON " +
                            files[i] +
                            " | " +
                            exception.Message
                        );
                    }
                }
            }


            List<LootContentInfo> result =
                new List<LootContentInfo>(
                    byId.Values
                );


            result.Sort(
                (
                    a,
                    b
                ) =>
                    string.Compare(
                        a.ID,
                        b.ID,
                        StringComparison.OrdinalIgnoreCase
                    )
            );


            return result;
        }


        public static List<string> LoadSurfaceBiomeIds()
        {
            return
                LoadBiomeIdsFromFolder(
                    Path.Combine(
                        Application.dataPath,
                        "GameData",
                        "Biomes"
                    )
                );
        }


        public static List<string> LoadCaveBiomeIds()
        {
            return
                LoadBiomeIdsFromFolder(
                    Path.Combine(
                        Application.dataPath,
                        "GameData",
                        "CaveBiomes"
                    )
                );
        }


        // Compatibility for older callers.
        public static List<string> LoadBiomeIds()
        {
            return
                LoadSurfaceBiomeIds();
        }


        private static List<string> LoadBiomeIdsFromFolder(
            string folder
        )
        {
            List<string> result =
                new List<string>();


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                return result;
            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            HashSet<string> unique =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                string fallback =
                    Path.GetFileNameWithoutExtension(
                        files[i]
                    );


                try
                {
                    BiomeJson biome =
                        JsonUtility.FromJson<
                            BiomeJson
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        biome !=
                        null
                        &&
                        !biome.Enabled
                    )
                    {
                        continue;
                    }


                    string id =
                        biome !=
                        null
                        &&
                        !string.IsNullOrWhiteSpace(
                            biome.ID
                        )
                            ? biome.ID
                            : (
                                biome !=
                                null
                                &&
                                !string.IsNullOrWhiteSpace(
                                    biome.Id
                                )
                                    ? biome.Id
                                    : fallback
                            );


                    if (
                        unique.Add(
                            id
                        )
                    )
                    {
                        result.Add(
                            id
                        );
                    }
                }
                catch
                {
                    if (
                        unique.Add(
                            fallback
                        )
                    )
                    {
                        result.Add(
                            fallback
                        );
                    }
                }
            }


            result.Sort(
                StringComparer.OrdinalIgnoreCase
            );


            return result;
        }


        private static bool HasTag(
            List<string> tags,
            string wanted
        )
        {
            if (
                tags ==
                null
                ||
                string.IsNullOrWhiteSpace(
                    wanted
                )
            )
            {
                return false;
            }


            for (
                int i = 0;
                i < tags.Count;
                i++
            )
            {
                if (
                    string.Equals(
                        tags[i],
                        wanted,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
        }
    }
}
