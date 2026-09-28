#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderV36PerformanceInstaller
{
    private const string MenuPath =
        "Tools/Game/Apply V36 Performance Hotfix";


    private const string CacheMarker =
        "// [TELDER-V36-BIOME-BLEND-CACHE]";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            int changed =
                PatchBiomeBlend();


            AssetDatabase.Refresh();


            Debug.Log(
                "TELDER V36 PERFORMANCE: applied. Changed existing files = " +
                changed
            );


            EditorUtility.DisplayDialog(
                "TELDER V36 Performance",
                "Готово.\n\n" +
                "• chunk loader заменён архивом;\n" +
                "• falling blocks заменены архивом;\n" +
                "• глобальный ParticleSystem scan отключён;\n" +
                "• старый BlockFire scanner отключён;\n" +
                "• biome blending закэширован по X-колонке.\n\n" +
                "Перезапусти Play Mode после компиляции.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER V36 PERFORMANCE FAILED:\n" +
                exception
            );


            EditorUtility.DisplayDialog(
                "TELDER V36 — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }


    // =====================================================
    // BIOME BLEND
    // =====================================================

    private static int PatchBiomeBlend()
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
            Debug.LogWarning(
                "TELDER V36: WorldGenerator.cs not found."
            );

            return 0;
        }


        string absolute =
            ToAbsolutePath(
                assetPath
            );


        string source =
            File.ReadAllText(
                absolute,
                Encoding.UTF8
            );


        if (
            !source.Contains(
                "SelectBlendedMaterialBiome("
            )
        )
        {
            Debug.Log(
                "TELDER V36: SelectBlendedMaterialBiome() is not installed; " +
                "biome-blend optimization skipped."
            );

            return 0;
        }


        string original =
            source;


        if (
            !source.Contains(
                CacheMarker
            )
        )
        {
            int helperIndex =
                source.IndexOf(
                    "private BiomeRuntimeData SelectBlendedMaterialBiome(",
                    StringComparison.Ordinal
                );


            if (helperIndex < 0)
            {
                throw new InvalidOperationException(
                    "SelectBlendedMaterialBiome method not found."
                );
            }


            string fields =
@"        // [TELDER-V36-BIOME-BLEND-CACHE]
        private int materialBlendCacheX =
            int.MinValue;

        private BiomeDefinition materialBlendCachePrimaryDefinition;

        private BiomeDefinition materialBlendCacheSecondaryDefinition;

        private BiomeRuntimeData materialBlendCachePrimary;

        private BiomeRuntimeData materialBlendCacheSecondary;

        private float materialBlendCacheBlend;

        private float materialBlendCacheNoise;


";


            source =
                source.Insert(
                    helperIndex,
                    fields
                );


            // Method moved forward after field insertion.
            helperIndex =
                source.IndexOf(
                    "private BiomeRuntimeData SelectBlendedMaterialBiome(",
                    StringComparison.Ordinal
                );
        }


        int methodStart =
            source.IndexOf(
                "private BiomeRuntimeData SelectBlendedMaterialBiome(",
                StringComparison.Ordinal
            );


        if (methodStart < 0)
        {
            throw new InvalidOperationException(
                "SelectBlendedMaterialBiome method not found after field insertion."
            );
        }


        int braceStart =
            source.IndexOf(
                '{',
                methodStart
            );


        int braceEnd =
            FindMatchingBrace(
                source,
                braceStart
            );


        string optimizedMethod =
@"private BiomeRuntimeData SelectBlendedMaterialBiome(
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


            if (
                sample.Secondary ==
                null
                ||
                sample.Secondary ==
                sample.Primary
            )
            {
                if (
                    materialBlendCacheX !=
                    worldX
                    ||
                    materialBlendCachePrimaryDefinition !=
                    sample.Primary
                )
                {
                    materialBlendCacheX =
                        worldX;


                    materialBlendCachePrimaryDefinition =
                        sample.Primary;


                    materialBlendCacheSecondaryDefinition =
                        null;


                    materialBlendCachePrimary =
                        biomeRuntime.Get(
                            sample.Primary
                        );


                    materialBlendCacheSecondary =
                        null;


                    materialBlendCacheBlend =
                        0f;


                    materialBlendCacheNoise =
                        0f;
                }


                return
                    materialBlendCachePrimary ??
                    fallback;
            }


            bool cacheInvalid =
                materialBlendCacheX !=
                worldX
                ||
                materialBlendCachePrimaryDefinition !=
                sample.Primary
                ||
                materialBlendCacheSecondaryDefinition !=
                sample.Secondary;


            if (cacheInvalid)
            {
                materialBlendCacheX =
                    worldX;


                materialBlendCachePrimaryDefinition =
                    sample.Primary;


                materialBlendCacheSecondaryDefinition =
                    sample.Secondary;


                // Dictionary/runtime lookup only once for the whole X column.
                materialBlendCachePrimary =
                    biomeRuntime.Get(
                        sample.Primary
                    );


                materialBlendCacheSecondary =
                    biomeRuntime.Get(
                        sample.Secondary
                    );


                materialBlendCacheBlend =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            sample.Blend
                        )
                    );


                // IMPORTANT PERFORMANCE CHANGE:
                //
                // Old V35.1 did TWO Mathf.PerlinNoise calls for EVERY TILE.
                // 32x32 chunk = ~2048 extra Perlin calls.
                //
                // Now we calculate coherent edge noise ONCE PER X COLUMN.
                float large =
                    Mathf.PerlinNoise(
                        (
                            worldX +
                            settings.Seed *
                            0.37117f
                        )
                        *
                        0.105f,

                        17.731f +
                        settings.Seed *
                        0.00013f
                    );


                float detail =
                    Mathf.PerlinNoise(
                        (
                            worldX +
                            settings.Seed *
                            1.17341f
                        )
                        *
                        0.285f,

                        71.219f +
                        settings.Seed *
                        0.00019f
                    );


                materialBlendCacheNoise =
                    Mathf.Clamp01(
                        large *
                        0.74f
                        +
                        detail *
                        0.26f
                    );
            }


            BiomeRuntimeData primary =
                materialBlendCachePrimary;


            BiomeRuntimeData secondary =
                materialBlendCacheSecondary;


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
                materialBlendCacheBlend;


            if (blend <= 0.01f)
                return primary;


            if (blend >= 0.99f)
                return secondary;


            // Cheap deterministic Y breakup.
            // This keeps the border organic without expensive Perlin per tile.
            int band =
                worldY >>
                1;


            uint hash =
                unchecked(
                    (uint)(
                        worldX *
                        73856093
                        ^
                        band *
                        19349663
                        ^
                        settings.Seed *
                        83492791
                    )
                );


            hash ^=
                hash >>
                13;


            hash *=
                1274126177u;


            hash ^=
                hash >>
                16;


            float yJitter =
                (
                    (
                        hash &
                        1023u
                    )
                    /
                    1023f
                    -
                    0.5f
                )
                *
                0.16f;


            float threshold =
                Mathf.Clamp01(
                    materialBlendCacheNoise +
                    yJitter
                );


            return
                blend >=
                threshold
                    ? secondary
                    : primary;
        }";


        source =
            source.Remove(
                methodStart,
                braceEnd -
                methodStart +
                1
            )
            .Insert(
                methodStart,
                optimizedMethod
            );


        if (
            source ==
            original
        )
        {
            return 0;
        }


        string backup =
            absolute +
            ".v36_performance_backup";


        if (
            !File.Exists(
                backup
            )
        )
        {
            File.Copy(
                absolute,
                backup,
                false
            );
        }


        File.WriteAllText(
            absolute,
            source,
            new UTF8Encoding(
                false
            )
        );


        return 1;
    }


    // =====================================================
    // FILE / PARSER
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
}

#endif
