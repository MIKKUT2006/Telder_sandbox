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
            this.worldSettings =
                worldSettings;

            this.settings =
                settings;


            // -------------------------------------------------
            // НЕ используем огромные значения Seed напрямую.
            //
            // Это важно для бесконечного мира:
            // Unity float имеет ограниченную точность.
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
        // PUBLIC
        // =====================================================

        public bool[,] GenerateCaveMask(
            int originX,
            int originY,
            int width,
            int height,
            int[] surfaceHeights
        )
        {
            bool[,] mask =
                new bool[
                    width,
                    height
                ];


            // =================================================
            // NATURAL CAVE FIELD
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


                    // -----------------------------------------
                    // ABOVE SURFACE
                    // -----------------------------------------

                    if (
                        worldY >
                        surface
                    )
                    {
                        continue;
                    }


                    int depth =
                        surface -
                        worldY;


                    // -----------------------------------------
                    // PROTECTION NEAR SURFACE
                    // -----------------------------------------

                    if (
                        depth <
                        settings.StartDepth
                    )
                    {
                        continue;
                    }


                    // -----------------------------------------
                    // DOMAIN WARP
                    // -----------------------------------------

                    float warpX =
                        Mathf.PerlinNoise(
                            (
                                worldX +
                                warpSeedX
                            ) *
                            settings.WarpScale,

                            (
                                worldY +
                                warpSeedY
                            ) *
                            settings.WarpScale
                        );


                    float warpY =
                        Mathf.PerlinNoise(
                            (
                                worldX +
                                warpSeedY +
                                731.17f
                            ) *
                            settings.WarpScale,

                            (
                                worldY +
                                warpSeedX +
                                319.71f
                            ) *
                            settings.WarpScale
                        );


                    float sampleX =
                        worldX +
                        (
                            warpX -
                            0.5f
                        ) *
                        settings.WarpStrength;


                    float sampleY =
                        worldY +
                        (
                            warpY -
                            0.5f
                        ) *
                        settings.WarpStrength;


                    // =================================================
                    // LARGE FIELD
                    // =================================================

                    float large =
                        Mathf.PerlinNoise(
                            (
                                sampleX +
                                largeSeedX
                            ) *
                            settings.LargeScale,

                            (
                                sampleY +
                                largeSeedY
                            ) *
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
                            ) *
                            settings.MediumScale,

                            (
                                sampleY +
                                mediumSeedY
                            ) *
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
                            ) *
                            settings.DetailScale,

                            (
                                sampleY +
                                detailSeedY
                            ) *
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
                        weightSum <= 0.0001f
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
                    // SHAPE THE FIELD
                    // =================================================

                    density =
                        Mathf.SmoothStep(
                            0.18f,
                            0.82f,
                            density
                        );


                    // =================================================
                    // DEPTH
                    // =================================================

                    float depth01 =
                        Mathf.Clamp01(
                            (
                                depth -
                                settings.StartDepth
                            )
                            /
                            Mathf.Max(
                                1f,
                                settings.SurfaceTransition
                            )
                        );


                    float depthMask =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            depth01
                        );


                    // -------------------------------------------------
                    // Не удаляем пещеры ниже определённой высоты.
                    //
                    // После переходной зоны mask = 1 навсегда.
                    // Поэтому мир может продолжаться вниз бесконечно.
                    // -------------------------------------------------

                    density *=
                        Mathf.Lerp(
                            0.05f,
                            1f,
                            depthMask
                        );


                    // =================================================
                    // TUNNEL FIELD
                    // =================================================

                    float tunnelNoise =
                        Mathf.PerlinNoise(
                            (
                                sampleX +
                                tunnelSeedX
                            ) *
                            settings.TunnelScale,

                            (
                                sampleY +
                                tunnelSeedY
                            ) *
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
                        ) *
                        2f;


                    // -------------------------------------------------
                    // Делаем тоннели РЕДКИМИ.
                    //
                    // В старой системе здесь был главный косяк:
                    // низкий threshold превращал почти весь ridge
                    // в тоннель.
                    // -------------------------------------------------

                    float tunnelMask =
                        Mathf.SmoothStep(
                            settings.TunnelThreshold,
                            Mathf.Min(
                                0.99f,
                                settings.TunnelThreshold +
                                0.08f
                            ),
                            tunnel
                        );


                    // =================================================
                    // TUNNEL INFLUENCE
                    // =================================================

                    float tunnelInfluence =
                        tunnelMask *
                        settings.TunnelStrength *
                        depthMask;


                    // -------------------------------------------------
                    // НЕ прибавляем tunnel к density.
                    //
                    // Мы плавно приближаем существующую плотность
                    // к 1.
                    // -------------------------------------------------

                    density =
                        Mathf.Lerp(
                            density,
                            1f,
                            Mathf.Clamp01(
                                tunnelInfluence
                            )
                        );


                    // =================================================
                    // FINAL CLAMP
                    // =================================================

                    density =
                        Mathf.Clamp01(
                            density
                        );


                    // =================================================
                    // CAVE
                    // =================================================

                    if (
                        density >=
                        settings.CaveThreshold
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
                ) - 1;


            int maxRegion =
                FloorDiv(
                    originX +
                    width -
                    1,
                    regionSize
                ) + 1;


            for (
                int regionX = minRegion;
                regionX <= maxRegion;
                regionX++
            )
            {
                // -------------------------------------------------
                // Редкий вход.
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
                    regionSize +

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


                if (
                    localX < 0 ||
                    localX >= width
                )
                {
                    continue;
                }


                int surface =
                    surfaceHeights[localX];


                // -------------------------------------------------
                // Начинаем немного над поверхностью.
                // -------------------------------------------------

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


                    // ---------------------------------------------
                    // Плавное изменение направления.
                    // ---------------------------------------------

                    float wanderNoise =
                        Mathf.PerlinNoise(
                            (
                                regionX *
                                17.17f +
                                i *
                                settings.EntranceWanderScale +
                                entranceSeed
                            ),
                            0.37f
                        );


                    float targetAngle =
                        -90f +
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


                    // ---------------------------------------------
                    // Вход сначала узкий, потом расширяется.
                    // ---------------------------------------------

                    float radius =
                        Mathf.Lerp(
                            settings.EntranceRadius,
                            settings.EntranceRadiusBottom,
                            t
                        );


                    // ---------------------------------------------
                    // Шаг.
                    // ---------------------------------------------

                    Vector2 next =
                        current +
                        directionVector *
                        1.15f;


                    // ---------------------------------------------
                    // Не просто круг.
                    //
                    // StampOrganicBrush создаёт неровную форму
                    // на основе Perlin.
                    // ---------------------------------------------

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
            int minX =
                Mathf.Max(
                    0,
                    Mathf.FloorToInt(
                        center.x -
                        radius -
                        2f
                    ) -
                    originX
                );


            int maxX =
                Mathf.Min(
                    width - 1,
                    Mathf.CeilToInt(
                        center.x +
                        radius +
                        2f
                    ) -
                    originX
                );


            int minY =
                Mathf.Max(
                    0,
                    Mathf.FloorToInt(
                        center.y -
                        radius -
                        2f
                    ) -
                    originY
                );


            int maxY =
                Mathf.Min(
                    height - 1,
                    Mathf.CeilToInt(
                        center.y +
                        radius +
                        2f
                    ) -
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
                        Mathf.Max(
                            0.01f,
                            radius
                        );


                    float dy =
                        (
                            worldY -
                            center.y
                        )
                        /
                        Mathf.Max(
                            0.01f,
                            radius
                        );


                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy
                        );


                    // ---------------------------------------------
                    // Если далеко от центра — сразу пропускаем.
                    // ---------------------------------------------

                    if (
                        distance >
                        1.35f
                    )
                    {
                        continue;
                    }


                    // ---------------------------------------------
                    // Organic deformation.
                    // ---------------------------------------------

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
        // SEED
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
                    (
                        h %
                        100000u
                    );
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