using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeTerrainSettings
    {
        public string TopBlock = "game:grass";
        public string SoilBlock = "game:dirt";
        public string StoneBlock = "game:stone";
        public string BackgroundBlock = "game:stone";

        public int SoilDepth = 8;

        public float HeightOffset = 0f;

        public float HillHeightMultiplier = 1f;
        public float HillScaleMultiplier = 1f;

        public float TerrainVariationMultiplier = 1f;
        public float TerrainScaleMultiplier = 1f;

        public float DetailMultiplier = 1f;
        public float DetailScaleMultiplier = 1f;

        public float DistortionStrength = 0f;
        public float DistortionScale = 0.0035f;

        public float RidgeStrength = 0f;
        public float RidgeScale = 0.010f;

        public float WaveStrength = 0f;
        public float WaveScale = 0.024f;
    }
}