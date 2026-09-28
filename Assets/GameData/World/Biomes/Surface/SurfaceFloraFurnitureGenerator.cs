
using System.Collections.Generic;

using UnityEngine;

using Game.World.Biomes.Generation;
using Game.World.Furniture;
using Game.World.Generation;


namespace Game.World.Biomes.Surface
{
    /// <summary>
    /// Generates deterministic surface plants into the Furniture layer.
    ///
    /// It runs after SaveGameRuntime.ApplyChangesToChunk(), so a player-built
    /// foreground block suppresses a plant instead of being overwritten by it.
    /// </summary>
    public static class SurfaceFloraFurnitureGenerator
    {
        /// <summary>
        /// Writes surface flora into the generation-time furniture buffer.
        /// V4 calls this from WorldGenerator after structures are stamped.
        /// </summary>
        public static void ApplyToChunkData(
            WorldGenerator generator,
            WorldSettings settings,
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (
                generator == null ||
                settings == null ||
                data == null
            )
            {
                return;
            }


            IReadOnlyList<
                SurfaceFloraProfileRuntime
            > profiles =
                SurfaceFloraRegistry.GetAll();


            if (
                profiles == null ||
                profiles.Count == 0
            )
            {
                return;
            }


            int chunkMinX =
                chunkX *
                Chunk.SizeX;


            int chunkMinY =
                chunkY *
                Chunk.SizeY;


            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                int worldX =
                    chunkMinX +
                    localX;


                int surfaceY =
                    generator.GetSurfaceHeight(
                        worldX
                    );


                int plantY =
                    surfaceY +
                    1;


                int localPlantY =
                    plantY -
                    chunkMinY;


                if (
                    localPlantY < 0 ||
                    localPlantY >= Chunk.SizeY
                )
                {
                    continue;
                }


                // Plant cell must still be empty after structures.
                if (
                    data.GetBlock(
                        localX,
                        localPlantY
                    ) != 0
                )
                {
                    continue;
                }


                if (
                    data.GetFurniture(
                        localX,
                        localPlantY
                    ) != 0
                )
                {
                    continue;
                }


                // Usually the ground is in this chunk. If plantY is exactly
                // the bottom cell, the surface block belongs to the chunk
                // directly below; generator surface height is enough to
                // accept that boundary case.
                int localSurfaceY =
                    surfaceY -
                    chunkMinY;


                if (
                    localSurfaceY >= 0 &&
                    localSurfaceY < Chunk.SizeY &&
                    data.GetBlock(
                        localX,
                        localSurfaceY
                    ) == 0
                )
                {
                    continue;
                }


                if (
                    localSurfaceY < -1 ||
                    localSurfaceY >= Chunk.SizeY
                )
                {
                    continue;
                }


                BiomeDefinition biome =
                    generator.GetDominantBiome(
                        worldX
                    );


                if (
                    biome == null
                )
                {
                    continue;
                }


                bool placed =
                    false;


                for (
                    int profileIndex = 0;
                    profileIndex < profiles.Count;
                    profileIndex++
                )
                {
                    SurfaceFloraProfileRuntime profile =
                        profiles[
                            profileIndex
                        ];


                    if (
                        profile == null ||
                        profile.Definition == null ||
                        !BiomeNameUtility.Matches(
                            biome,
                            profile.Definition.Biomes
                        )
                    )
                    {
                        continue;
                    }


                    for (
                        int plantIndex = 0;
                        plantIndex < profile.Plants.Count;
                        plantIndex++
                    )
                    {
                        SurfaceFloraEntryRuntime plant =
                            profile.Plants[
                                plantIndex
                            ];


                        if (
                            plant == null ||
                            plant.Definition == null ||
                            plant.BlockId == 0
                        )
                        {
                            continue;
                        }


                        SurfaceFloraEntry definition =
                            plant.Definition;


                        if (
                            definition.NoiseThreshold > 0f
                        )
                        {
                            float scale =
                                Mathf.Max(
                                    0.00001f,
                                    definition.NoiseScale
                                );


                            float patchNoise =
                                Mathf.PerlinNoise(
                                    (
                                        worldX +
                                        settings.Seed *
                                        0.173f +
                                        profileIndex *
                                        131.7f
                                    )
                                    *
                                    scale,

                                    67.31f +
                                    plantIndex *
                                    11.9f
                                );


                            if (
                                patchNoise <
                                Mathf.Clamp01(
                                    definition.NoiseThreshold
                                )
                            )
                            {
                                continue;
                            }
                        }


                        float roll =
                            Hash01(
                                settings.Seed,
                                worldX,
                                profileIndex,
                                plantIndex
                            );


                        if (
                            roll >
                            Mathf.Clamp01(
                                definition.Chance
                            )
                        )
                        {
                            continue;
                        }


                        if (
                            data.SetFurniture(
                                localX,
                                localPlantY,
                                plant.BlockId
                            )
                        )
                        {
                            placed =
                                true;


                            break;
                        }
                    }


                    if (placed)
                    {
                        break;
                    }
                }
            }
        }


