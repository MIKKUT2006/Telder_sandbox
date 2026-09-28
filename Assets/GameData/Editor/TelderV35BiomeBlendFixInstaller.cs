#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderV351BiomeBlendInstaller
{
    private const string MenuPath =
        "Tools/Game/Apply V35.1 Biome Blend Fix";


    private const string Marker =
        "// [TELDER-V35.1-BIOME-MATERIAL-BLEND]";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string assetPath =
                FindScriptPath(
                    "WorldGenerator"
                );


            if (
                string.IsNullOrWhiteSpace(
                    assetPath
                )
            )
            {
                Debug.LogError(
                    "TELDER V35.1: WorldGenerator.cs not found."
                );

                return;
            }


            string absolutePath =
                ToAbsolutePath(
                    assetPath
                );


            string source =
                File.ReadAllText(
                    absolutePath,
                    Encoding.UTF8
                );


            if (
                source.Contains(
                    Marker
                )
            )
            {
                Debug.Log(
                    "TELDER V35.1: biome blending is already installed."
                );

                return;
            }


            string backup =
                absolutePath +
                ".v351_biome_backup";


            if (
                !File.Exists(
                    backup
                )
            )
            {
                File.Copy(
                    absolutePath,
                    backup,
                    false
                );
            }


            source =
                PatchGenerateChunkData(
                    source
                );


            source =
                InsertBlendHelper(
                    source
                );


            File.WriteAllText(
                absolutePath,
                source,
                new UTF8Encoding(
                    false
                )
            );


            AssetDatabase.Refresh();


            Debug.Log(
                "TELDER V35.1: biome material blending installed into " +
                assetPath
            );


            EditorUtility.DisplayDialog(
                "TELDER V35.1",
                "Biome blending установлен.\n\n" +
                "Проверь новые/перегенерированные чанки.\n" +
                "Старые сохранённые чанки сами не изменятся.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER V35.1 FAILED:\n" +
                exception
            );


            EditorUtility.DisplayDialog(
                "TELDER V35.1 — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }


    // =====================================================
    // GENERATE CHUNK DATA
    // =====================================================

    private static string PatchGenerateChunkData(
        string source
    )
    {
        int methodStart =
            source.IndexOf(
                "public ChunkData GenerateChunkData(",
                StringComparison.Ordinal
            );


        if (methodStart < 0)
        {
            throw new InvalidOperationException(
                "WorldGenerator.GenerateChunkData() not found."
            );
        }


        int methodBrace =
            source.IndexOf(
                '{',
                methodStart
            );


        if (methodBrace < 0)
        {
            throw new InvalidOperationException(
                "GenerateChunkData opening brace not found."
            );
        }


        int methodEnd =
            FindMatchingBrace(
                source,
                methodBrace
            );


        string method =
            source.Substring(
                methodStart,
                methodEnd -
                methodStart +
                1
            );


        // -------------------------------------------------
        // Insert cellBiome immediately before foreground
        // generation. This does not depend on how worldY
        // is formatted in the file.
        // -------------------------------------------------

        int foregroundStatement =
            method.IndexOf(
                "ushort foreground",
                StringComparison.Ordinal
            );


        if (foregroundStatement < 0)
        {
            throw new InvalidOperationException(
                "GenerateChunkData: 'ushort foreground' not found."
            );
        }


        int lineStart =
            method.LastIndexOf(
                '\n',
                foregroundStatement
            );


        if (lineStart < 0)
            lineStart = foregroundStatement;


        string indentation =
            GetIndentationAt(
                method,
                foregroundStatement
            );


        string injected =
            indentation +
            "BiomeRuntimeData cellBiome =\n" +
            indentation +
            "    SelectBlendedMaterialBiome(\n" +
            indentation +
            "        worldX,\n" +
            indentation +
            "        worldY,\n" +
            indentation +
            "        sample,\n" +
            indentation +
            "        biome\n" +
            indentation +
            "    );\n\n\n";


        method =
            method.Insert(
                lineStart + 1,
                injected
            );


        // -------------------------------------------------
        // Change ONLY the biome argument of the two terrain
        // generation calls inside this method.
        // -------------------------------------------------

        method =
            ReplaceLastArgumentIdentifier(
                method,
                "GenerateForegroundBlock(",
                "biome",
                "cellBiome"
            );


        method =
            ReplaceLastArgumentIdentifier(
                method,
                "GenerateBackgroundBlock(",
                "biome",
                "cellBiome"
            );


        return
            source.Remove(
                methodStart,
                methodEnd -
                methodStart +
                1
            )
            .Insert(
                methodStart,
                method
            );
    }


    // =====================================================
    // BLEND HELPER
    // =====================================================

    private static string InsertBlendHelper(
        string source
    )
    {
        int insertion =
            source.IndexOf(
                "private ushort GenerateForegroundBlock(",
                StringComparison.Ordinal
            );


        if (insertion < 0)
        {
            throw new InvalidOperationException(
                "GenerateForegroundBlock() insertion point not found."
            );
        }


        string helper =
@"        // [TELDER-V35.1-BIOME-MATERIAL-BLEND]
        private BiomeRuntimeData SelectBlendedMaterialBiome(
            int worldX,
            int worldY,
            BiomeSample sample,
            BiomeRuntimeData fallback
        )
        {
            if (
                sample.Primary ==
                null
            )
            {
                return fallback;
            }


            BiomeRuntimeData primary =
                biomeRuntime.Get(
                    sample.Primary
                );


            if (
                sample.Secondary ==
                null
                ||
                sample.Secondary ==
                sample.Primary
            )
            {
                return
                    primary ??
                    fallback;
            }


            BiomeRuntimeData secondary =
                biomeRuntime.Get(
                    sample.Secondary
                );


            if (primary == null)
            {
                return
                    secondary ??
                    fallback;
            }


            if (secondary == null)
            {
                return primary;
            }


            float blend =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        sample.Blend
                    )
                );


            if (blend <= 0.01f)
            {
                return primary;
            }


            if (blend >= 0.99f)
            {
                return secondary;
            }


            // -------------------------------------------------
            // LARGE ORGANIC PATCHES
            // -------------------------------------------------

            float largeNoise =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        settings.Seed *
                        0.37117f
                    )
                    *
                    0.105f,

                    (
                        worldY +
                        settings.Seed *
                        0.61873f
                    )
                    *
                    0.105f
                );


            // -------------------------------------------------
            // SMALLER EDGE BREAKUP
            // -------------------------------------------------

            float detailNoise =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        settings.Seed *
                        1.17341f
                    )
                    *
                    0.285f,

                    (
                        worldY +
                        settings.Seed *
                        0.84713f
                    )
                    *
                    0.285f
                );


            float organicThreshold =
                Mathf.Clamp01(
                    largeNoise *
                    0.74f
                    +
                    detailNoise *
                    0.26f
                );


            // Near the Primary side only a few Secondary cells appear.
            // Moving through the transition increases their density until
            // Secondary completely takes over.
            return
                blend >=
                organicThreshold
                    ? secondary
                    : primary;
        }


