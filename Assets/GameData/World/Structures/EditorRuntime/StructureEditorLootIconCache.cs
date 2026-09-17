
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Game.World.Structures.EditorRuntime
{
    public static class StructureEditorLootIconCache
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


        public static Texture2D Get(
            StructureEditorContentScanner.LootContentInfo info
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


            if (
                info.IsBlock
            )
            {
                Texture2D blockTexture =
                    StructureEditorIconCache.Get(
                        info.ID
                    );


                cache[
                    info.ID
                ] =
                    blockTexture;


                return blockTexture;
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


            string path =
                "Assets/GameData/ResourcePacks/Default/textures/items/" +
                key.Replace(
                    "\\",
                    "/"
                ) +
                ".png";


            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<
                    Texture2D
                >(
                    path
                );


            if (
                texture ==
                null
            )
            {
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
                            "Assets/GameData/ResourcePacks/Default/textures/items"
                        }
                    );


                for (
                    int i = 0;
                    i < guids.Length;
                    i++
                )
                {
                    string candidatePath =
                        AssetDatabase.GUIDToAssetPath(
                            guids[i]
                        );


                    Texture2D candidate =
                        AssetDatabase.LoadAssetAtPath<
                            Texture2D
                        >(
                            candidatePath
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


                    if (
                        string.Equals(
                            Path.GetFileNameWithoutExtension(
                                candidatePath
                            ),
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


        public static void Clear()
        {
            cache.Clear();
        }


        private static string ShortId(
            string id
        )
        {
            int separator =
                id.IndexOf(
                    ':'
                );


            return
                separator >=
                0
                &&
                separator <
                id.Length -
                1
                    ? id.Substring(
                        separator +
                        1
                    )
                    : id;
        }
    }
}
