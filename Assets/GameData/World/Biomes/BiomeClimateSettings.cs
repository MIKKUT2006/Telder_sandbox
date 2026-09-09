using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeClimateSettings
    {
        public float Temperature = 0.5f;
        public float Humidity = 0.5f;
        public float Weirdness = 0.5f;
    }
}