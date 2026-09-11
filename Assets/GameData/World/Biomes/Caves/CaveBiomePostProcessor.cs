
using UnityEngine;

using Game.World.Biomes.Generation;
using Game.World.Dimensions;
using Game.World.Generation;


namespace Game.World.Biomes.Caves
{
    public static class CaveBiomePostProcessor
    {
        public static void ApplyToChunk(
            WorldGenerator generator,
            WorldSettings settings,
            Chunk chunk
        )
        {
            if (
                generator ==
                null
                ||
                settings ==
                null
                ||
                chunk ==
                null
            )
            {
                return;
            }


            string dimensionName =
                DimensionTravelRuntime.Current !=
                null
                    ? DimensionTravelRuntime.Current.Name
                    : string.Empty;


            int chunkMinX =
                chunk.X *
                Chunk.SizeX;


            int chunkMinY =
                chunk.Y *
                Chunk.SizeY;


            // =================================================
            // PASS 1:
            // material overrides + biome-specific extra caves
            // =================================================

            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                int worldX =
                    chunkMinX +
                    localX;


                if (
                    !WorldGenerationBiomeProbe
                        .TryGetTerrainIds(
                            generator,
                            worldX,
                            out ushort baseStone,
                            out ushort baseBackground
                        )
                )
                {
                    continue;
                }


                for (
                    int localY = 0;
                    localY < Chunk.SizeY;
                    localY++
                )
                {
                    int worldY =
                        chunkMinY +
                        localY;


                    CaveBiomeRuntimeData biome =
                        CaveBiomeRegistry.FindAt(
                            worldX,
                            worldY,
                            settings.Seed,
                            dimensionName
                        );


                    if (
                        biome ==
                        null
                    )
                    {
                        continue;
                    }


                    ushort foreground =
                        chunk.GetBlock(
                            localX,
                            localY
                        );


                    ushort background =
                        chunk.GetBackground(
                            localX,
                            localY
                        );


                    if (
                        foreground !=
                        0
                        &&
                        foreground ==
                        baseStone
                    )
                    {
                        if (
                            biome.Definition.ExtraCaves
                            &&
                            IsExtraCave(
                                biome.Definition,
                                worldX,
                                worldY,
                                settings.Seed
                            )
                        )
                        {
                            chunk.SetBlock(
                                localX,
                                localY,
                                0
                            );
                        }
                        else if (
                            biome.StoneBlockId !=
                            0
                        )
                        {
                            chunk.SetBlock(
                                localX,
                                localY,
                                biome.StoneBlockId
                            );
                        }
                    }


                    // Do not replace background ores. Only the
                    // normal background material is transformed.
                    if (
                        background !=
                        0
                        &&
                        background ==
                        baseBackground
                        &&
                        biome.BackgroundBlockId !=
                        0
                    )
                    {
                        chunk.SetBackground(
                            localX,
                            localY,
                            biome.BackgroundBlockId
                        );
                    }
                }
            }


            // =================================================
            // PASS 2:
            // optional cave floor / ceiling materials
            // =================================================

            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                int worldX =
                    chunkMinX +
                    localX;


                for (
                    int localY = 1;
                    localY < Chunk.SizeY - 1;
                    localY++
                )
                {
                    int worldY =
                        chunkMinY +
                        localY;


                    CaveBiomeRuntimeData biome =
                        CaveBiomeRegistry.FindAt(
                            worldX,
                            worldY,
                            settings.Seed,
                            dimensionName
                        );


                    if (
                        biome ==
                        null
                    )
                    {
                        continue;
                    }


                    ushort current =
                        chunk.GetBlock(
                            localX,
                            localY
                        );


                    if (
                        current ==
                        0
                    )
                    {
                        continue;
                    }


                    bool airAbove =
                        chunk.GetBlock(
                            localX,
                            localY +
                            1
                        ) ==
                        0;


                    bool airBelow =
                        chunk.GetBlock(
                            localX,
                            localY -
                            1
                        ) ==
                        0;


                    if (
                        airAbove
                        &&
                        biome.FloorBlockId !=
                        0
                    )
                    {
                        chunk.SetBlock(
                            localX,
                            localY,
                            biome.FloorBlockId
                        );


                        continue;
                    }


                    if (
                        airBelow
                        &&
                        biome.CeilingBlockId !=
                        0
                    )
                    {
                        chunk.SetBlock(
                            localX,
                            localY,
                            biome.CeilingBlockId
                        );
                    }
                }
            }
        }


        private static bool IsExtraCave(
            CaveBiomeDefinition definition,
            int worldX,
            int worldY,
            int worldSeed
        )
        {
            float scale =
                Mathf.Max(
                    0.00001f,
                    definition.ExtraCaveScale
                );


            float detailScale =
                Mathf.Max(
                    0.00001f,
                    definition.ExtraCaveDetailScale
                );


            float seedX =
                HashSeed(
                    worldSeed,
                    7101
                );


            float seedY =
                HashSeed(
                    worldSeed,
                    7102
                );


            float large =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seedX
                    )
                    *
                    scale,

                    (
                        worldY +
                        seedY
                    )
                    *
                    scale
                );


            float detail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seedY +
                        191.7f
                    )
                    *
                    detailScale,

                    (
                        worldY +
                        seedX +
                        517.3f
                    )
                    *
                    detailScale
                );


            float field =
                large *
                0.78f +
                detail *
                0.22f;


            return
                field >=
                Mathf.Clamp01(
                    definition.ExtraCaveThreshold
                );
        }


        private static float HashSeed(
            int seed,
            int salt
        )
        {
            unchecked
            {
                uint h =
                    (uint)
                    seed;


                h ^=
                    (uint)
                    salt *
                    0x9E3779B9u;


                h ^=
                    h >>
                    16;


                h *=
                    0x85EBCA6Bu;


                h ^=
                    h >>
                    13;


                return
                    h %
                    100000u;
            }
        }
    }
}
