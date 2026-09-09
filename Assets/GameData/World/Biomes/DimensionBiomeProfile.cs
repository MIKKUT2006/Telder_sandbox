using System.Collections.Generic;

namespace Game.World.Biomes
{
    public sealed class DimensionBiomeProfile
    {
        public readonly List<BiomeDefinition>
            SurfaceBiomes =
            new List<BiomeDefinition>();

        public int BaseRegionSize = 520;
        public int RegionJitter = 120;
        public int BlendWidth = 64;
        public float BoundaryWarp = 55f;

        public bool IsValid
        {
            get
            {
                return
                    SurfaceBiomes.Count >
                    0;
            }
        }
    }
}