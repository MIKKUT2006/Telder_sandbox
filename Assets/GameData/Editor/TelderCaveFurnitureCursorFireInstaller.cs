#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TelderCaveFurnitureCursorFireInstaller
{
    [MenuItem("Tools/Game/Apply Cave Furniture + Cursor + Fire Fix")]
    public static void Apply()
    {
        try
        {
            string worldGeneratorPath = FindScriptPath("WorldGenerator");

            if (string.IsNullOrWhiteSpace(worldGeneratorPath))
            {
                Debug.LogError("TELDER FIX: WorldGenerator.cs not found.");
                return;
            }

            PatchWorldGenerator(worldGeneratorPath);

            AssetDatabase.Refresh();

            Debug.Log(
                "TELDER FIX: Cave furniture vegetation + cursor + fire FX applied."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER FIX FAILED:\n" +
                exception
            );
        }
    }

    private static void PatchWorldGenerator(string assetPath)
    {
        string absolute = ToAbsolutePath(assetPath);
        string source = File.ReadAllText(absolute, Encoding.UTF8);

        string backup = absolute + ".cave_furniture_fix_backup";

        if (!File.Exists(backup))
        {
            File.Copy(absolute, backup, false);
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

        int braceStart = source.IndexOf('{', methodIndex);
        int braceEnd = FindMatchingBrace(source, braceStart);

        string method =
            source.Substring(
                methodIndex,
                braceEnd - methodIndex + 1
            );

        int existingIndex =
            method.IndexOf(
                "Game.World.Vegetation.Caves",
                StringComparison.Ordinal
            );

        if (existingIndex >= 0)
        {
            int returnIndexOld =
                method.IndexOf(
                    "return data;",
                    existingIndex,
                    StringComparison.Ordinal
                );

            if (returnIndexOld > existingIndex)
            {
                method =
                    method.Remove(
                        existingIndex,
                        returnIndexOld - existingIndex
                    );
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
                "return data; not found."
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

        method = method.Insert(returnIndex, hook);

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
    }

    private static int FindMatchingBrace(string source, int start)
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

    private static string FindScriptPath(string className)
    {
        string[] guids =
            AssetDatabase.FindAssets(className + " t:MonoScript");

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guids[i]);

            if (string.Equals(
                Path.GetFileNameWithoutExtension(path),
                className,
                StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot =
            Directory.GetParent(Application.dataPath).FullName;

        return Path.Combine(projectRoot, assetPath);
    }
}

#endif
