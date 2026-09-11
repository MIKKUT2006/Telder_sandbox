
using System;
using System.Collections.Generic;
using System.IO;

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
            string dimensionName
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
                        dimensionName
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
                    "Biomes",
                    "Caves"
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
                    CaveBiomeDefinition definition =
                        JsonUtility.FromJson<
                            CaveBiomeDefinition
                        >(
                            File.ReadAllText(
                                files[i]
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


        private static bool DimensionAllowed(
            CaveBiomeDefinition definition,
            string dimensionName
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
                    definition.Dimensions[i];


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
                    string.Equals(
                        required,
                        dimensionName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }


            return false;
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
