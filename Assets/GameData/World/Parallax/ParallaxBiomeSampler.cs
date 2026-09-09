using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Game.Content;
using Game.World.Generation;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class ParallaxBiomeSampler
    {
        public sealed class Palette
        {
            public ushort Top;
            public ushort Soil;
            public ushort Stone;

            public int SoilDepth;

            public float HeightOffset;

            public float HillHeightMultiplier;
            public float TerrainVariationMultiplier;

            public float DistortionStrength;
            public float DistortionScale;

            public float RidgeStrength;
            public float RidgeScale;

            public float WaveStrength;
            public float WaveScale;
        }


        private readonly WorldGenerator generator;

        private readonly MethodInfo getDominantBiomeMethod;

        private readonly Dictionary<string, Palette>
            cached =
            new Dictionary<string, Palette>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly Palette fallback;


        public ParallaxBiomeSampler(
            WorldGenerator generator
        )
        {
            this.generator = generator;

            if (generator != null)
            {
                getDominantBiomeMethod =
                    generator.GetType().GetMethod(
                        "GetDominantBiome",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new Type[]
                        {
                            typeof(int)
                        },
                        null
                    );
            }

            fallback =
                CreateFallback();
        }


        public Palette GetPalette(
            int worldX
        )
        {
            if (
                generator == null ||
                getDominantBiomeMethod == null
            )
            {
                return fallback;
            }

            object biome;

            try
            {
                biome =
                    getDominantBiomeMethod.Invoke(
                        generator,
                        new object[]
                        {
                            worldX
                        }
                    );
            }
            catch
            {
                return fallback;
            }

            if (biome == null)
            {
                return fallback;
            }

            string id =
                ReadString(
                    biome,
                    "ID",
                    biome.GetType().FullName
                );

            if (
                !string.IsNullOrEmpty(id) &&
                cached.TryGetValue(
                    id,
                    out Palette existing
                )
            )
            {
                return existing;
            }

            Palette palette =
                BuildPalette(
                    biome
                );

            if (!string.IsNullOrEmpty(id))
            {
                cached[id] =
                    palette;
            }

            return palette;
        }


        private Palette BuildPalette(
            object biome
        )
        {
            object terrain =
                ReadObject(
                    biome,
                    "Terrain"
                );

            if (terrain == null)
            {
                return fallback;
            }

            Palette palette =
                new Palette();

            palette.Top =
                ResolveBlock(
                    ReadString(
                        terrain,
                        "TopBlock",
                        GetDefaultTop()
                    ),
                    GetDefaultTop()
                );

            palette.Soil =
                ResolveBlock(
                    ReadString(
                        terrain,
                        "SoilBlock",
                        GetDefaultSoil()
                    ),
                    GetDefaultSoil()
                );

            palette.Stone =
                ResolveBlock(
                    ReadString(
                        terrain,
                        "StoneBlock",
                        GetDefaultStone()
                    ),
                    GetDefaultStone()
                );

            palette.SoilDepth =
                Mathf.Max(
                    1,
                    ReadInt(
                        terrain,
                        "SoilDepth",
                        7
                    )
                );

            palette.HeightOffset =
                ReadFloat(
                    terrain,
                    "HeightOffset",
                    0f
                );

            palette.HillHeightMultiplier =
                ReadFloat(
                    terrain,
                    "HillHeightMultiplier",
                    1f
                );

            palette.TerrainVariationMultiplier =
                ReadFloat(
                    terrain,
                    "TerrainVariationMultiplier",
                    1f
                );

            palette.DistortionStrength =
                ReadFloat(
                    terrain,
                    "DistortionStrength",
                    0f
                );

            palette.DistortionScale =
                ReadFloat(
                    terrain,
                    "DistortionScale",
                    0.0035f
                );

            palette.RidgeStrength =
                ReadFloat(
                    terrain,
                    "RidgeStrength",
                    0f
                );

            palette.RidgeScale =
                ReadFloat(
                    terrain,
                    "RidgeScale",
                    0.01f
                );

            palette.WaveStrength =
                ReadFloat(
                    terrain,
                    "WaveStrength",
                    0f
                );

            palette.WaveScale =
                ReadFloat(
                    terrain,
                    "WaveScale",
                    0.024f
                );

            return palette;
        }


        private Palette CreateFallback()
        {
            Palette result =
                new Palette();

            result.Top =
                ResolveBlock(
                    GetDefaultTop(),
                    "game:grass"
                );

            result.Soil =
                ResolveBlock(
                    GetDefaultSoil(),
                    "game:dirt"
                );

            result.Stone =
                ResolveBlock(
                    GetDefaultStone(),
                    "game:stone"
                );

            result.SoilDepth = 7;

            result.HeightOffset = 0f;

            result.HillHeightMultiplier = 1f;
            result.TerrainVariationMultiplier = 1f;

            result.DistortionStrength = 0f;
            result.DistortionScale = 0.0035f;

            result.RidgeStrength = 0f;
            result.RidgeScale = 0.01f;

            result.WaveStrength = 0f;
            result.WaveScale = 0.024f;

            if (
                IsDimensionType(
                    "distorted"
                )
            )
            {
                result.DistortionStrength = 72f;
                result.DistortionScale = 0.003f;

                result.RidgeStrength = 18f;
                result.RidgeScale = 0.009f;

                result.WaveStrength = 8f;
                result.WaveScale = 0.026f;
            }

            return result;
        }


        private string GetDefaultTop()
        {
            return
                IsDimensionType(
                    "distorted"
                )
                ? "game:blue_grass"
                : "game:grass";
        }


        private string GetDefaultSoil()
        {
            return
                IsDimensionType(
                    "distorted"
                )
                ? "game:blue_stone"
                : "game:dirt";
        }


        private string GetDefaultStone()
        {
            return
                IsDimensionType(
                    "distorted"
                )
                ? "game:blue_stone"
                : "game:stone";
        }


        private bool IsDimensionType(
            string id
        )
        {
            try
            {
                DimensionDefinition current =
                    DimensionTravelRuntime.Current;

                return
                    current != null &&
                    current.Type != null &&
                    string.Equals(
                        current.Type.Id,
                        id,
                        StringComparison.OrdinalIgnoreCase
                    );
            }
            catch
            {
                return false;
            }
        }


        private ushort ResolveBlock(
            string requested,
            string fallback
        )
        {
            ushort id =
                TryResolve(
                    requested
                );

            if (id != 0)
                return id;

            return
                TryResolve(
                    fallback
                );
        }


        private ushort TryResolve(
            string value
        )
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            try
            {
                ContentID id =
                    ContentID.Parse(
                        value
                    );

                if (
                    !BlockIDRegistry.Contains(
                        id
                    )
                )
                {
                    return 0;
                }

                return
                    BlockIDRegistry.GetID(
                        id
                    );
            }
            catch
            {
                return 0;
            }
        }


        private object ReadObject(
            object source,
            string name
        )
        {
            if (source == null)
                return null;

            Type type =
                source.GetType();

            FieldInfo field =
                type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            if (field != null)
            {
                return
                    field.GetValue(
                        source
                    );
            }

            PropertyInfo property =
                type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            if (
                property != null &&
                property.CanRead
            )
            {
                return
                    property.GetValue(
                        source,
                        null
                    );
            }

            return null;
        }


        private string ReadString(
            object source,
            string name,
            string fallbackValue
        )
        {
            object value =
                ReadObject(
                    source,
                    name
                );

            return
                value != null
                ? value.ToString()
                : fallbackValue;
        }


        private float ReadFloat(
            object source,
            string name,
            float fallbackValue
        )
        {
            object value =
                ReadObject(
                    source,
                    name
                );

            if (value == null)
                return fallbackValue;

            try
            {
                return
                    Convert.ToSingle(
                        value
                    );
            }
            catch
            {
                return fallbackValue;
            }
        }


        private int ReadInt(
            object source,
            string name,
            int fallbackValue
        )
        {
            object value =
                ReadObject(
                    source,
                    name
                );

            if (value == null)
                return fallbackValue;

            try
            {
                return
                    Convert.ToInt32(
                        value
                    );
            }
            catch
            {
                return fallbackValue;
            }
        }
    }
}