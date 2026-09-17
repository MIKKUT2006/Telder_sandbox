using System;
using Game.World.Biomes;
namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeDefinition
    {
        public string ID;
        public string DisplayName;

        public string[] AllowedDimensionTypes =
            new string[] { "*" };

        public float Weight = 1f;
        public bool Rare = false;

        public BiomeTerrainSettings Terrain =
            new BiomeTerrainSettings();

        public BiomeCaveSettings Caves =
            new BiomeCaveSettings();

        public BiomeClimateSettings Climate =
            new BiomeClimateSettings();

        public BiomeWeatherSettings Weather =
            new BiomeWeatherSettings();

        public bool SupportsDimensionType(
            string dimensionTypeID
        )
        {
            if (
                AllowedDimensionTypes == null ||
                AllowedDimensionTypes.Length == 0
            )
            {
                return true;
            }

            for (
                int i = 0;
                i < AllowedDimensionTypes.Length;
                i++
            )
            {
                string value =
                    AllowedDimensionTypes[i];

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (value == "*")
                    return true;

                if (
                    string.Equals(
                        value,
                        dimensionTypeID,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }


        public bool IsExplicitForDimensionType(
            string dimensionTypeID
        )
        {
            if (
                AllowedDimensionTypes == null ||
                AllowedDimensionTypes.Length == 0 ||
                string.IsNullOrWhiteSpace(
                    dimensionTypeID
                )
            )
            {
                return false;
            }

            for (
                int i = 0;
                i < AllowedDimensionTypes.Length;
                i++
            )
            {
                string value =
                    AllowedDimensionTypes[i];

                if (
                    string.IsNullOrWhiteSpace(value) ||
                    value == "*"
                )
                {
                    continue;
                }

                if (
                    string.Equals(
                        value,
                        dimensionTypeID,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }
    }
}