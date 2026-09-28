namespace Game.World.Vegetation.Runtime
{
    public static class GeneratedVegetationFurnitureBridge
    {
        public static void Apply(
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            GeneratedFloraFurnitureCommit.Apply(
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
            GeneratedFloraFurnitureCommit.Unload(
                chunkX,
                chunkY
            );
        }
    }
}
