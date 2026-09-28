namespace Game.World.PolishFixes
{
    public static class GeneratedFurnitureChunkBridge
    {
        public static void Apply(
            Game.World.ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            Game.World.Vegetation.Runtime
                .GeneratedFloraFurnitureCommit
                .Apply(
                    data,
                    chunkX,
                    chunkY
                );
        }


        public static void Unload(
            int chunkX,
            int chunkY
        )
        {
            Game.World.Vegetation.Runtime
                .GeneratedFloraFurnitureCommit
                .Unload(
                    chunkX,
                    chunkY
                );
        }
    }
}
