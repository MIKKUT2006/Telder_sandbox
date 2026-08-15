using Game.Content;
using Game.World.Generation.Ores;
using UnityEngine;

namespace Game.World.Generation
{
    public class WorldGenerator
    {
        private readonly WorldSettings settings;

        private readonly CaveSettings caveSettings;

        private readonly OreGenerator oreGenerator;

        private readonly CaveGenerator caveGenerator;


        private readonly float terrainOffset;
        private readonly float hillOffset;
        private readonly float detailOffset;
        private readonly float mountainOffset;
        private readonly float mountainDetailOffset;


        private readonly ushort airID;
        private readonly ushort grassID;
        private readonly ushort dirtID;
        private readonly ushort stoneID;


        public WorldGenerator(
            WorldSettings settings
        )
        {
            this.settings =
                settings;


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
            // ORES
            // =================================================

            oreGenerator =
                new OreGenerator(
                    settings
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


            mountainOffset =
                settings.Seed *
                1.73127f;


            mountainDetailOffset =
                settings.Seed *
                2.91317f;


            // =================================================
            // BLOCK IDS
            // =================================================

            airID =
                GetBlockID(
                    "game:air"
                );


            grassID =
                GetBlockID(
                    "game:grass"
                );


            dirtID =
                GetBlockID(
                    "game:dirt"
                );


            stoneID =
                GetBlockID(
                    "game:stone"
                );


            // =================================================
            // ORES
            // =================================================

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
        // CHUNK
        // =====================================================

        public ChunkData GenerateChunkData(
            int chunkX,
            int chunkY
        )
        {
            ChunkData data =
                new ChunkData();


            int originX =
                chunkX *
                Chunk.SizeX;


            int originY =
                chunkY *
                Chunk.SizeY;


            // =================================================
            // SURFACE
            // =================================================

            int[] surfaces =
                new int[Chunk.SizeX];


            for (
                int x = 0;
                x < Chunk.SizeX;
                x++
            )
            {
                surfaces[x] =
                    GetSurfaceHeight(
                        originX + x
                    );
            }


            // =================================================
            // CAVE MASK
            // =================================================

            bool[,] caveMask =
                caveGenerator.GenerateCaveMask(
                    originX,
                    originY,
                    Chunk.SizeX,
                    Chunk.SizeY,
                    surfaces
                );


            // =================================================
            // BLOCKS
            // =================================================

            for (
                int x = 0;
                x < Chunk.SizeX;
                x++
            )
            {
                int worldX =
                    originX + x;


                int surface =
                    surfaces[x];


                for (
                    int y = 0;
                    y < Chunk.SizeY;
                    y++
                )
                {
                    int worldY =
                        originY + y;


                    bool cave =
                        caveMask[x, y];


                    ushort foreground;


                    // =================================================
                    // AIR ABOVE SURFACE
                    // =================================================

                    if (
                        worldY >
                        surface
                    )
                    {
                        foreground =
                            airID;
                    }


                    // =================================================
                    // CAVE
                    // =================================================

                    else if (cave)
                    {
                        foreground =
                            airID;
                    }


                    // =================================================
                    // GRASS
                    // =================================================

                    else if (
                        worldY ==
                        surface
                    )
                    {
                        foreground =
                            grassID;
                    }


                    // =================================================
                    // UNDERGROUND
                    // =================================================

                    else
                    {
                        int depth =
                            surface -
                            worldY;


                        // ---------------------------------------------
                        // DIRT
                        // ---------------------------------------------

                        if (
                            depth <= 8
                        )
                        {
                            foreground =
                                dirtID;
                        }


                        // ---------------------------------------------
                        // STONE / ORE
                        // ---------------------------------------------

                        else
                        {
                            ushort ore =
                                oreGenerator.GetOre(
                                    worldX,
                                    worldY,
                                    surface
                                );


                            if (
                                ore != 0
                            )
                            {
                                foreground =
                                    ore;
                            }
                            else
                            {
                                foreground =
                                    stoneID;
                            }
                        }
                    }


                    // =================================================
                    // BACKGROUND
                    // =================================================

                    ushort background =
                        GenerateBackground(
                            worldX,
                            worldY,
                            surface,
                            foreground,
                            cave
                        );


                    data.SetBlock(
                        x,
                        y,
                        foreground
                    );


                    data.SetBackground(
                        x,
                        y,
                        background
                    );
                }
            }


            return data;
        }


        // =====================================================
        // BACKGROUND
        // =====================================================

        private ushort GenerateBackground(
            int worldX,
            int worldY,
            int surface,
            ushort foreground,
            bool cave
        )
        {
            // =================================================
            // ABOVE SURFACE
            // =================================================

            if (
                worldY >=
                surface
            )
            {
                return airID;
            }


            // =================================================
            // CAVE
            // =================================================

            if (cave)
            {
                return stoneID;
            }


            // =================================================
            // SHALLOW AREA
            // =================================================

            int depth =
                surface -
                worldY;


            if (
                depth <= 2
            )
            {
                return airID;
            }


            // =================================================
            // ORE BACKGROUND
            // =================================================

            if (
                IsOre(
                    foreground
                )
            )
            {
                return stoneID;
            }


            return stoneID;
        }


        // =====================================================
        // SURFACE
        // =====================================================

        public int GetSurfaceHeight(
            int worldX
        )
        {
            // =================================================
            // LARGE HILLS
            // =================================================

            float large =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        terrainOffset
                    )
                    *
                    settings.HillScale,
                    0f
                );


            // =================================================
            // MEDIUM TERRAIN
            // =================================================

            float medium =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        hillOffset
                    )
                    *
                    settings.TerrainScale,
                    0f
                );


            // =================================================
            // DETAIL
            // =================================================

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


            // =================================================
            // MOUNTAIN
            // =================================================

            float mountain =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        mountainOffset
                    )
                    *
                    settings.MountainScale,
                    0f
                );


            float mountainDetail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        mountainDetailOffset
                    )
                    *
                    settings.MountainDetailScale,
                    0f
                );


            float mountainMask =
                Mathf.InverseLerp(
                    0.47f,
                    0.72f,
                    mountain
                );


            mountainMask =
                Mathf.Pow(
                    mountainMask,
                    settings.MountainPower
                );


            // =================================================
            // HEIGHT
            // =================================================

            float height =
                settings.SurfaceHeight;


            height +=
                (
                    large -
                    0.5f
                )
                *
                settings.HillHeight;


            height +=
                (
                    medium -
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


            float mountainHeight =
                mountainMask *
                (
                    settings.MountainHeight +
                    (
                        mountainDetail -
                        0.5f
                    )
                    *
                    settings.MountainDetailHeight
                );


            height +=
                mountainHeight;


            return
                Mathf.RoundToInt(
                    height
                );
        }


        // =====================================================
        // ORE
        // =====================================================

        private bool IsOre(
            ushort id
        )
        {
            return
                id != airID &&
                id != grassID &&
                id != dirtID &&
                id != stoneID;
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
    }
}