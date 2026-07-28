using System.Collections.Generic;

namespace Game.World
{

    public class World
    {

        private readonly Dictionary<
            UnityEngine.Vector2Int,
            Chunk
        >
        chunks =
            new Dictionary<
                UnityEngine.Vector2Int,
                Chunk
            >();


        // =====================================================
        // CREATE CHUNK
        // =====================================================

        public Chunk CreateChunk(
            int chunkX,
            int chunkY
        )
        {

            UnityEngine.Vector2Int position =
                new UnityEngine.Vector2Int(
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
                new UnityEngine.Vector2Int(
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
                new UnityEngine.Vector2Int(
                    chunkX,
                    chunkY
                )
            );

        }


        // =====================================================
        // GET FOREGROUND BLOCK
        // =====================================================

        public ushort GetBlock(
            int worldX,
            int worldY
        )
        {

            GetChunkCoordinates(
                worldX,
                worldY,
                out int chunkX,
                out int chunkY,
                out int localX,
                out int localY
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


            return
                chunk.GetBlock(
                    localX,
                    localY
                );

        }


        // =====================================================
        // SET FOREGROUND BLOCK
        // =====================================================

        public bool SetBlock(
            int worldX,
            int worldY,
            ushort blockID
        )
        {

            GetChunkCoordinates(
                worldX,
                worldY,
                out int chunkX,
                out int chunkY,
                out int localX,
                out int localY
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

                return false;

            }


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


            chunk.SetBlock(
                localX,
                localY,
                blockID
            );


            return true;

        }


        // =====================================================
        // GET BACKGROUND BLOCK
        // =====================================================

        public ushort GetBackground(
            int worldX,
            int worldY
        )
        {

            GetChunkCoordinates(
                worldX,
                worldY,
                out int chunkX,
                out int chunkY,
                out int localX,
                out int localY
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


            return
                chunk.GetBackground(
                    localX,
                    localY
                );

        }


        // =====================================================
        // SET BACKGROUND BLOCK
        // =====================================================

        public bool SetBackground(
            int worldX,
            int worldY,
            ushort blockID
        )
        {

            GetChunkCoordinates(
                worldX,
                worldY,
                out int chunkX,
                out int chunkY,
                out int localX,
                out int localY
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

                return false;

            }


            ushort oldBlockID =
                chunk.GetBackground(
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


            chunk.SetBackground(
                localX,
                localY,
                blockID
            );


            return true;

        }


        // =====================================================
        // GET CHUNK COORDINATES
        // =====================================================

        private void GetChunkCoordinates(
            int worldX,
            int worldY,
            out int chunkX,
            out int chunkY,
            out int localX,
            out int localY
        )
        {

            chunkX =
                FloorDiv(
                    worldX,
                    Chunk.SizeX
                );


            chunkY =
                FloorDiv(
                    worldY,
                    Chunk.SizeY
                );


            localX =
                Mod(
                    worldX,
                    Chunk.SizeX
                );


            localY =
                Mod(
                    worldY,
                    Chunk.SizeY
                );

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