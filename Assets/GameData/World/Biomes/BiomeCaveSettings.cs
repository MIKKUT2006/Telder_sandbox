using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeCaveSettings
    {
        public float FrequencyMultiplier = 1f;
        public float SizeMultiplier = 1f;
        public float ChamberMultiplier = 1f;
        public float EntranceMultiplier = 1f;
        public bool EnableSurfaceEntrances = true;
        // Negative uses defaults; zero disables entrances in this biome.
        public float SurfaceEntranceChance = -1f;
        public bool PreferSlopeEntrances = false;
    }
}
