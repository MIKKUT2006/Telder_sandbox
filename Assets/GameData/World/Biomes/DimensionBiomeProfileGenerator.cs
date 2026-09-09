using System;
using System.Collections.Generic;
using Game.World.Dimensions;

namespace Game.World.Biomes
{
    public static class DimensionBiomeProfileGenerator
    {
        private sealed class Candidate
        {
            public BiomeDefinition Biome;
            public double Key;
        }


        public static DimensionBiomeProfile Generate(
            DimensionDefinition dimension
        )
        {
            DimensionBiomeProfile profile =
                new DimensionBiomeProfile();

            IReadOnlyList<BiomeDefinition> all =
                BiomeRegistry.GetAll();

            if (
                dimension == null ||
                all == null ||
                all.Count == 0
            )
            {
                return profile;
            }

            string typeID =
                dimension.Type != null
                ? dimension.Type.Id
                : string.Empty;

            List<BiomeDefinition> explicitBiomes =
                new List<BiomeDefinition>();

            List<BiomeDefinition> fallbackBiomes =
                new List<BiomeDefinition>();

            for (
                int i = 0;
                i < all.Count;
                i++
            )
            {
                BiomeDefinition biome =
                    all[i];

                if (biome == null)
                    continue;

                if (
                    biome.IsExplicitForDimensionType(
                        typeID
                    )
                )
                {
                    explicitBiomes.Add(
                        biome
                    );

                    continue;
                }

                if (
                    biome.SupportsDimensionType(
                        typeID
                    )
                )
                {
                    fallbackBiomes.Add(
                        biome
                    );
                }
            }

            List<BiomeDefinition> available =
                explicitBiomes.Count > 0
                ? explicitBiomes
                : fallbackBiomes;

            if (available.Count == 0)
                return profile;

            int desired =
                3 +
                (
                    BiomeHash.Hash(
                        dimension.Seed,
                        0,
                        0x0B10
                    )
                    %
                    3
                );

            desired =
                Math.Min(
                    desired,
                    available.Count
                );

            List<Candidate> candidates =
                new List<Candidate>();

            for (
                int i = 0;
                i < available.Count;
                i++
            )
            {
                BiomeDefinition biome =
                    available[i];

                double u =
                    Math.Max(
                        0.000001,
                        BiomeHash.To01(
                            BiomeHash.HashString(
                                dimension.Seed,
                                biome.ID,
                                0x51A7
                            )
                        )
                    );

                candidates.Add(
                    new Candidate
                    {
                        Biome = biome,

                        Key =
                            -Math.Log(u) /
                            Math.Max(
                                0.001,
                                biome.Weight
                            )
                    }
                );
            }

            candidates.Sort(
                (a, b) =>
                    a.Key.CompareTo(
                        b.Key
                    )
            );

            for (
                int i = 0;
                i < desired;
                i++
            )
            {
                profile.SurfaceBiomes.Add(
                    candidates[i].Biome
                );
            }

            profile.BaseRegionSize =
                460 +
                BiomeHash.Hash(
                    dimension.Seed,
                    1,
                    0x0711
                )
                %
                181;

            profile.RegionJitter =
                85 +
                BiomeHash.Hash(
                    dimension.Seed,
                    2,
                    0x0712
                )
                %
                66;

            profile.BlendWidth =
                52 +
                BiomeHash.Hash(
                    dimension.Seed,
                    3,
                    0x0713
                )
                %
                25;

            profile.BoundaryWarp =
                38f +
                BiomeHash.Hash(
                    dimension.Seed,
                    4,
                    0x0714
                )
                %
                33;

            return profile;
        }
    }
}