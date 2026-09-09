using Game.World.Biomes;
using Game.World.Dimensions;
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

        private readonly CaveQueryAdapter caveQuery;


        private readonly DimensionDefinition dimension;

        private readonly DimensionBiomeProfile biomeProfile;

        private readonly SurfaceBiomeSelector biomeSelector;

        private readonly BiomeRuntimeTable biomeRuntime;


        private readonly float terrainOffset;

        private readonly float hillOffset;

        private readonly float detailOffset;


        /*
         * IMPORTANT:
         *
         * These values are now INTERNAL GENERATOR BASE VALUES.
         *
         * The previous V9 incorrectly expected fields:
         *
         * settings.TerrainScale
         * settings.TerrainDetailScale
         * settings.TerrainVariation
         * settings.TerrainDetail
         *
         * Your current WorldSettings does not expose them,
         * so V9.1 does not reference them at all.
         */
        private const float BaseTerrainScale =
            0.012f;

        private const float BaseTerrainVariation =
            10f;

        private const float BaseDetailScale =
            0.035f;

        private const float BaseDetailStrength =
            3f;


        public WorldGenerator(
            WorldSettings settings
        )
        {
            this.settings =
                settings;


            BiomeRegistry.Initialize();


            dimension =
                DimensionTravelRuntime.Current;


            biomeProfile =
                DimensionBiomeProfileGenerator.Generate(
                    dimension
                );


            if (
                biomeProfile == null ||
                !biomeProfile.IsValid
            )
            {
                Debug.LogError(
                    "WORLD GENERATOR: biome profile is empty. " +
                    "Check Assets/GameData/Biomes."
                );
            }


            biomeSelector =
                new SurfaceBiomeSelector(
                    settings.Seed,
                    biomeProfile
                );


            biomeRuntime =
                new BiomeRuntimeTable(
                    biomeProfile
                );


            caveSettings =
                new CaveSettings();


            caveGenerator =
                new CaveGenerator(
                    settings,
                    caveSettings
                );


            /*
             * No direct call to caveGenerator.IsCave().
             * Therefore this compiles even if your current
             * CaveGenerator renamed that method.
             */
            caveQuery =
                new CaveQueryAdapter(
                    caveGenerator,
                    settings.Seed
                );


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


            PrintBiomeProfile();
        }


        public void ReloadOres()
        {
            oreGenerator.ReloadOres();
        }


        public BiomeSample GetBiomeSample(
            int worldX
        )
        {
            return
                biomeSelector.GetSample(
                    worldX
                );
        }


        public BiomeDefinition GetDominantBiome(
            int worldX
        )
        {
            return
                GetBiomeSample(
                    worldX
                ).Dominant;
        }


        public DimensionBiomeProfile GetBiomeProfile()
        {
            return
                biomeProfile;
        }


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


                BiomeSample sample =
                    biomeSelector.GetSample(
                        worldX
                    );


                int surfaceHeight =
                    GetSurfaceHeight(
                        worldX,
                        sample
                    );


                BiomeRuntimeData biome =
                    biomeRuntime.Get(
                        sample.Dominant
                    );


                if (biome == null)
                {
                    Debug.LogError(
                        "WORLD GENERATOR: runtime biome data is null at X=" +
                        worldX
                    );

                    continue;
                }


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
                            surfaceHeight,
                            biome
                        );


                    ushort background =
                        GenerateBackgroundBlock(
                            worldX,
                            worldY,
                            surfaceHeight,
                            foreground,
                            biome
                        );


                    data.SetBlock(
                        localX,
                        localY,
                        foreground
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


        private ushort GenerateForegroundBlock(
            int worldX,
            int worldY,
            int surfaceHeight,
            BiomeRuntimeData biome
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
                return
                    biome.TopBlockID;
            }


            int depth =
                surfaceHeight -
                worldY;


            if (
                depth <=
                biome.Definition
                    .Terrain
                    .SoilDepth
            )
            {
                return
                    biome.SoilBlockID;
            }


            if (
                caveQuery.IsCave(
                    worldX,
                    worldY,
                    surfaceHeight
                )
            )
            {
                return 0;
            }


            ushort ore =
                oreGenerator.GetOre(
                    worldX,
                    worldY,
                    surfaceHeight
                );


            if (ore != 0)
            {
                return ore;
            }


            return
                biome.StoneBlockID;
        }


        private ushort GenerateBackgroundBlock(
            int worldX,
            int worldY,
            int surfaceHeight,
            ushort foreground,
            BiomeRuntimeData biome
        )
        {
            if (
                worldY >=
                surfaceHeight
            )
            {
                return 0;
            }


            int depth =
                surfaceHeight -
                worldY;


            if (depth <= 2)
            {
                return 0;
            }


            if (
                IsOre(
                    foreground,
                    biome
                )
            )
            {
                return
                    biome.BackgroundBlockID;
            }


            if (
                caveQuery.IsCave(
                    worldX,
                    worldY,
                    surfaceHeight
                )
            )
            {
                return
                    biome.BackgroundBlockID;
            }


            if (
                IsBackgroundOre(
                    worldX,
                    worldY
                )
            )
            {
                ushort backgroundOre =
                    oreGenerator.GetOre(
                        worldX +
                        100000,

                        worldY +
                        100000,

                        surfaceHeight
                    );


                if (backgroundOre != 0)
                {
                    return
                        backgroundOre;
                }
            }


            return
                biome.BackgroundBlockID;
        }


        private bool IsOre(
            ushort blockID,
            BiomeRuntimeData biome
        )
        {
            return
                blockID != 0 &&
                biome != null &&
                !biome.IsTerrainBlock(
                    blockID
                );
        }


        private bool IsBackgroundOre(
            int worldX,
            int worldY
        )
        {
            float noise =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        settings.Seed *
                        0.173f
                    )
                    *
                    0.045f,

                    (
                        worldY +
                        settings.Seed *
                        0.731f
                    )
                    *
                    0.045f
                );


            return
                noise >
                0.72f;
        }


        public int GetSurfaceHeight(
            int worldX
        )
        {
            return
                GetSurfaceHeight(
                    worldX,
                    biomeSelector.GetSample(
                        worldX
                    )
                );
        }


        private int GetSurfaceHeight(
            int worldX,
            BiomeSample sample
        )
        {
            if (sample.Primary == null)
            {
                return
                    ClampSurface(
                        Mathf.RoundToInt(
                            settings.SurfaceHeight
                        )
                    );
            }


            float primary =
                CalculateBiomeSurfaceHeight(
                    worldX,
                    sample.Primary
                );


            if (
                sample.Secondary == null ||
                sample.Secondary ==
                sample.Primary
            )
            {
                return
                    ClampSurface(
                        Mathf.RoundToInt(
                            primary
                        )
                    );
            }


            float secondary =
                CalculateBiomeSurfaceHeight(
                    worldX,
                    sample.Secondary
                );


            return
                ClampSurface(
                    Mathf.RoundToInt(
                        Mathf.Lerp(
                            primary,
                            secondary,
                            sample.Blend
                        )
                    )
                );
        }


        private float CalculateBiomeSurfaceHeight(
            int worldX,
            BiomeDefinition biome
        )
        {
            BiomeTerrainSettings terrain =
                biome.Terrain;


            float distortedX =
                worldX;


            // ---------------------------------------------
            // DOMAIN DISTORTION
            // ---------------------------------------------

            if (
                Mathf.Abs(
                    terrain.DistortionStrength
                )
                >
                0.001f
            )
            {
                float distortion =
                    Mathf.PerlinNoise(
                        (
                            worldX +
                            settings.Seed *
                            0.41731f
                        )
                        *
                        Mathf.Max(
                            0.000001f,
                            terrain.DistortionScale
                        ),

                        43.713f +
                        settings.Seed *
                        0.000031f
                    );


                distortedX +=
                    (
                        distortion -
                        0.5f
                    )
                    *
                    2f *
                    terrain.DistortionStrength;
            }


            // ---------------------------------------------
            // LARGE TERRAIN
            //
            // Uses ONLY fields that exist in the user's
            // current WorldSettings:
            //
            // Seed
            // SurfaceHeight
            // HillScale
            // HillHeight
            // WorldHeight
            // ---------------------------------------------

            float large =
                Mathf.PerlinNoise(
                    (
                        distortedX +
                        terrainOffset
                    )
                    *
                    Mathf.Max(
                        0.000001f,
                        settings.HillScale *
                        terrain.HillScaleMultiplier
                    ),

                    0f
                );


            float middle =
                Mathf.PerlinNoise(
                    (
                        distortedX +
                        hillOffset
                    )
                    *
                    Mathf.Max(
                        0.000001f,
                        BaseTerrainScale *
                        terrain.TerrainScaleMultiplier
                    ),

                    0f
                );


            float detail =
                Mathf.PerlinNoise(
                    (
                        distortedX +
                        detailOffset
                    )
                    *
                    Mathf.Max(
                        0.000001f,
                        BaseDetailScale *
                        terrain.DetailScaleMultiplier
                    ),

                    0f
                );


            float height =
                settings.SurfaceHeight +
                terrain.HeightOffset;


            height +=
                (
                    large -
                    0.5f
                )
                *
                settings.HillHeight *
                terrain.HillHeightMultiplier;


            height +=
                (
                    middle -
                    0.5f
                )
                *
                BaseTerrainVariation *
                terrain.TerrainVariationMultiplier;


            height +=
                (
                    detail -
                    0.5f
                )
                *
                BaseDetailStrength *
                terrain.DetailMultiplier;


            // ---------------------------------------------
            // DISTORTED RIDGES
            // ---------------------------------------------

            if (
                Mathf.Abs(
                    terrain.RidgeStrength
                )
                >
                0.001f
            )
            {
                float ridgeNoise =
                    Mathf.PerlinNoise(
                        (
                            distortedX +
                            settings.Seed *
                            0.8917f
                        )
                        *
                        Mathf.Max(
                            0.000001f,
                            terrain.RidgeScale
                        ),

                        91.37f
                    );


                float ridge =
                    1f -
                    Mathf.Abs(
                        ridgeNoise *
                        2f -
                        1f
                    );


                ridge =
                    ridge *
                    ridge;


                height +=
                    (
                        ridge -
                        0.35f
                    )
                    *
                    terrain.RidgeStrength;
            }


            // ---------------------------------------------
            // DISTORTED WAVE
            // ---------------------------------------------

            if (
                Mathf.Abs(
                    terrain.WaveStrength
                )
                >
                0.001f
            )
            {
                float phase =
                    Mathf.PerlinNoise(
                        (
                            worldX +
                            settings.Seed *
                            0.267f
                        )
                        *
                        0.0041f,

                        157.8f
                    );


                float wave =
                    Mathf.Sin(
                        distortedX *
                        terrain.WaveScale +
                        phase *
                        Mathf.PI *
                        3.5f
                    );


                wave =
                    Mathf.Sign(
                        wave
                    )
                    *
                    Mathf.Pow(
                        Mathf.Abs(
                            wave
                        ),
                        1.65f
                    );


                height +=
                    wave *
                    terrain.WaveStrength;
            }


            return height;
        }


        private int ClampSurface(
            int surface
        )
        {
            return
                Mathf.Clamp(
                    surface,
                    2,
                    settings.WorldHeight -
                    2
                );
        }


        private void PrintBiomeProfile()
        {
            if (
                biomeProfile == null ||
                !biomeProfile.IsValid
            )
            {
                return;
            }


            string list =
                string.Empty;


            for (
                int i = 0;
                i < biomeProfile.SurfaceBiomes.Count;
                i++
            )
            {
                if (i > 0)
                    list += ", ";

                list +=
                    biomeProfile
                        .SurfaceBiomes[i]
                        .DisplayName;
            }


            Debug.Log(
                "WORLD BIOME PROFILE: " +
                dimension.Name +
                " | TYPE: " +
                (
                    dimension.Type != null
                    ? dimension.Type.DisplayName
                    : "UNKNOWN"
                ) +
                " | BIOMES: " +
                list
            );
        }
    }
}