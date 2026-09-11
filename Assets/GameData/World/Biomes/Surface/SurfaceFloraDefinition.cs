
using System;
using System.Collections.Generic;


namespace Game.World.Biomes.Surface
{
    [Serializable]
    public class SurfaceFloraProfile
    {
        public string ID =
            "surface_flora";


        public bool Enabled =
            true;


        // Empty = all surface biomes.
        //
        // Entries can match biome ID, Name or DisplayName.
        public List<string> Biomes =
            new List<string>();


        public List<SurfaceFloraEntry> Plants =
            new List<SurfaceFloraEntry>();
    }


    [Serializable]
    public class SurfaceFloraEntry
    {
        public string BlockId;


        // Final deterministic chance for a column.
        public float Chance =
            0.15f;


        // Patch noise. Set NoiseThreshold = 0 to disable the
        // patch restriction.
        public float NoiseScale =
            0.08f;


        public float NoiseThreshold =
            0f;
    }
}
