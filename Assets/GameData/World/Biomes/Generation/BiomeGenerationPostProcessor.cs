
using Game.World.Biomes.Caves;
using Game.World.Biomes.Surface;
using Game.World.Generation;


namespace Game.World.Biomes.Generation
{
    public static class BiomeGenerationPostProcessor
    {
        public static void ApplyToChunk(
            WorldGenerator generator,
            WorldSettings settings,
            Chunk chunk
        )
        {
            CaveBiomePostProcessor.ApplyToChunk(
                generator,
                settings,
                chunk
            );


            SurfaceFloraPostProcessor.ApplyToChunk(
                generator,
                settings,
                chunk
            );
        }
    }
}