        public static void ApplyToLoadedChunk(
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


            FurnitureLayerManager furniture =
                FurnitureLayerManager.EnsureInstance();


            if (
                furniture ==
                null
            )
            {
                return;
            }


            IReadOnlyList<
                SurfaceFloraProfileRuntime
            > profiles =
                SurfaceFloraRegistry.GetAll();


            if (
                profiles ==
                null
                ||
                profiles.Count ==
                0
            )
            {
                return;
            }


            World world =
                WorldManager.Instance !=
                null
                    ? WorldManager.Instance.GetWorld()
                    : null;


            int chunkMinX =
                chunk.X *
                Chunk.SizeX;


            int chunkMinY =
                chunk.Y *
                Chunk.SizeY;


            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                int worldX =
                    chunkMinX +
                    localX;


                int surfaceY =
                    generator.GetSurfaceHeight(
                        worldX
                    );


                int plantY =
                    surfaceY +
                    1;


                int localPlantY =
                    plantY -
                    chunkMinY;


                // The furniture belongs to the chunk containing its own cell.
                if (
                    localPlantY <
                    0
                    ||
                    localPlantY >=
                    Chunk.SizeY
                )
                {
                    continue;
                }


                // Respect saved/player changes.
                if (
                    world !=
                    null
                )
                {
                    if (
                        world.GetBlock(
                            worldX,
                            plantY
                        ) !=
                        0
                        ||
                        world.GetBlock(
                            worldX,
                            surfaceY
                        ) ==
                        0
                    )
                    {
                        continue;
                    }
                }
                else if (
                    chunk.GetBlock(
                        localX,
                        localPlantY
                    ) !=
                    0
                )
                {
                    continue;
                }


                if (
                    furniture.HasFurniture(
                        worldX,
                        plantY
                    )
                )
                {
                    continue;
                }


                BiomeDefinition biome =
                    generator.GetDominantBiome(
                        worldX
                    );


                if (
                    biome ==
                    null
                )
                {
                    continue;
                }


                bool placed =
                    false;


                for (
                    int profileIndex = 0;
                    profileIndex < profiles.Count;
                    profileIndex++
                )
                {
                    SurfaceFloraProfileRuntime profile =
                        profiles[
                            profileIndex
                        ];


                    if (
                        profile ==
                        null
                        ||
                        profile.Definition ==
                        null
                        ||
                        !BiomeNameUtility.Matches(
                            biome,
                            profile.Definition.Biomes
                        )
                    )
                    {
                        continue;
                    }


                    for (
                        int plantIndex = 0;
                        plantIndex < profile.Plants.Count;
                        plantIndex++
                    )
                    {
                        SurfaceFloraEntryRuntime plant =
                            profile.Plants[
                                plantIndex
                            ];


                        if (
                            plant ==
                            null
                            ||
                            plant.Definition ==
                            null
                            ||
                            plant.BlockId ==
                            0
                        )
                        {
                            continue;
                        }


                        SurfaceFloraEntry definition =
                            plant.Definition;


                        if (
                            definition.NoiseThreshold >
                            0f
                        )
                        {
                            float scale =
                                Mathf.Max(
                                    0.00001f,
                                    definition.NoiseScale
                                );


                            float patchNoise =
                                Mathf.PerlinNoise(
                                    (
                                        worldX +
                                        settings.Seed *
                                        0.173f +
                                        profileIndex *
                                        131.7f
                                    )
                                    *
                                    scale,

                                    67.31f +
                                    plantIndex *
                                    11.9f
                                );


                            if (
                                patchNoise <
                                Mathf.Clamp01(
                                    definition.NoiseThreshold
                                )
                            )
                            {
                                continue;
                            }
                        }


                        float roll =
                            Hash01(
                                settings.Seed,
                                worldX,
                                profileIndex,
                                plantIndex
                            );


                        if (
                            roll >
                            Mathf.Clamp01(
                                definition.Chance
                            )
                        )
                        {
                            continue;
                        }


                        if (
                            furniture.SetGeneratedFurniture(
                                worldX,
                                plantY,
                                definition.BlockId
                            )
                        )
                        {
                            placed =
                                true;


                            break;
                        }
                    }


                    if (
                        placed
                    )
                    {
                        break;
                    }
                }
            }
        }


        private static float Hash01(
            int seed,
            int worldX,
            int profileIndex,
            int plantIndex
        )
        {
            unchecked
            {
                uint h =
                    (uint)seed;


                h ^=
                    (uint)worldX *
                    374761393u;


                h ^=
                    (uint)profileIndex *
                    668265263u;


                h ^=
                    (uint)plantIndex *
                    2246822519u;


                h ^=
                    h >>
                    13;


                h *=
                    1274126177u;


                h ^=
                    h >>
                    16;


                return
                    (
                        h &
                        0x00FFFFFFu
                    )
                    /
                    16777215f;
            }
        }
    }
}
