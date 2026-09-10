using System;
using UnityEngine;

namespace Game.World.Generation
{
    public class CaveGenerator
    {
        private readonly WorldSettings worldSettings;
        private readonly CaveSettings settings;


        // =====================================================
        // SEEDS
        // =====================================================

        private readonly float largeSeedX;
        private readonly float largeSeedY;

        private readonly float mediumSeedX;
        private readonly float mediumSeedY;

        private readonly float detailSeedX;
        private readonly float detailSeedY;

        private readonly float warpSeedX;
        private readonly float warpSeedY;

        private readonly float tunnelSeedX;
        private readonly float tunnelSeedY;

        private readonly float entranceSeed;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public CaveGenerator(
            WorldSettings worldSettings,
            CaveSettings settings
        )
        {
            if (worldSettings == null)
            {
                throw new ArgumentNullException(
                    nameof(worldSettings)
                );
            }

            if (settings == null)
            {
                throw new ArgumentNullException(
                    nameof(settings)
                );
            }


            this.worldSettings =
                worldSettings;

            this.settings =
                settings;


            // -------------------------------------------------
            // Не используем огромный Seed напрямую в Perlin.
            //
            // Это особенно важно для бесконечного мира,
            // потому что float постепенно теряет точность.
            // -------------------------------------------------

            largeSeedX =
                HashSeed(
                    worldSettings.Seed,
                    101
                );

            largeSeedY =
                HashSeed(
                    worldSettings.Seed,
                    102
                );


            mediumSeedX =
                HashSeed(
                    worldSettings.Seed,
                    201
                );

            mediumSeedY =
                HashSeed(
                    worldSettings.Seed,
                    202
                );


            detailSeedX =
                HashSeed(
                    worldSettings.Seed,
                    301
                );

            detailSeedY =
                HashSeed(
                    worldSettings.Seed,
                    302
                );


            warpSeedX =
                HashSeed(
                    worldSettings.Seed,
                    401
                );

            warpSeedY =
                HashSeed(
                    worldSettings.Seed,
                    402
                );


            tunnelSeedX =
                HashSeed(
                    worldSettings.Seed,
                    501
                );

            tunnelSeedY =
                HashSeed(
                    worldSettings.Seed,
                    502
                );


            entranceSeed =
                HashSeed(
                    worldSettings.Seed,
                    601
                );
        }


        // =====================================================
        // PUBLIC GENERATION
        // =====================================================

