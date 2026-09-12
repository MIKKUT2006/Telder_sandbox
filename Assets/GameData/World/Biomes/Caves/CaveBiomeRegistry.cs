
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;

using Game.Content;


namespace Game.World.Biomes.Caves
{
    internal sealed class CaveBiomeRuntimeData
    {
        public CaveBiomeDefinition Definition;

        public ushort StoneBlockId;
        public ushort BackgroundBlockId;
        public ushort FloorBlockId;
        public ushort CeilingBlockId;
    }


    public static class CaveBiomeRegistry
    {
        private static readonly List<
            CaveBiomeRuntimeData
        > biomes =
            new List<
                CaveBiomeRuntimeData
            >();


        private static bool loaded;


        public static void Reload()
        {
            loaded =
                false;


            biomes.Clear();


            EnsureLoaded();
        }


        internal static CaveBiomeRuntimeData FindAt(
            int worldX,
            int worldY,
            int worldSeed,
            string[] dimensionKeys
        )
        {
            EnsureLoaded();


            CaveBiomeRuntimeData best =
                null;


            int bestPriority =
                int.MinValue;


            for (
                int i = 0;
                i < biomes.Count;
                i++
            )
            {
                CaveBiomeRuntimeData biome =
                    biomes[i];


                if (
                    biome ==
                    null
                    ||
                    biome.Definition ==
                    null
                    ||
                    !biome.Definition.Enabled
                )
                {
                    continue;
                }


                CaveBiomeDefinition definition =
                    biome.Definition;


                if (
                    !DimensionAllowed(
                        definition,
                        dimensionKeys
                    )
                )
                {
                    continue;
                }


                int minY =
                    Mathf.Min(
                        definition.MinY,
                        definition.MaxY
                    );


                int maxY =
                    Mathf.Max(
                        definition.MinY,
                        definition.MaxY
                    );


                // Organic upper transition.
                if (
                    definition.TransitionHeight >
                    0
                )
                {
                    float noise =
                        Mathf.PerlinNoise(
                            (
                                worldX +
                                HashSeed(
                                    worldSeed,
                                    definition.ID
                                )
                            )
                            *
                            0.018f,

                            37.17f
                        );


                    maxY +=
                        Mathf.RoundToInt(
                            (
                                noise -
                                0.5f
                            )
                            *
                            2f
                            *
                            definition.TransitionHeight
                        );
                }


                if (
                    worldY <
                    minY
                    ||
                    worldY >
                    maxY
                )
                {
                    continue;
                }


                if (
                    definition.Priority >
                    bestPriority
                )
                {
                    best =
                        biome;


                    bestPriority =
                        definition.Priority;
                }
            }


            return best;
        }


        public static string GetBiomeIdAt(
            int worldX,
            int worldY,
            int worldSeed
        )
        {
            CaveBiomeRuntimeData biome =
                FindAt(
                    worldX,
                    worldY,
                    worldSeed,
                    CaveBiomeDimensionRuntime
                        .GetCurrentKeys()
                );


            if (
                biome ==
                null
                ||
                biome.Definition ==
                null
            )
            {
                return null;
            }


            return
                biome.Definition.ID;
        }


        private static void EnsureLoaded()
        {
            if (
                loaded
            )
            {
                return;
            }


            loaded =
                true;


            biomes.Clear();


            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "CaveBiomes"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {
                Directory.CreateDirectory(
                    folder
                );


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
                    string rawJson =
                        File.ReadAllText(
                            files[i]
                        );


                    CaveBiomeDefinition definition =
                        JsonUtility.FromJson<
                            CaveBiomeDefinition
                        >(
                            NormalizeDimensionsJson(
                                rawJson
                            )
                        );


                    if (
                        definition ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            definition.ID
                        )
                    )
                    {
                        continue;
                    }


                    CaveBiomeRuntimeData runtime =
                        new CaveBiomeRuntimeData
                        {
                            Definition =
                                definition,

                            StoneBlockId =
                                ResolveOptionalBlock(
                                    definition.StoneBlockId
                                ),

                            BackgroundBlockId =
                                ResolveOptionalBlock(
                                    definition.BackgroundBlockId
                                ),

                            FloorBlockId =
                                ResolveOptionalBlock(
                                    definition.FloorBlockId
                                ),

                            CeilingBlockId =
                                ResolveOptionalBlock(
                                    definition.CeilingBlockId
                                )
                        };


                    WarnIfBlockMissing(
                        definition.ID,
                        "StoneBlockId",
                        definition.StoneBlockId,
                        runtime.StoneBlockId
                    );


                    WarnIfBlockMissing(
                        definition.ID,
                        "BackgroundBlockId",
                        definition.BackgroundBlockId,
                        runtime.BackgroundBlockId
                    );


                    Debug.Log(
                        "CAVE BIOME LOADED: " +
                        definition.ID +
                        " | Y=" +
                        definition.MinY +
                        ".." +
                        definition.MaxY +
                        " | dimensions=[" +
                        (
                            definition.Dimensions != null
                                ? string.Join(
                                    ", ",
                                    definition.Dimensions.ToArray()
                                )
                                : string.Empty
                        ) +
                        "] | stoneNumeric=" +
                        runtime.StoneBlockId +
                        " | backgroundNumeric=" +
                        runtime.BackgroundBlockId
                    );


                    biomes.Add(
                        runtime
                    );
                }
                catch (
                    Exception exception
                )
                {
                    Debug.LogWarning(
                        "CAVE BIOME: Failed to load " +
                        files[i] +
                        " | " +
                        exception.Message
                    );
                }
            }


