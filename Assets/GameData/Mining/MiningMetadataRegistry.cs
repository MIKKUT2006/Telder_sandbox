
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace Game.Mining
{

    [Serializable]
    public class ToolMetadata
    {

        public string ID;

        public string ToolType;

        public int ToolLevel;

        public float MiningSpeed =
            1f;


        public float HeldScale =
            1f;


        public float HeldRotation;


        public float HeldOffsetX;

        public float HeldOffsetY;

    }


    [Serializable]
    public class BlockMiningMetadata
    {

        public string ID;

        public string RequiredTool;

        public int RequiredToolLevel;

    }


    public static class MiningMetadataRegistry
    {

        private static readonly Dictionary<
            string,
            ToolMetadata
        > tools =
            new Dictionary<
                string,
                ToolMetadata
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private static readonly Dictionary<
            string,
            BlockMiningMetadata
        > blocks =
            new Dictionary<
                string,
                BlockMiningMetadata
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private static bool loaded;


        public static void Reload()
        {

            loaded =
                true;


            tools.Clear();

            blocks.Clear();


            LoadTools();

            LoadBlocks();

        }


        public static bool TryGetTool(
            string itemId,
            out ToolMetadata tool
        )
        {

            EnsureLoaded();


            tool =
                null;


            if (
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {

                return false;

            }


            return
                tools.TryGetValue(
                    itemId,
                    out tool
                )
                &&
                tool !=
                null;

        }


        public static bool TryGetBlock(
            string blockId,
            out BlockMiningMetadata metadata
        )
        {

            EnsureLoaded();


            metadata =
                null;


            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {

                return false;

            }


            return
                blocks.TryGetValue(
                    blockId,
                    out metadata
                )
                &&
                metadata !=
                null;

        }


        private static void EnsureLoaded()
        {

            if (
                loaded
            )
            {

                return;

            }


            Reload();

        }


        private static void LoadTools()
        {

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {

                return;

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

                    ToolMetadata metadata =
                        JsonUtility.FromJson<
                            ToolMetadata
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        metadata ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            metadata.ID
                        )
                        ||
                        string.IsNullOrWhiteSpace(
                            metadata.ToolType
                        )
                    )
                    {

                        continue;

                    }


                    metadata.ToolLevel =
                        Mathf.Max(
                            0,
                            metadata.ToolLevel
                        );


                    metadata.MiningSpeed =
                        Mathf.Max(
                            0.05f,
                            metadata.MiningSpeed
                        );


                    metadata.HeldScale =
                        Mathf.Max(
                            0.01f,
                            metadata.HeldScale
                        );


                    tools[
                        metadata.ID
                    ] =
                        metadata;

                }
                catch (
                    Exception exception
                )
                {

                    Debug.LogWarning(
                        "MINING METADATA: failed item JSON " +
                        files[i] +
                        "\n" +
                        exception.Message
                    );

                }

            }

        }


        private static void LoadBlocks()
        {

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

                return;

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

                    BlockMiningMetadata metadata =
                        JsonUtility.FromJson<
                            BlockMiningMetadata
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        metadata ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            metadata.ID
                        )
                    )
                    {

                        continue;

                    }


                    metadata.RequiredToolLevel =
                        Mathf.Max(
                            0,
                            metadata.RequiredToolLevel
                        );


                    blocks[
                        metadata.ID
                    ] =
                        metadata;

                }
                catch (
                    Exception exception
                )
                {

                    Debug.LogWarning(
                        "MINING METADATA: failed block JSON " +
                        files[i] +
                        "\n" +
                        exception.Message
                    );

                }

            }

        }

    }

}
