using System;

namespace Game.World.Biomes
{
    /// <summary>
    /// One liquid that may be chosen by the procedural liquid generators.
    /// The old surface-lake fields are kept for backwards compatibility.
    /// </summary>
    [Serializable]
    public class BiomeLiquidEntry
    {
        public string ID = "game:water";

        // Relative chance against the other liquids in the same biome.
        public float Weight = 1f;

        // Chance for one surface-lake candidate in a surface generation region.
        public float LakeChance = 0.08f;

        // Optional cave-specific chance. < 0 means: use LakeChance.
        public float PoolChance = -1f;

        // Approximate amount/footprint. Existing biome JSON can keep using these.
        public int MinLakeSize = 20;
        public int MaxLakeSize = 120;

        // Optional explicit full width in blocks. 0 = derive from LakeSize.
        public int MinWidth = 0;
        public int MaxWidth = 0;

        public int MinDepth = 2;
        public int MaxDepth = 7;

        // Legacy V1/V2 field. V3 world generation always places true source
        // blocks, so generated liquid is always full and stable. Kept only so
        // existing biome JSON continues to deserialize without changes.
        public int FillAmount = 8;
    }

    /// <summary>
    /// Surface biome liquid generation.
    /// </summary>
    [Serializable]
    public class BiomeLiquidSettings
    {
        public bool Enabled = true;

        // Surface lakes are generated on a deterministic global grid.
        // Larger values = fewer possible lakes. Clamped at runtime.
        public int RegionSize = 48;

        // A lake is rejected when the natural terrain across its footprint is
        // steeper than this. This prevents vertical cut-outs on mountains.
        public int MaxSurfaceVariation = 7;

        // 0 = perfectly smooth ellipse, 1 = very rough shore. 0.15..0.35 is sane.
        public float EdgeRoughness = 0.22f;

        public BiomeLiquidEntry[] Liquids = new BiomeLiquidEntry[0];
    }

    /// <summary>
    /// Cave-biome liquid generation. It does not blindly carve large cavities:
    /// it injects liquid above real cave floors and lets LiquidRuntime settle it.
    /// </summary>
    [Serializable]
    public class CaveBiomeLiquidSettings
    {
        public bool Enabled = false;

        // Candidate grid size. The same candidate is seen by neighbouring chunks,
        // so pools can cross horizontal and vertical chunk borders naturally.
        public int HorizontalRegionSize = 24;
        public int VerticalRegionSize = 16;

        // Organic outline of cave pools.
        public float EdgeRoughness = 0.28f;

        // Usually true: prevents liquids from appearing in completely open voids.
        public bool RequireBackground = true;

        public BiomeLiquidEntry[] Liquids = new BiomeLiquidEntry[0];
    }
}
