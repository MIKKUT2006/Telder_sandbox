using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeWeatherSettings
    {
        // None / Rain / Snow
        public string Precipitation = "None";

        // Вероятность начала осадков после периода ясной погоды.
        public float Chance = 0.35f;

        // 0..1.5
        public float Intensity = 1f;

        public float MinDuration = 30f;
        public float MaxDuration = 90f;

        public float MinClearDuration = 45f;
        public float MaxClearDuration = 150f;
    }
}