            biomes.Sort(
                (
                    a,
                    b
                ) =>
                {
                    int aPriority =
                        a !=
                        null
                        &&
                        a.Definition !=
                        null
                            ? a.Definition.Priority
                            : 0;


                    int bPriority =
                        b !=
                        null
                        &&
                        b.Definition !=
                        null
                            ? b.Definition.Priority
                            : 0;


                    return
                        bPriority.CompareTo(
                            aPriority
                        );
                }
            );
        }


        private static string NormalizeDimensionsJson(
            string json
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    json
                )
            )
            {
                return json;
            }


            // CaveBiomeDefinition.Dimensions is List<string>.
            // Accept both:
            //
            // \"Dimensions\": [\"crystalline\"]
            //
            // and the convenient single-value form:
            //
            // \"Dimensions\": \"crystalline\"
            //
            // The latter must be normalized before JsonUtility.
            Regex regex =
                new Regex(
                    "\"Dimensions\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
                    RegexOptions.IgnoreCase
                );


            Match match =
                regex.Match(
                    json
                );


            if (
                !match.Success
            )
            {
                return json;
            }


            string replacement =
                "\"Dimensions\": [\"" +
                match.Groups[1].Value +
                "\"]";


            return
                regex.Replace(
                    json,
                    replacement,
                    1
                );
        }


        private static bool DimensionAllowed(
            CaveBiomeDefinition definition,
            string[] dimensionKeys
        )
        {
            if (
                definition.Dimensions ==
                null
                ||
                definition.Dimensions.Count ==
                0
            )
            {
                return true;
            }


            for (
                int i = 0;
                i < definition.Dimensions.Count;
                i++
            )
            {
                string required =
                    NormalizeDimensionKey(
                        definition.Dimensions[i]
                    );


                if (
                    string.IsNullOrWhiteSpace(
                        required
                    )
                    ||
                    required ==
                    "*"
                )
                {
                    return true;
                }


                if (
                    dimensionKeys ==
                    null
                )
                {
                    continue;
                }


                for (
                    int keyIndex = 0;
                    keyIndex < dimensionKeys.Length;
                    keyIndex++
                )
                {
                    string current =
                        NormalizeDimensionKey(
                            dimensionKeys[keyIndex]
                        );


                    if (
                        string.Equals(
                            required,
                            current,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        private static string NormalizeDimensionKey(
            string value
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return string.Empty;
            }


            string normalized =
                value.Trim();


            int separator =
                normalized.LastIndexOf(
                    ':'
                );


            if (
                separator >=
                0
                &&
                separator <
                normalized.Length -
                1
            )
            {
                normalized =
                    normalized.Substring(
                        separator + 1
                    );
            }


            return normalized;
        }


        private static void WarnIfBlockMissing(
            string biomeId,
            string fieldName,
            string blockId,
            ushort numericId
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
                ||
                numericId !=
                0
            )
            {
                return;
            }


            Debug.LogWarning(
                "CAVE BIOME: block is not registered. " +
                "Biome=" +
                biomeId +
                " | " +
                fieldName +
                "=" +
                blockId
            );
        }


        private static ushort ResolveOptionalBlock(
            string id
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    id
                )
            )
            {
                return 0;
            }


            try
            {
                ContentID contentId =
                    ContentID.Parse(
                        id
                    );


                if (
                    !BlockIDRegistry.Contains(
                        contentId
                    )
                )
                {
                    return 0;
                }


                return
                    BlockIDRegistry.GetID(
                        contentId
                    );
            }
            catch
            {
                return 0;
            }
        }


        private static float HashSeed(
            int seed,
            string id
        )
        {
            unchecked
            {
                int hash =
                    seed;


                if (
                    id !=
                    null
                )
                {
                    for (
                        int i = 0;
                        i < id.Length;
                        i++
                    )
                    {
                        hash =
                            hash *
                            31 +
                            id[i];
                    }
                }


                uint value =
                    (uint)
                    hash;


                value ^=
                    value >>
                    16;


                value *=
                    0x7FEB352Du;


                value ^=
                    value >>
                    15;


                return
                    value %
                    100000u;
            }
        }
    }
}
