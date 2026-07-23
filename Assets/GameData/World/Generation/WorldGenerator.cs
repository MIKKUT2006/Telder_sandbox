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
        // CHUNK GENERATION
        // =====================================================

        public void GenerateChunk(
    Chunk chunk
)
        {

            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {

                for (
                    int localY = 0;
                    localY < Chunk.SizeY;
                    localY++
                )
                {

                    int worldX =
                        chunk.X *
                        Chunk.SizeX +
                        localX;


                    int worldY =
                        chunk.Y *
                        Chunk.SizeY +
                        localY;


                    ushort block =
                        GenerateBlock(
                            worldX,
                            worldY
                        );


                    chunk.SetBlock(
                        localX,
                        localY,
                        block
                    );

                }

            }


            oreGenerator.PrintStatistics();

        }


        // =====================================================
        // BLOCK GENERATION
        // =====================================================

        private ushort GenerateBlock(
    int worldX,
    int worldY
)
        {

            // =====================================================
            // œŒÀ”◊¿≈Ã ¬€—Œ“” œŒ¬≈–’ÕŒ—“»
            // =====================================================

            int surfaceHeight =
                GetSurfaceHeight(
                    worldX
                );


            // =====================================================
            // ¬Œ«ƒ”’ Õ¿ƒ œŒ¬≈–’ÕŒ—“‹ﬁ
            // =====================================================

            if (
                worldY >
                surfaceHeight
            )
            {

                return 0;

            }


            // =====================================================
            // “–¿¬¿
            // =====================================================

            if (
                worldY ==
                surfaceHeight
            )
            {

                return GetBlockID(
                    "game:grass"
                );

            }


            // =====================================================
            // «≈ÃÀﬂ
            // =====================================================

            if (
                worldY >
                surfaceHeight - 4
            )
            {

                return GetBlockID(
                    "game:dirt"
                );

            }


            // =====================================================
            // –”ƒ¿
            // =====================================================

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


            // =====================================================
            //  ¿Ã≈Õ‹
            // =====================================================

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
        // TERRAIN HEIGHT
        // =====================================================

        private int GetSurfaceHeight(
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