        public bool[,] GenerateCaveMask(
            int originX,
            int originY,
            int width,
            int height,
            int[] surfaceHeights
        )
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width)
                );
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height)
                );
            }

            if (surfaceHeights == null)
            {
                throw new ArgumentNullException(
                    nameof(surfaceHeights)
                );
            }

            if (surfaceHeights.Length < width)
            {
                throw new ArgumentException(
                    "surfaceHeights.Length must be >= width.",
                    nameof(surfaceHeights)
                );
            }


            bool[,] mask =
                new bool[
                    width,
                    height
                ];


            // =================================================
            // NATURAL CAVES
            // =================================================

            for (
                int x = 0;
                x < width;
                x++
            )
            {
                int worldX =
                    originX + x;

                int surface =
                    surfaceHeights[x];


                for (
                    int y = 0;
                    y < height;
                    y++
                )
                {
                    int worldY =
                        originY + y;


                    if (
                        IsNaturalCave(
                            worldX,
                            worldY,
                            surface
                        )
                    )
                    {
                        mask[x, y] =
                            true;
                    }
                }
            }


            // =================================================
            // SURFACE ENTRANCES
            // =================================================

            if (
                settings.EnableSurfaceEntrances
            )
            {
                GenerateSurfaceEntrances(
                    mask,
                    originX,
                    originY,
                    width,
                    height,
                    surfaceHeights
                );
            }


            return mask;
        }
        // =====================================================
        // SINGLE CAVE QUERY
        // =====================================================

        public bool IsCave(
            int worldX,
            int worldY,
            int surfaceHeight
        )
        {
            return IsNaturalCave(
                worldX,
                worldY,
                surfaceHeight
            );
        }

        // =====================================================
        // NATURAL CAVE
        // =====================================================

        private bool IsNaturalCave(
            int worldX,
            int worldY,
            int surface
        )
        {
            // -------------------------------------------------
            // ABOVE SURFACE
            // -------------------------------------------------

            if (
                worldY >
                surface
            )
            {
                return false;
            }


            int depth =
                surface -
                worldY;


            // -------------------------------------------------
            // PROTECTION NEAR SURFACE
            // -------------------------------------------------

            if (
                depth <
                settings.StartDepth
            )
            {
                return false;
            }


            // =================================================
            // DOMAIN WARP
            // =================================================

            float warpX =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        warpSeedX
                    )
                    *
                    settings.WarpScale,

                    (
                        worldY +
                        warpSeedY
                    )
                    *
                    settings.WarpScale
                );


            float warpY =
                Mathf.PerlinNoise(
                    (
                        worldX +
                        warpSeedY +
                        731.17f
                    )
                    *
                    settings.WarpScale,

                    (
                        worldY +
                        warpSeedX +
                        319.71f
                    )
                    *
                    settings.WarpScale
                );


            float sampleX =
                worldX +
                (
                    warpX -
                    0.5f
                )
                *
                settings.WarpStrength;


            float sampleY =
                worldY +
                (
                    warpY -
                    0.5f
                )
                *
                settings.WarpStrength;


            // =================================================
            // LARGE FIELD
            // =================================================

            float large =
                Mathf.PerlinNoise(
                    (
                        sampleX +
                        largeSeedX
                    )
                    *
                    settings.LargeScale,

                    (
                        sampleY +
                        largeSeedY
                    )
                    *
                    settings.LargeScale
                );


            // =================================================
            // MEDIUM FIELD
            // =================================================

            float medium =
                Mathf.PerlinNoise(
                    (
                        sampleX +
                        mediumSeedX
                    )
                    *
                    settings.MediumScale,

                    (
                        sampleY +
                        mediumSeedY
                    )
                    *
                    settings.MediumScale
                );


            // =================================================
            // DETAIL FIELD
            // =================================================

            float detail =
                Mathf.PerlinNoise(
                    (
                        sampleX +
                        detailSeedX
                    )
                    *
                    settings.DetailScale,

                    (
                        sampleY +
                        detailSeedY
                    )
                    *
                    settings.DetailScale
                );


            // =================================================
            // COMBINE
            // =================================================

            float weightSum =
                settings.LargeWeight +
                settings.MediumWeight +
                settings.DetailWeight;


            if (
                weightSum <=
                0.0001f
            )
            {
                weightSum =
                    1f;
            }


            float density =
            (
                large *
                settings.LargeWeight +

                medium *
                settings.MediumWeight +

                detail *
                settings.DetailWeight
            )
            /
            weightSum;


            // =================================================
            // SHAPE
            // =================================================

            density =
                Mathf.SmoothStep(
                    0.18f,
                    0.82f,
                    density
                );


            // =================================================
            // DEPTH TRANSITION
            // =================================================

            float transition =
                Mathf.Max(
                    1f,
                    settings.SurfaceTransition
                );


            float depth01 =
                Mathf.Clamp01(
                    (
                        depth -
                        settings.StartDepth
                    )
                    /
                    transition
                );


            float depthMask =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    depth01
                );


            // -------------------------------------------------
            // В переходной зоне возле поверхности пещер мало.
            //
            // Ниже transition значение становится 1,
            // поэтому генерация может продолжаться вниз
            // без искусственной нижней границы.
            // -------------------------------------------------

            density *=
                Mathf.Lerp(
                    0.05f,
                    1f,
                    depthMask
                );


            // =================================================
            // TUNNELS
            // =================================================

            float tunnelNoise =
                Mathf.PerlinNoise(
                    (
                        sampleX +
                        tunnelSeedX
                    )
                    *
                    settings.TunnelScale,

                    (
                        sampleY +
                        tunnelSeedY
                    )
                    *
                    settings.TunnelScale
                );


            // -------------------------------------------------
            // RIDGED PERLIN
            // -------------------------------------------------

            float tunnel =
                1f -
                Mathf.Abs(
                    tunnelNoise -
                    0.5f
                )
                *
                2f;


            float tunnelThresholdEnd =
                Mathf.Min(
                    0.99f,
                    settings.TunnelThreshold +
                    0.08f
                );


            float tunnelMask =
                Mathf.SmoothStep(
                    settings.TunnelThreshold,
                    tunnelThresholdEnd,
                    tunnel
                );


            float tunnelInfluence =
                tunnelMask *
                settings.TunnelStrength *
                depthMask;


            // -------------------------------------------------
            // Тоннель не просто складывается с density.
            //
            // Он постепенно двигает density к 1,
            // поэтому края получаются органичнее.
            // -------------------------------------------------

            density =
                Mathf.Lerp(
                    density,
                    1f,
                    Mathf.Clamp01(
                        tunnelInfluence
                    )
                );


            density =
                Mathf.Clamp01(
                    density
                );


            // =================================================
            // RESULT
            // =================================================

            return
                density >=
                settings.CaveThreshold;
        }


        // =====================================================
        // SURFACE ENTRANCES
        // =====================================================

        private void GenerateSurfaceEntrances(
            bool[,] mask,
            int originX,
            int originY,
            int width,
            int height,
            int[] surfaceHeights
        )
        {
            int regionSize =
                Mathf.Max(
                    16,
                    settings.EntranceRegionSize
                );


            int minRegion =
                FloorDiv(
                    originX,
                    regionSize
                )
                -
                1;


            int maxRegion =
                FloorDiv(
                    originX +
                    width -
                    1,
                    regionSize
                )
                +
                1;


            for (
                int regionX = minRegion;
                regionX <= maxRegion;
                regionX++
            )
            {
                // -------------------------------------------------
                // DETERMINISTIC CHANCE
                // -------------------------------------------------

                float chance =
                    Hash01(
                        regionX,
                        0,
                        900
                    );


                if (
                    chance >
                    settings.SurfaceEntranceChance
                )
                {
                    continue;
                }


                int entranceX =
                    regionX *
                    regionSize
                    +
                    Mathf.FloorToInt(
                        Hash01(
                            regionX,
                            0,
                            901
                        )
                        *
                        regionSize
                    );


                int localX =
                    entranceX -
                    originX;


                // -------------------------------------------------
                // Центр входа должен находиться в текущем участке.
                // -------------------------------------------------

                if (
                    localX < 0 ||
                    localX >= width
                )
                {
                    continue;
                }


                int surface =
                    surfaceHeights[
                        localX
                    ];


                Vector2 current =
                    new Vector2(
                        entranceX,
                        surface + 1f
                    );


                float direction =
                    -90f;


                int length =
                    Mathf.Max(
                        1,
                        settings.EntranceLength
                    );


                for (
                    int i = 0;
                    i < length;
                    i++
                )
                {
                    float t =
                        i /
                        (float)
                        Mathf.Max(
                            1,
                            length - 1
                        );


                    // =============================================
                    // WANDERING
                    // =============================================

                    float wanderNoise =
                        Mathf.PerlinNoise(
                            regionX *
                            17.17f
                            +
                            i *
                            settings.EntranceWanderScale
                            +
                            entranceSeed,

                            0.37f
                        );


                    float targetAngle =
                        -90f
                        +
                        (
                            wanderNoise -
                            0.5f
                        )
                        *
                        settings.EntranceWander;


                    direction =
                        Mathf.LerpAngle(
                            direction,
                            targetAngle,
                            0.12f
                        );


                    Vector2 directionVector =
                        new Vector2(
                            Mathf.Cos(
                                direction *
                                Mathf.Deg2Rad
                            ),

                            Mathf.Sin(
                                direction *
                                Mathf.Deg2Rad
                            )
                        );


                    // =============================================
                    // RADIUS
                    // =============================================

                    float radius =
                        Mathf.Lerp(
                            settings.EntranceRadius,
                            settings.EntranceRadiusBottom,
                            t
                        );


                    // =============================================
                    // NEXT POINT
                    // =============================================

                    Vector2 next =
                        current
                        +
                        directionVector *
                        1.15f;


                    // =============================================
                    // ORGANIC BRUSH
                    // =============================================

                    StampOrganicBrush(
                        mask,
                        originX,
                        originY,
                        width,
                        height,
                        current,
                        radius,
                        regionX,
                        i
                    );


                    StampOrganicBrush(
                        mask,
                        originX,
                        originY,
                        width,
                        height,
                        next,
                        radius,
                        regionX,
                        i + 1
                    );


                    current =
                        next;
                }
            }
        }


        // =====================================================
        // ORGANIC BRUSH
        // =====================================================

        private void StampOrganicBrush(
            bool[,] mask,
            int originX,
            int originY,
            int width,
            int height,
            Vector2 center,
            float radius,
            int regionX,
            int step
        )
        {
            radius =
                Mathf.Max(
                    0.01f,
                    radius
                );


            int minX =
                Mathf.Max(
                    0,
                    Mathf.FloorToInt(
                        center.x -
                        radius -
                        2f
                    )
                    -
                    originX
                );


            int maxX =
                Mathf.Min(
                    width - 1,
                    Mathf.CeilToInt(
                        center.x +
                        radius +
                        2f
                    )
                    -
                    originX
                );


            int minY =
                Mathf.Max(
                    0,
                    Mathf.FloorToInt(
                        center.y -
                        radius -
                        2f
                    )
                    -
                    originY
                );


            int maxY =
                Mathf.Min(
                    height - 1,
                    Mathf.CeilToInt(
                        center.y +
                        radius +
                        2f
                    )
                    -
                    originY
                );


            for (
                int x = minX;
                x <= maxX;
                x++
            )
            {
                int worldX =
                    originX + x;


                for (
                    int y = minY;
                    y <= maxY;
                    y++
                )
                {
                    int worldY =
                        originY + y;


                    float dx =
                        (
                            worldX -
                            center.x
                        )
                        /
                        radius;


                    float dy =
                        (
                            worldY -
                            center.y
                        )
                        /
                        radius;


                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy
                        );


                    if (
                        distance >
                        1.35f
                    )
                    {
                        continue;
                    }


                    // =============================================
                    // ORGANIC DEFORMATION
                    // =============================================

                    float noise =
                        Mathf.PerlinNoise(
                            (
                                worldX +
                                regionX *
                                83.17f +
                                entranceSeed
                            )
                            *
                            0.11f,

                            (
                                worldY +
                                step *
                                47.31f +
                                entranceSeed
                            )
                            *
                            0.11f
                        );


                    float localRadius =
                        0.82f +
                        noise *
                        0.38f;


                    if (
                        distance <=
                        localRadius
                    )
                    {
                        mask[x, y] =
                            true;
                    }
                }
            }
        }


        // =====================================================
        // SEED HASH
        // =====================================================

        private float HashSeed(
            int seed,
            int salt
        )
        {
            unchecked
            {
                uint h =
                    (uint)seed;


                h ^=
                    (uint)salt *
                    0x9E3779B9u;


                h ^=
                    h >> 16;

                h *=
                    0x85EBCA6Bu;


                h ^=
                    h >> 13;

                h *=
                    0xC2B2AE35u;


                h ^=
                    h >> 16;


                return
                    h %
                    100000u;
            }
        }


        // =====================================================
        // HASH 0..1
        // =====================================================

        private float Hash01(
            int x,
            int y,
            int salt
        )
        {
            unchecked
            {
                uint h =
                    (uint)worldSettings.Seed;


                h ^=
                    (uint)x *
                    374761393u;


                h ^=
                    (uint)y *
                    668265263u;


                h ^=
                    (uint)salt *
                    2246822519u;


                h ^=
                    h >> 13;


                h *=
                    1274126177u;


                h ^=
                    h >> 16;


                return
                    (
                        h &
                        0x00FFFFFFu
                    )
                    /
                    16777215f;
            }
        }


        // =====================================================
        // FLOOR DIVISION
        // =====================================================

        private int FloorDiv(
            int value,
            int divisor
        )
        {
            int result =
                value /
                divisor;


            if (
                value < 0 &&
                value %
                divisor != 0
            )
            {
                result--;
            }


            return result;
        }
    }
}