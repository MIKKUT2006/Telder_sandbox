
using System;
using System.Collections.Generic;


namespace Game.World.Biomes.Caves
{
    [Serializable]
    public class CaveBiomeDefinition
    {
        public string ID =
            "game:cave_biome";


        public string DisplayName =
            "Cave Biome";


        public bool Enabled =
            true;


        // Absolute world Y range.
        //
        // Example Ancient Caves:
        // MinY = -100000
        // MaxY = -80
        public int MinY =
            -100000;


        public int MaxY =
            -80;


        // Makes the upper border organic rather than a perfectly
        // straight horizontal line.
        public int TransitionHeight =
            18;


        public int Priority =
            0;


        // Empty = any dimension.
        public List<string> Dimensions =
            new List<string>();


        // Optional terrain overrides.
        //
        // Empty string = keep surface biome material.
        public string StoneBlockId =
            string.Empty;


        public string BackgroundBlockId =
            string.Empty;


        public string FloorBlockId =
            string.Empty;


        public string CeilingBlockId =
            string.Empty;


        // Additional cave pockets unique to this cave biome.
        // This works even when custom biome blocks are not ready yet.
        public bool ExtraCaves =
            false;


        public float ExtraCaveScale =
            0.018f;


        public float ExtraCaveDetailScale =
            0.055f;


        public float ExtraCaveThreshold =
            0.79f;
    }
}