";


        return
            source.Insert(
                insertion,
                helper
            );
    }


    // =====================================================
    // CALL PATCHING
    // =====================================================

    private static string ReplaceLastArgumentIdentifier(
        string source,
        string callName,
        string oldIdentifier,
        string newIdentifier
    )
    {
        int callStart =
            source.IndexOf(
                callName,
                StringComparison.Ordinal
            );


        if (callStart < 0)
        {
            throw new InvalidOperationException(
                "Call not found: " +
                callName
            );
        }


        int openParen =
            source.IndexOf(
                '(',
                callStart
            );


        if (openParen < 0)
        {
            throw new InvalidOperationException(
                "Opening parenthesis not found for " +
                callName
            );
        }


        int closeParen =
            FindMatchingParenthesis(
                source,
                openParen
            );


        string call =
            source.Substring(
                callStart,
                closeParen -
                callStart +
                1
            );


        int identifierIndex =
            LastIdentifierIndex(
                call,
                oldIdentifier
            );


        if (identifierIndex < 0)
        {
            throw new InvalidOperationException(
                "Last argument '" +
                oldIdentifier +
                "' not found in " +
                callName
            );
        }


        string patchedCall =
            call.Remove(
                identifierIndex,
                oldIdentifier.Length
            )
            .Insert(
                identifierIndex,
                newIdentifier
            );


        return
            source.Remove(
                callStart,
                call.Length
            )
            .Insert(
                callStart,
                patchedCall
            );
    }


    private static int LastIdentifierIndex(
        string text,
        string identifier
    )
    {
        int search =
            text.Length;


        while (search > 0)
        {
            int index =
                text.LastIndexOf(
                    identifier,
                    search - 1,
                    StringComparison.Ordinal
                );


            if (index < 0)
                return -1;


            bool leftOkay =
                index == 0
                ||
                !IsIdentifierChar(
                    text[index - 1]
                );


            int rightIndex =
                index +
                identifier.Length;


            bool rightOkay =
                rightIndex >=
                text.Length
                ||
                !IsIdentifierChar(
                    text[rightIndex]
                );


            if (
                leftOkay
                &&
                rightOkay
            )
            {
                return index;
            }


            search =
                index;
        }


        return -1;
    }


    private static bool IsIdentifierChar(
        char c
    )
    {
        return
            char.IsLetterOrDigit(c)
            ||
            c == '_';
    }


    // =====================================================
    // PARSING HELPERS
    // =====================================================

    private static string GetIndentationAt(
        string source,
        int index
    )
    {
        int lineStart =
            source.LastIndexOf(
                '\n',
                Mathf.Max(
                    0,
                    index - 1
                )
            );


        lineStart =
            lineStart < 0
                ? 0
                : lineStart + 1;


        int cursor =
            lineStart;


        while (
            cursor <
            source.Length
            &&
            (
                source[cursor] == ' '
                ||
                source[cursor] == '\t'
            )
        )
        {
            cursor++;
        }


        return
            source.Substring(
                lineStart,
                cursor -
                lineStart
            );
    }


    private static int FindMatchingParenthesis(
        string source,
        int openingParen
    )
    {
        int depth =
            0;


        bool inString =
            false;


        bool inChar =
            false;


        bool escape =
            false;


        for (
            int i = openingParen;
            i < source.Length;
            i++
        )
        {
            char c =
                source[i];


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


            if (inChar)
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


                if (c == '\'')
                    inChar = false;


                continue;
            }


            if (c == '"')
            {
                inString = true;
                continue;
            }


            if (c == '\'')
            {
                inChar = true;
                continue;
            }


            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;


                if (depth == 0)
                    return i;
            }
        }


        throw new InvalidOperationException(
            "Matching parenthesis not found."
        );
    }


    private static int FindMatchingBrace(
        string source,
        int openingBrace
    )
    {
        int depth =
            0;


        bool inString =
            false;


        bool inChar =
            false;


        bool escape =
            false;


        bool lineComment =
            false;


        bool blockComment =
            false;


        for (
            int i = openingBrace;
            i < source.Length;
            i++
        )
        {
            char c =
                source[i];


            char next =
                i + 1 <
                source.Length
                    ? source[i + 1]
                    : '\0';


            if (lineComment)
            {
                if (c == '\n')
                    lineComment = false;


                continue;
            }


            if (blockComment)
            {
                if (
                    c == '*'
                    &&
                    next == '/'
                )
                {
                    blockComment = false;
                    i++;
                }


                continue;
            }


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


            if (inChar)
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


                if (c == '\'')
                    inChar = false;


                continue;
            }


            if (
                c == '/'
                &&
                next == '/'
            )
            {
                lineComment = true;
                i++;
                continue;
            }


            if (
                c == '/'
                &&
                next == '*'
            )
            {
                blockComment = true;
                i++;
                continue;
            }


            if (c == '"')
            {
                inString = true;
                continue;
            }


            if (c == '\'')
            {
                inChar = true;
                continue;
            }


            if (c == '{')
            {
                depth++;
            }
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


    // =====================================================
    // FILE HELPERS
    // =====================================================

    private static string FindScriptPath(
        string className
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className +
                " t:MonoScript"
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


            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(
                        path
                    ),
                    className,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return path;
            }
        }


        return null;
    }


    private static string ToAbsolutePath(
        string assetPath
    )
    {
        string projectRoot =
            Directory
                .GetParent(
                    Application.dataPath
                )
                .FullName;


        return
            Path.Combine(
                projectRoot,
                assetPath
            );
    }
}

#endif
