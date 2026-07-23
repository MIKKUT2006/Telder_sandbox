using System.Collections.Generic;
using UnityEngine;


namespace Game.World
{

    public class World
    {

        private readonly Dictionary<Vector2Int, Chunk> chunks =
            new Dictionary<Vector2Int, Chunk>();


        // =====================================================
        // CHUNKS
        // =====================================================

        public Chunk GetChunk(
            int x,
            int y
        )
        {

            Vector2Int position =
                new Vector2Int(
                    x,
                    y
                );


            if (
                chunks.TryGetValue(
                    position,
                    out Chunk chunk
                )
            )
            {
                return chunk;
            }


            return null;

        }


        public Chunk CreateChunk(
            int x,
            int y
        )
        {

            Vector2Int position =
                new Vector2Int(
                    x,
                    y
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
                    x,
                    y
                );


            chunks.Add(
                position,
                chunk
            );


            return chunk;

        }


        public void RemoveChunk(
            int chunkX,
            int chunkY
        )
        {

            Vector2Int position =
                new Vector2Int(
                    chunkX,
                    chunkY
                );


            chunks.Remove(
                position
            );

        }


        public bool HasChunk(
            int x,
            int y
        )
        {

            return chunks.ContainsKey(
                new Vector2Int(
                    x,
                    y
                )
            );

        }


        public void Clear()
        {

            chunks.Clear();

        }


        public int ChunkCount
        {
            get
            {
                return chunks.Count;
            }
        }


        // =====================================================
        // BLOCK GET
        // =====================================================

        public ushort GetBlock(
            int worldX,
            int worldY
        )
        {

            int chunkX =
                WorldToChunk(
                    worldX
                );


            int chunkY =
                WorldToChunk(
                    worldY
                );


            Chunk chunk =
                GetChunk(
                    chunkX,
                    chunkY
                );


            // Чанк не загружен.
            // Для игрового мира считаем это воздухом.

            if (
                chunk == null
            )
            {
                return 0;
            }


            int localX =
                WorldToLocal(
                    worldX
                );


            int localY =
                WorldToLocal(
                    worldY
                );


            return chunk.GetBlock(
                localX,
                localY
            );

        }


        // =====================================================
        // BLOCK SET
        // =====================================================

        public bool SetBlock(
            int worldX,
            int worldY,
            ushort blockID
        )
        {

            int chunkX =
                WorldToChunk(
                    worldX
                );


            int chunkY =
                WorldToChunk(
                    worldY
                );


            Chunk chunk =
                GetChunk(
                    chunkX,
                    chunkY
                );


            // Нельзя изменить блок
            // в незагруженном чанке.

            if (
                chunk == null
            )
            {
                return false;
            }


            int localX =
                WorldToLocal(
                    worldX
                );


            int localY =
                WorldToLocal(
                    worldY
                );


            ushort oldBlockID =
                chunk.GetBlock(
                    localX,
                    localY
                );


            // Если блок уже такой же,
            // ничего не делаем.

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
        // WORLD → CHUNK
        // =====================================================

        private int WorldToChunk(
            int coordinate
        )
        {

            return Mathf.FloorToInt(
                (float)coordinate /
                Chunk.SizeX
            );

        }


        // =====================================================
        // WORLD → LOCAL
        // =====================================================

        private int WorldToLocal(
            int coordinate
        )
        {

            int local =
                coordinate %
                Chunk.SizeX;


            if (
                local < 0
            )
            {
                local +=
                    Chunk.SizeX;
            }


            return local;

        }

    }

}