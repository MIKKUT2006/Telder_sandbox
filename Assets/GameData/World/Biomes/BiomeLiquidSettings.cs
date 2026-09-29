using System;

namespace Game.World.Biomes
{
    [Serializable]
    public class BiomeLiquidEntry
    {
        public string ID = "game:water";
        public float Weight = 1f;
        public float LakeChance = 0.08f;
        public int MinLakeSize = 20;
        public int MaxLakeSize = 120;
        public int MinDepth = 2;
        public int MaxDepth = 7;
    }

    [Serializable]
    public class BiomeLiquidSettings
    {
        public bool Enabled = true;
        public BiomeLiquidEntry[] Liquids = new BiomeLiquidEntry[0];
    }
}
