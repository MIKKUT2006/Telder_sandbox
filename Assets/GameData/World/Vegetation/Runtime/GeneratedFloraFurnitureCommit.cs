using Game.Content;
using Game.World.Furniture;


namespace Game.World.Vegetation.Runtime
{
    /// <summary>
    /// Moves numeric procedural furniture from ChunkData into the live
    /// FurnitureLayerManager. Must be called after Chunk.ApplyData().
    /// </summary>
    public static class GeneratedFloraFurnitureCommit
    {
        public static void Apply(
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (data == null)
            {
                return;
            }


            FurnitureLayerManager manager =
                FurnitureLayerManager.EnsureInstance();


            if (manager == null)
            {
                return;
            }


            int startX =
                chunkX *
                Chunk.SizeX;


            int startY =
                chunkY *
                Chunk.SizeY;


            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                for (
                    int localY = 0;
                    localY < Chunk.SizeY;
                    localY++
                )
                {
                    ushort blockId =
                        data.GetFurniture(
                            localX,
                            localY
                        );


                    if (blockId == 0)
                    {
                        continue;
                    }


                    string contentId;


                    try
                    {
                        contentId =
                            BlockIDRegistry
                                .GetContentID(
                                    blockId
                                )
                                .ToString();
                    }
                    catch
                    {
                        continue;
                    }


                    if (
                        string.IsNullOrWhiteSpace(
                            contentId
                        )
                    )
                    {
                        continue;
                    }


                    manager.SetGeneratedFurniture(
                        startX + localX,
                        startY + localY,
                        contentId
                    );
                }
            }
        }


        public static void Unload(
            int chunkX,
            int chunkY
        )
        {
            if (
                FurnitureLayerManager.Instance ==
                null
            )
            {
                return;
            }


            FurnitureLayerManager.Instance
                .UnloadGeneratedFurnitureChunk(
                    chunkX,
                    chunkY
                );
        }
    }
}
