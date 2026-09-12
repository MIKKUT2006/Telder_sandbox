
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Game.World.Structures.EditorRuntime
{
    public static class StructureEditorIconCache
    {
        private static readonly Dictionary<
            string,
            Texture2D
        > cache =
            new Dictionary<
                string,
                Texture2D
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private static readonly Dictionary<
            string,
            StructureEditorContentScanner.BlockInfo
        > blocksById =
            new Dictionary<
                string,
                StructureEditorContentScanner.BlockInfo
            >(
                StringComparer.OrdinalIgnoreCase
            );


        public static void Configure(
            IList<
                StructureEditorContentScanner.BlockInfo
            > blocks
        )
        {
            blocksById.Clear();


            if (
                blocks ==
                null
            )
            {
                return;
            }


            for (
                int i = 0;
                i < blocks.Count;
                i++
            )
            {
                StructureEditorContentScanner.BlockInfo block =
                    blocks[i];


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


                blocksById[
                    block.ID
                ] =
                    block;
            }
        }


        public static Texture2D Get(
            string blockId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {
                return null;
            }


            if (
                cache.TryGetValue(
                    blockId,
                    out Texture2D cached
                )
            )
            {
                return cached;
            }


            if (
                !blocksById.TryGetValue(
                    blockId,
                    out StructureEditorContentScanner.BlockInfo info
                )
            )
            {
                // IMPORTANT:
                // Do not rescan all block JSON here.
                //
                // Old implementation called LoadBlocks() for every
                // grid cell icon, which caused repeated
                // Directory.GetFiles + File.ReadAllText during OnGUI.
                cache[
                    blockId
                ] =
                    null;


                return null;
            }


            return
                Get(
                    info
                );
        }


        public static Texture2D Get(
            StructureEditorContentScanner.BlockInfo info
        )
        {
            if (
                info ==
                null
                ||
                string.IsNullOrWhiteSpace(
                    info.ID
                )
            )
            {
                return null;
            }


            if (
                cache.TryGetValue(
                    info.ID,
                    out Texture2D cached
                )
            )
            {
                return cached;
            }


#if UNITY_EDITOR
            string key =
                string.IsNullOrWhiteSpace(
                    info.Texture
                )
                    ? ShortId(
                        info.ID
                    )
                    : info.Texture.Trim();


            if (
                key.EndsWith(
                    ".png",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                key =
                    key.Substring(
                        0,
                        key.Length -
                        4
                    );
            }


            string directPath =
                "Assets/GameData/ResourcePacks/Default/textures/blocks/" +
                key.Replace(
                    "\\",
                    "/"
                ) +
                ".png";


            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<
                    Texture2D
                >(
                    directPath
                );


            if (
                texture ==
                null
            )
            {
                // Fallback only when the expected resource-pack path
                // does not exist. This is now a rare one-time operation.
                string shortKey =
                    Path.GetFileName(
                        key
                    );


                string[] guids =
                    AssetDatabase.FindAssets(
                        shortKey +
                        " t:Texture2D",

                        new[]
                        {
                            "Assets/GameData/ResourcePacks/Default/textures/blocks"
                        }
                    );


                for (
                    int i = 0;
                    i < guids.Length;
                    i++
                )
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(
                            guids[i]
                        );


                    Texture2D candidate =
                        AssetDatabase.LoadAssetAtPath<
                            Texture2D
                        >(
                            path
                        );


                    if (
                        candidate ==
                        null
                    )
                    {
                        continue;
                    }


                    texture =
                        candidate;


                    string file =
                        Path.GetFileNameWithoutExtension(
                            path
                        );


                    if (
                        string.Equals(
                            file,
                            shortKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        break;
                    }
                }
            }


            cache[
                info.ID
            ] =
                texture;


            return texture;
#else
            return null;
#endif
        }


        private static string ShortId(
            string id
        )
        {
            int colon =
                id.IndexOf(
                    ':'
                );


            return
                colon >=
                0
                &&
                colon <
                id.Length -
                1
                    ? id.Substring(
                        colon +
                        1
                    )
                    : id;
        }


        public static void Clear()
        {
            cache.Clear();
        }
    }
}
