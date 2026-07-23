using Game.Content;
using Game.World.Collision;
using Game.World.Rendering;
using UnityEngine;


namespace Game.World.Modification
{

    public class BlockModificationService
    {

        private readonly World world;

        private readonly ChunkRenderer renderer;

        private readonly ChunkCollision collision;


        public BlockModificationService(
            World world,
            ChunkRenderer renderer,
            ChunkCollision collision
        )
        {

            this.world =
                world;

            this.renderer =
                renderer;

            this.collision =
                collision;

        }


        // =====================================================
        // SET BLOCK
        // =====================================================

        public bool SetBlock(
            int worldX,
            int worldY,
            ushort blockID
        )
        {

            Chunk chunk =
                GetChunkAt(
                    worldX,
                    worldY
                );


            if (
                chunk == null
            )
            {
                return false;
            }


            bool changed =
                world.SetBlock(
                    worldX,
                    worldY,
                    blockID
                );


            if (
                !changed
            )
            {
                return false;
            }


            RefreshChunk(
                chunk
            );


            RefreshNeighbourChunks(
                worldX,
                worldY
            );


            return true;

        }


        // =====================================================
        // BREAK BLOCK
        // =====================================================

        public bool BreakBlock(
            int worldX,
            int worldY
        )
        {

            ushort blockID =
                world.GetBlock(
                    worldX,
                    worldY
                );


            if (
                blockID == 0
            )
            {
                return false;
            }


            return SetBlock(
                worldX,
                worldY,
                0
            );

        }


        // =====================================================
        // PLACE BLOCK
        // =====================================================

        public bool PlaceBlock(
            int worldX,
            int worldY,
            ushort blockID
        )
        {

            if (
                blockID == 0
            )
            {
                return false;
            }


            ushort currentBlock =
                world.GetBlock(
                    worldX,
                    worldY
                );


            // Нельзя поставить блок
            // поверх другого блока.

            if (
                currentBlock != 0
            )
            {
                return false;
            }


            return SetBlock(
                worldX,
                worldY,
                blockID
            );

        }


        // =====================================================
        // GET CHUNK
        // =====================================================

        private Chunk GetChunkAt(
            int worldX,
            int worldY
        )
        {

            int chunkX =
                Mathf.FloorToInt(
                    (float)worldX /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    (float)worldY /
                    Chunk.SizeY
                );


            return world.GetChunk(
                chunkX,
                chunkY
            );

        }


        // =====================================================
        // REFRESH CHUNK
        // =====================================================

        private void RefreshChunk(
            Chunk chunk
        )
        {

            if (
                chunk == null
            )
            {
                return;
            }


            renderer.Render(
                chunk
            );


            collision.BuildChunkCollision(
                chunk
            );

        }


        // =====================================================
        // REFRESH NEIGHBOURS
        // =====================================================

        private void RefreshNeighbourChunks(
            int worldX,
            int worldY
        )
        {

            int localX = FloorMod(worldX,Chunk.SizeX);


            int localY = FloorMod(worldY,Chunk.SizeY);


            // Левая граница чанка.

            if (
                localX == 0
            )
            {

                RefreshChunkAt(
                    worldX - 1,
                    worldY
                );

            }


            // Правая граница чанка.

            if (
                localX == Chunk.SizeX - 1
            )
            {

                RefreshChunkAt(
                    worldX + 1,
                    worldY
                );

            }


            // Нижняя граница чанка.

            if (
                localY == 0
            )
            {

                RefreshChunkAt(
                    worldX,
                    worldY - 1
                );

            }


            // Верхняя граница чанка.

            if (
                localY == Chunk.SizeY - 1
            )
            {

                RefreshChunkAt(
                    worldX,
                    worldY + 1
                );

            }

        }


        // =====================================================
        // REFRESH CHUNK AT WORLD POSITION
        // =====================================================

        private void RefreshChunkAt(
            int worldX,
            int worldY
        )
        {

            int chunkX =
                Mathf.FloorToInt(
                    (float)worldX /
                    Chunk.SizeX
                );


            int chunkY =
                Mathf.FloorToInt(
                    (float)worldY /
                    Chunk.SizeY
                );


            Chunk chunk =
                world.GetChunk(
                    chunkX,
                    chunkY
                );


            if (
                chunk == null
            )
            {
                return;
            }


            RefreshChunk(
                chunk
            );

        }
        private int FloorMod(
    int value,
    int size
)
        {

            int result =
                value %
                size;


            if (
                result < 0
            )
            {
                result +=
                    size;
            }


            return result;

        }

    }

}