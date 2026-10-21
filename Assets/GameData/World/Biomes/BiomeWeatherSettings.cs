using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeWeatherSettings
    {
        // None / Rain / Snow
        public string Precipitation = "None";

        // Optional fog event chance, even in rainy/snowy biomes.
        public float FogChance = 0.18f;
        public string FogColor = "#AFBDCA";
        public float FogIntensity = 0.65f;

        // Вероятность начала осадков после периода ясной погоды.
        public float Chance = 0.35f;

        // 0..1.5
        public float Intensity = 1f;

        public float MinDuration = 1200f;
        public float MaxDuration = 2400f;

        public float MinClearDuration = 1200f;
        public float MaxClearDuration = 2400f;
    }
}
