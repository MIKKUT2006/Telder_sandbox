using Game.Content;

using Game.World.Generation.Ores;

using UnityEngine;


namespace Game.World.Generation
{

    public class WorldGenerator
    {

        private readonly WorldSettings settings;

        private readonly OreGenerator oreGenerator;


        private readonly float terrainOffset;

        private readonly float hillOffset;

        private readonly float detailOffset;


        public WorldGenerator(
            WorldSettings settings
        )
        {

            this.settings =
                settings;


            oreGenerator =
                new OreGenerator(
                    settings
                );


            terrainOffset =
                settings.Seed *
                0.12345f;


            hillOffset =
                settings.Seed *
                0.54321f;


            detailOffset =
                settings.Seed *
                0.98765f;


            oreGenerator.ReloadOres();

        }


        // =====================================================
        // ORES
        // =====================================================

        public void ReloadOres()
        {

            oreGenerator.ReloadOres();

        }


        // =====================================================
        // GENERATE CHUNK DATA
        // =====================================================

        public ChunkData GenerateChunkData(
            int chunkX,
            int chunkY
        )
        {

            ChunkData data =
                new ChunkData(
                    chunkX,
                    chunkY
                );


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


                    ushort blockID =
                        GenerateBlock(
                            worldX,
                            worldY
                        );


                    data.SetBlock(
                        localX,
                        localY,
                        blockID
                    );

                }

            }


            return data;

        }


        // =====================================================
        // GENERATE BLOCK
        // =====================================================

        private ushort GenerateBlock(
            int worldX,
            int worldY
        )
        {

            int surfaceHeight =
                GetSurfaceHeight(
                    worldX
                );


            // =================================================
            // AIR
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
            // DIRT
            // =================================================

            if (
                worldY >
                surfaceHeight -
                4
            )
            {

                return GetBlockID(
                    "game:dirt"
                );

            }


            // =================================================
            // ORE
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
                    ) *
                    settings.HillScale,
                    0f
                );


            float hills =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        hillOffset
                    ) *
                    settings.TerrainScale,
                    0f
                );


            float detail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        detailOffset
                    ) *
                    settings.TerrainDetailScale,
                    0f
                );


            float height =
                settings.SurfaceHeight;


            height +=
                (
                    largeTerrain -
                    0.5f
                ) *
                settings.HillHeight;


            height +=
                (
                    hills -
                    0.5f
                ) *
                settings.TerrainVariation;


            height +=
                (
                    detail -
                    0.5f
                ) *
                settings.TerrainDetail;


            return
                Mathf.RoundToInt(
                    height
                );

        }

    }

}