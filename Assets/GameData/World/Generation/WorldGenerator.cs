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


        // =====================================================
        // TERRAIN OFFSETS
        // =====================================================
        private readonly float soilTransitionOffset;

        private readonly float largeTerrainOffset;

        private readonly float hillOffset;

        private readonly float mountainOffset;

        private readonly float mountainDetailOffset;

        private readonly float surfaceDetailOffset;


        // =====================================================
        // BLOCK IDS
        // =====================================================

        private readonly ushort airID;

        private readonly ushort grassID;

        private readonly ushort dirtID;

        private readonly ushort stoneID;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WorldGenerator(
            WorldSettings settings
        )
        {
            this.settings =
                settings;


            caveSettings =
                new CaveSettings();


            oreGenerator =
                new OreGenerator(
                    settings
                );


            caveGenerator =
                new CaveGenerator(
                    settings,
                    caveSettings
                );


            // =================================================
            // DETERMINISTIC OFFSETS
            // =================================================

            soilTransitionOffset = settings.Seed * 4.73129f;

            largeTerrainOffset =
                settings.Seed *
                0.17321f;


            hillOffset =
                settings.Seed *
                0.73129f;


            mountainOffset =
                settings.Seed *
                1.91371f;


            mountainDetailOffset =
                settings.Seed *
                2.47193f;


            surfaceDetailOffset =
                settings.Seed *
                3.71391f;


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
            // SURFACE HEIGHTS
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
            // BLOCK GENERATION
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
                    // ABOVE SURFACE
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
                    // SURFACE
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


                        int soilDepth =
                            GetSoilDepth(
                                worldX
                            );


                        if (
                            depth <= soilDepth
                        )
                        {
                            foreground =
                                dirtID;
                        }
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
        private int GetSoilDepth(
    int worldX
)
        {
            // =====================================================
            // LARGE SOIL SHAPE
            // =====================================================

            float large =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        soilTransitionOffset
                    )
                    *
                    settings.SoilDepthScale,

                    0f
                );


            // =====================================================
            // DETAIL
            // =====================================================

            float detail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        soilTransitionOffset *
                        2.17f
                    )
                    *
                    settings.SoilDepthDetailScale,

                    0f
                );


            // =====================================================
            // COMBINE
            // =====================================================

            float depth =
                settings.SoilDepthBase;


            depth +=
                (
                    large -
                    0.5f
                )
                *
                2f *
                settings.SoilDepthVariation;


            depth +=
                (
                    detail -
                    0.5f
                )
                *
                2f *
                settings.SoilDepthDetailStrength;


            return Mathf.Clamp(
                Mathf.RoundToInt(
                    depth
                ),
                4,
                16
            );
        }

        // =====================================================
        // SURFACE HEIGHT
        // =====================================================

        public int GetSurfaceHeight(int worldX)
        {
            // Чем меньше значение,
            // тем чаще меняется рельеф.
            const int sampleDistance = 2;

            int leftX =
                Mathf.FloorToInt(
                    worldX / (float)sampleDistance
                ) * sampleDistance;

            int rightX =
                leftX + sampleDistance;


            float t =
                (worldX - leftX) /
                (float)sampleDistance;


            // Плавный переход между точками.
            t =
                t * t *
                (3f - 2f * t);


            float left =
                GetLandscapeSample(
                    leftX
                );

            float right =
                GetLandscapeSample(
                    rightX
                );


            return
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        left,
                        right,
                        t
                    )
                );
        }

        private float GetLandscapeSample(int worldX)
        {
            float seed =
                settings.Seed;


            // =====================================================
            // LARGE TERRAIN
            // =====================================================

            float large =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 0.137f
                    )
                    *
                    0.0028f,

                    0f
                );


            float largeHeight =
                (
                    large -
                    0.5f
                )
                *
                18f;


            // =====================================================
            // MEDIUM HILLS
            // =====================================================

            float hills =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 0.731f
                    )
                    *
                    0.007f,

                    0f
                );


            float hillHeight =
                (
                    hills -
                    0.5f
                )
                *
                16f;


            // =====================================================
            // SMALL HILLS
            // =====================================================

            float smallHills =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 1.913f
                    )
                    *
                    0.018f,

                    0f
                );


            float smallHillHeight =
                (
                    smallHills -
                    0.5f
                )
                *
                7f;


            // =====================================================
            // MOUNTAIN REGION
            // =====================================================

            float mountainRegion =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 2.371f
                    )
                    *
                    0.0028f,

                    0f
                );


            float mountainMask =
                Mathf.SmoothStep(
                    0.58f,
                    0.72f,
                    mountainRegion
                );


            // =====================================================
            // MOUNTAIN SHAPE
            // =====================================================

            float mountain =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 3.193f
                    )
                    *
                    0.0055f,

                    0f
                );


            float mountainShape =
                (
                    mountain -
                    0.5f
                )
                *
                2f;


            // =====================================================
            // MOUNTAIN DETAIL
            // =====================================================

            float mountainDetail =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        seed * 4.731f
                    )
                    *
                    0.014f,

                    0f
                );


            float mountainDetailShape =
                (
                    mountainDetail -
                    0.5f
                )
                *
                2f;


            // =====================================================
            // MOUNTAIN HEIGHT
            // =====================================================

            float mountainHeight =
                28f +
                mountainShape * 25f +
                mountainDetailShape * 5f;


            mountainHeight *=
                mountainMask;


            // =====================================================
            // FINAL TERRAIN
            // =====================================================

            float height =
                settings.SurfaceHeight;


            height +=
                largeHeight;


            height +=
                hillHeight;


            height +=
                smallHillHeight;


            height +=
                mountainHeight;


            return height;
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
            // DEPTH
            // =================================================

            int depth =
                surface -
                worldY;


            // =================================================
            // SHALLOW
            // =================================================

            if (
                depth <= 2
            )
            {
                return airID;
            }


            // =================================================
            // FRONT ORE
            // =================================================

            if (
                IsOre(
                    foreground
                )
            )
            {
                return stoneID;
            }


            // =================================================
            // BACKGROUND ORES
            // =================================================

            ushort backgroundOre =
                oreGenerator.GetOre(
                    worldX + 100000,
                    worldY + 100000,
                    surface
                );


            if (
                backgroundOre != 0
            )
            {
                return backgroundOre;
            }


            return stoneID;
        }


        // =====================================================
        // ORE TEST
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