using Game.Content;
using Game.World.Generation.Ores;

using UnityEngine;

namespace Game.World.Generation
{
    public class WorldGenerator
    {

        // =====================================================
        // SETTINGS
        // =====================================================

        private readonly WorldSettings settings;

        private readonly CaveSettings caveSettings;


        // =====================================================
        // GENERATORS
        // =====================================================

        private readonly OreGenerator oreGenerator;

        private readonly CaveGenerator caveGenerator;


        // =====================================================
        // TERRAIN OFFSETS
        // =====================================================

        private readonly float terrainOffset;

        private readonly float hillOffset;

        private readonly float detailOffset;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WorldGenerator(
            WorldSettings settings
        )
        {

            this.settings =
                settings;


            // =================================================
            // ORES
            // =================================================

            oreGenerator =
                new OreGenerator(
                    settings
                );


            // =================================================
            // CAVES
            // =================================================

            caveSettings =
                new CaveSettings();


            caveGenerator =
                new CaveGenerator(
                    settings,
                    caveSettings
                );


            // =================================================
            // TERRAIN SEEDS
            // =================================================

            terrainOffset =
                settings.Seed *
                0.12345f;


            hillOffset =
                settings.Seed *
                0.54321f;


            detailOffset =
                settings.Seed *
                0.98765f;


            // =================================================
            // ORES
            // =================================================

            oreGenerator.ReloadOres();

        }


        // =====================================================
        // RELOAD ORES
        // =====================================================

        public void ReloadOres()
        {

            oreGenerator.ReloadOres();

        }


        // =====================================================
        // GENERATE CHUNK
        // =====================================================

        public ChunkData GenerateChunkData(
            int chunkX,
            int chunkY
        )
        {

            ChunkData data =
                new ChunkData();


            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {

                int worldX =
                    chunkX *
                    Chunk.SizeX +
                    localX;


                int surfaceHeight =
                    GetSurfaceHeight(
                        worldX
                    );


                for (
                    int localY = 0;
                    localY < Chunk.SizeY;
                    localY++
                )
                {

                    int worldY =
                        chunkY *
                        Chunk.SizeY +
                        localY;


                    ushort foreground =
                        GenerateForegroundBlock(
                            worldX,
                            worldY,
                            surfaceHeight
                        );


                    data.SetBlock(
                        localX,
                        localY,
                        foreground
                    );


                    ushort background =
                        GenerateBackgroundBlock(
                            worldX,
                            worldY,
                            surfaceHeight
                        );


                    data.SetBackground(
                        localX,
                        localY,
                        background
                    );

                }

            }


            return data;

        }


        // =====================================================
        // FOREGROUND
        // =====================================================

        private ushort GenerateForegroundBlock(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            // =================================================
            // AIR ABOVE SURFACE
            // =================================================

            if (
                worldY >
                surfaceHeight
            )
            {
                return 0;
            }


            // =================================================
            // GRASS
            // =================================================

            if (
                worldY ==
                surfaceHeight
            )
            {
                return GetBlockID(
                    "game:grass"
                );
            }


            // =================================================
            // DEPTH
            // =================================================

            int depth =
                surfaceHeight -
                worldY;


            // =================================================
            // DIRT
            // =================================================

            const int dirtDepth =
                8;


            if (
                depth <=
                dirtDepth
            )
            {
                return GetBlockID(
                    "game:dirt"
                );
            }


            // =================================================
            // DIRT -> STONE
            // =================================================

            const int transitionDepth =
                6;


            if (
                depth <=
                dirtDepth +
                transitionDepth
            )
            {

                float transitionNoise =
                    Mathf.PerlinNoise(
                        (
                            worldX +
                            terrainOffset *
                            1.73f
                        )
                        *
                        0.18f,

                        (
                            worldY +
                            hillOffset *
                            2.37f
                        )
                        *
                        0.18f
                    );


                int transitionDepthFromSurface =
                    depth -
                    dirtDepth;


                float dirtChance =
                    1f -
                    (
                        (float)
                        transitionDepthFromSurface /
                        transitionDepth
                    );


                dirtChance +=
                    (
                        transitionNoise -
                        0.5f
                    )
                    *
                    0.5f;


                if (
                    transitionNoise <
                    dirtChance
                )
                {
                    return GetBlockID(
                        "game:dirt"
                    );
                }

            }


            // =================================================
            // CAVE
            // =================================================

            if (
                caveGenerator.IsCave(
                    worldX,
                    worldY,
                    surfaceHeight
                )
            )
            {
                return 0;
            }


            // =================================================
            // ORES
            // =================================================

            ushort ore =
                oreGenerator.GetOre(
                    worldX,
                    worldY,
                    surfaceHeight
                );


            if (
                ore != 0
            )
            {
                return ore;
            }


            // =================================================
            // STONE
            // =================================================

            return GetBlockID(
                "game:stone"
            );

        }


        // =====================================================
        // BACKGROUND
        // =====================================================

        private ushort GenerateBackgroundBlock(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {

            if (
                worldY >
                surfaceHeight
            )
            {
                return 0;
            }


            if (
                worldY ==
                surfaceHeight
            )
            {
                return 0;
            }


            int depth =
                surfaceHeight -
                worldY;


            if (
                depth >
                2
            )
            {
                return GetBlockID(
                    "game:stone"
                );
            }


            return 0;

        }


        // =====================================================
        // BLOCK ID
        // =====================================================

        private ushort GetBlockID(
            string blockID
        )
        {

            ContentID id =
                ContentID.Parse(
                    blockID
                );


            if (
                !BlockIDRegistry.Contains(
                    id
                )
            )
            {

                Debug.LogError(
                    "WORLD GENERATOR: BLOCK NOT REGISTERED: " +
                    blockID
                );


                return 0;

            }


            return
                BlockIDRegistry.GetID(
                    id
                );

        }


        // =====================================================
        // SURFACE HEIGHT
        // =====================================================

        public int GetSurfaceHeight(
            int worldX
        )
        {

            float largeTerrain =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        terrainOffset
                    )
                    *
                    settings.HillScale,

                    0f
                );


            float hills =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        hillOffset
                    )
                    *
                    settings.TerrainScale,

                    0f
                );


            float detail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        detailOffset
                    )
                    *
                    settings.TerrainDetailScale,

                    0f
                );


            float height =
                settings.SurfaceHeight;


            height +=
                (
                    largeTerrain -
                    0.5f
                )
                *
                settings.HillHeight;


            height +=
                (
                    hills -
                    0.5f
                )
                *
                settings.TerrainVariation;


            height +=
                (
                    detail -
                    0.5f
                )
                *
                settings.TerrainDetail;


            return
                Mathf.RoundToInt(
                    height
                );

        }

    }
}