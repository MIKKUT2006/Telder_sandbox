#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TelderCaveVegetationHookInstaller
{
    [MenuItem("Tools/Game/Fix Cave Vegetation Hook")]
    public static void Apply()
    {
        try
        {
            string path = FindWorldGenerator();

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError(
                    "CAVE VEGETATION FIX: WorldGenerator.cs not found."
                );
                return;
            }

            string absolute = ToAbsolutePath(path);

            string source =
                File.ReadAllText(
                    absolute,
                    Encoding.UTF8
                );

            string backup =
                absolute +
                ".cave_vegetation_v2_backup";

            if (!File.Exists(backup))
            {
                File.Copy(
                    absolute,
                    backup,
                    false
                );
            }

            int methodIndex =
                source.IndexOf(
                    "public ChunkData GenerateChunkData(",
                    StringComparison.Ordinal
                );

            if (methodIndex < 0)
            {
                throw new InvalidOperationException(
                    "GenerateChunkData() not found."
                );
            }

            int braceStart =
                source.IndexOf(
                    '{',
                    methodIndex
                );

            int braceEnd =
                FindMatchingBrace(
                    source,
                    braceStart
                );

            string method =
                source.Substring(
                    methodIndex,
                    braceEnd - methodIndex + 1
                );

            const string hookMarker =
                "CaveVegetationGenerationRuntime";

            if (method.Contains(hookMarker))
            {
                // Remove the previous hook block to guarantee a clean one.
                int markerIndex =
                    method.IndexOf(
                        "Game.World.Vegetation.Caves",
                        StringComparison.Ordinal
                    );

                if (markerIndex >= 0)
                {
                    int returnAfterOld =
                        method.IndexOf(
                            "return data;",
                            markerIndex,
                            StringComparison.Ordinal
                        );

                    if (returnAfterOld > markerIndex)
                    {
                        method =
                            method.Remove(
                                markerIndex,
                                returnAfterOld - markerIndex
                            );
                    }
                }
            }

            int returnIndex =
                method.LastIndexOf(
                    "return data;",
                    StringComparison.Ordinal
                );

            if (returnIndex < 0)
            {
                throw new InvalidOperationException(
                    "return data; not found inside GenerateChunkData()."
                );
            }

            const string hook =
@"Game.World.Vegetation.Caves
                .CaveVegetationGenerationRuntime
                .ApplyToChunk(
                    this,
                    settings,
                    data,
                    chunkX,
                    chunkY
                );


            ";

            method =
                method.Insert(
                    returnIndex,
                    hook
                );

            source =
                source.Remove(
                    methodIndex,
                    braceEnd - methodIndex + 1
                )
                .Insert(
                    methodIndex,
                    method
                );

            File.WriteAllText(
                absolute,
                source,
                new UTF8Encoding(false)
            );

            AssetDatabase.Refresh();

            Debug.Log(
                "CAVE VEGETATION FIX: hook installed in " +
                path
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "CAVE VEGETATION FIX FAILED:\n" +
                exception
            );
        }
    }

    private static int FindMatchingBrace(
        string source,
        int start)
    {
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < source.Length; i++)
        {
            char c = source[i];

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                    inString = false;

                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }

        throw new InvalidOperationException(
            "Matching brace not found."
        );
    }

    private static string FindWorldGenerator()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "WorldGenerator t:MonoScript"
            );

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            if (string.Equals(
                Path.GetFileNameWithoutExtension(path),
                "WorldGenerator",
                StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    private static string ToAbsolutePath(
        string assetPath)
    {
        string projectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;

        return
            Path.Combine(
                projectRoot,
                assetPath
            );
    }
}

#endif
