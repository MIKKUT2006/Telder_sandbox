using UnityEngine;

namespace Game.World.Biomes
{
    public sealed class SurfaceBiomeSelector
    {
        private readonly int seed;
        private readonly DimensionBiomeProfile profile;
        private readonly float warpOffset;


        public SurfaceBiomeSelector(
            int seed,
            DimensionBiomeProfile profile
        )
        {
            this.seed = seed;
            this.profile = profile;

            warpOffset =
                (
                    seed &
                    0x7FFFFFFF
                )
                *
                0.000173f;
        }


        public BiomeSample GetSample(
            int worldX
        )
        {
            if (
                profile == null ||
                profile.SurfaceBiomes.Count == 0
            )
            {
                return
                    new BiomeSample(
                        null,
                        null,
                        0f
                    );
            }

            if (
                profile.SurfaceBiomes.Count == 1
            )
            {
                BiomeDefinition only =
                    profile.SurfaceBiomes[0];

                return
                    new BiomeSample(
                        only,
                        only,
                        0f
                    );
            }

            float warpedX =
                WarpX(
                    worldX
                );

            int region =
                FindRegion(
                    warpedX
                );

            float leftBoundary =
                GetBoundary(
                    region
                );

            float rightBoundary =
                GetBoundary(
                    region + 1
                );

            float blendWidth =
                profile.BlendWidth;

            if (
                warpedX >=
                rightBoundary -
                blendWidth
            )
            {
                BiomeDefinition left =
                    GetBiomeForRegion(
                        region
                    );

                BiomeDefinition right =
                    GetBiomeForRegion(
                        region + 1
                    );

                float t =
                    Mathf.InverseLerp(
                        rightBoundary -
                        blendWidth,

                        rightBoundary +
                        blendWidth,

                        warpedX
                    );

                return
                    new BiomeSample(
                        left,
                        right,
                        Smooth01(t)
                    );
            }

            if (
                warpedX <=
                leftBoundary +
                blendWidth
            )
            {
                BiomeDefinition left =
                    GetBiomeForRegion(
                        region - 1
                    );

                BiomeDefinition right =
                    GetBiomeForRegion(
                        region
                    );

                float t =
                    Mathf.InverseLerp(
                        leftBoundary -
                        blendWidth,

                        leftBoundary +
                        blendWidth,

                        warpedX
                    );

                return
                    new BiomeSample(
                        left,
                        right,
                        Smooth01(t)
                    );
            }

            BiomeDefinition biome =
                GetBiomeForRegion(
                    region
                );

            return
                new BiomeSample(
                    biome,
                    biome,
                    0f
                );
        }


        private float WarpX(
            int worldX
        )
        {
            float noise =
                Mathf.PerlinNoise(
                    worldX *
                    0.00175f +
                    warpOffset,

                    17.123f +
                    warpOffset *
                    0.37f
                );

            return
                worldX +
                (
                    noise -
                    0.5f
                )
                *
                2f *
                profile.BoundaryWarp;
        }


        private int FindRegion(
            float x
        )
        {
            int region =
                Mathf.FloorToInt(
                    x /
                    profile.BaseRegionSize
                );

            while (
                x <
                GetBoundary(
                    region
                )
            )
            {
                region--;
            }

            while (
                x >=
                GetBoundary(
                    region + 1
                )
            )
            {
                region++;
            }

            return region;
        }


        private float GetBoundary(
            int index
        )
        {
            float random =
                BiomeHash.To01(
                    BiomeHash.Hash(
                        seed,
                        index,
                        0xB0A1
                    )
                );

            float jitter =
                (
                    random *
                    2f -
                    1f
                )
                *
                profile.RegionJitter;

            return
                index *
                profile.BaseRegionSize +
                jitter;
        }


        private BiomeDefinition GetBiomeForRegion(
            int region
        )
        {
            int count =
                profile.SurfaceBiomes.Count;

            if (count == 1)
                return profile.SurfaceBiomes[0];

            int index =
                BiomeHash.Hash(
                    seed,
                    region,
                    0xB10E
                )
                %
                count;

            int previous =
                BiomeHash.Hash(
                    seed,
                    region - 1,
                    0xB10E
                )
                %
                count;

            if (index == previous)
            {
                int shift =
                    1 +
                    BiomeHash.Hash(
                        seed,
                        region,
                        0x5A17
                    )
                    %
                    (
                        count -
                        1
                    );

                index =
                    (
                        index +
                        shift
                    )
                    %
                    count;
            }

            return
                profile.SurfaceBiomes[
                    index
                ];
        }


        private static float Smooth01(
            float value
        )
        {
            value =
                Mathf.Clamp01(
                    value
                );

            return
                value *
                value *
                (
                    3f -
                    2f *
                    value
                );
        }
    }
}