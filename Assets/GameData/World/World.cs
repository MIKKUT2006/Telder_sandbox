using System.Collections.Generic;

namespace Game.World
{

    public class World
    {

        private readonly Dictionary<Vector2Int, Chunk> chunks =
            new Dictionary<Vector2Int, Chunk>();


        // =====================================================
        // CREATE CHUNK
        // =====================================================

        public Chunk CreateChunk(
            int chunkX,
            int chunkY
        )
        {

            Vector2Int position =
                new Vector2Int(
                    chunkX,
                    chunkY
                );


            if (
                chunks.TryGetValue(
                    position,
                    out Chunk existingChunk
                )
            )
            {

                return existingChunk;

            }


            Chunk chunk =
                new Chunk(
                    chunkX,
                    chunkY
                );


            chunks.Add(
                position,
                chunk
            );


            return chunk;

        }


        // =====================================================
        // GET CHUNK
        // =====================================================

        public Chunk GetChunk(
            int chunkX,
            int chunkY
        )
        {

            chunks.TryGetValue(
                new Vector2Int(
                    chunkX,
                    chunkY
                ),
                out Chunk chunk
            );


            return chunk;

        }


        // =====================================================
        // REMOVE CHUNK
        // =====================================================

        public void RemoveChunk(
            int chunkX,
            int chunkY
        )
        {

            chunks.Remove(
                new Vector2Int(
                    chunkX,
                    chunkY
                )
            );

        }


        // =====================================================
        // GET BLOCK
        // =====================================================

        public ushort GetBlock(
            int worldX,
            int worldY
        )
        {

            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );


            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );


            int localX =
                Mod(
                    worldX,
                    Chunk.SizeX
                );


            int localY =
                Mod(
                    worldY,
                    Chunk.SizeY
                );


            Chunk chunk =
                GetChunk(
                    chunkX,
                    chunkY
                );


            if (
                chunk == null
            )
            {

                return 0;

            }


            return chunk.GetBlock(
                localX,
                localY
            );

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

            int chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );


            int chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );


            int localX =
                Mod(
                    worldX,
                    Chunk.SizeX
                );


            int localY =
                Mod(
                    worldY,
                    Chunk.SizeY
                );


            Chunk chunk =
                GetChunk(
                    chunkX,
                    chunkY
                );


            // =================================================
            // ЧАНК НЕ ЗАГРУЖЕН
            // =================================================

            if (
                chunk == null
            )
            {

                return false;

            }


            // =================================================
            // ПРОВЕРЯЕМ СТАРОЕ ЗНАЧЕНИЕ
            // =================================================

            ushort oldBlockID =
                chunk.GetBlock(
                    localX,
                    localY
                );


            if (
                oldBlockID ==
                blockID
            )
            {

                return false;

            }


            // =================================================
            // МЕНЯЕМ БЛОК
            // =================================================

            chunk.SetBlock(
                localX,
                localY,
                blockID
            );


            return true;

        }


        // =====================================================
        // FLOOR DIV
        // =====================================================

        private int FloorDiv(
            int value,
            int divisor
        )
        {

            int result =
                value /
                divisor;


            int remainder =
                value %
                divisor;


            if (
                remainder != 0 &&
                value < 0
            )
            {

                result--;

            }


            return result;

        }


        // =====================================================
        // MOD
        // =====================================================

        private int Mod(
            int value,
            int divisor
        )
        {

            int result =
                value %
                divisor;


            if (
                result < 0
            )
            {

                result +=
                    divisor;

            }


            return result;

        }

    }

}