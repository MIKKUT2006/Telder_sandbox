using System.Collections.Generic;

using Game.Blocks;
using Game.Content;
using Game.World.Lighting;


namespace Game.World
{
    public class World :
        ILightWorld
    {
        // =====================================================
        // CHUNKS
        // =====================================================

        private readonly Dictionary<
            Vector2Int,
            Chunk
        >
        chunks =
            new Dictionary<
                Vector2Int,
                Chunk
            >();


        // =====================================================
        // LIGHT ENGINE
        // =====================================================

        private readonly LightPropagationEngine lightEngine;


        // =====================================================
        // WORLD HEIGHT
        // =====================================================

        private int worldHeight = 256;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public World()
        {
            lightEngine =
                new LightPropagationEngine(
                    this
                );
        }


        // =====================================================
        // WORLD HEIGHT
        // =====================================================

        public void SetWorldHeight(
            int height
        )
        {
            if (
                height <= 0
            )
            {
                return;
            }


            worldHeight =
                height;
        }


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
                EnsureLightData(
                    position
                );


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


            EnsureLightData(
                position
            );


            return chunk;
        }


        // =====================================================
        // CHUNK GENERATED
        // =====================================================

        public void NotifyChunkGenerated(
            int chunkX,
            int chunkY
        )
        {
            lightEngine.RebuildAfterChunkGenerated(
                chunkX,
                chunkY,
                worldHeight
            );
        }


        // =====================================================
        // REBUILD LIGHT
        // =====================================================

        public void RebuildLighting()
        {
            lightEngine.RebuildLoadedWorld(
                worldHeight
            );
        }


        // =====================================================
        // GET ENGINE
        // =====================================================

        public LightPropagationEngine GetLightEngine()
        {
            return lightEngine;
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
        // GET LOADED CHUNKS
        // =====================================================

        public IEnumerable<
            KeyValuePair<
                Vector2Int,
                Chunk
            >
        >
        GetLoadedChunks()
        {
            return chunks;
        }


        // =====================================================
        // REMOVE CHUNK
        // =====================================================

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


        // =====================================================
        // GET BLOCK
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


            return chunk.GetBlock(
                localX,
                localY
            );
        }


        public ushort GetLiquidID(int worldX, int worldY)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); return c == null ? (ushort)0 : c.GetLiquidID(lx, ly);
        }

        public byte GetLiquidAmount(int worldX, int worldY)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); return c == null ? (byte)0 : c.GetLiquidAmount(lx, ly);
        }

        public bool GetLiquidIsSource(int worldX, int worldY)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); return c != null && c.GetLiquidIsSource(lx, ly);
        }

        public byte GetLiquidFlowDistance(int worldX, int worldY)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); return c == null ? (byte)0 : c.GetLiquidFlowDistance(lx, ly);
        }

        public bool GetLiquidIsFalling(int worldX, int worldY)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); return c != null && c.GetLiquidIsFalling(lx, ly);
        }

        public bool SetLiquidState(
            int worldX,
            int worldY,
            ushort id,
            byte amount,
            bool isSource,
            byte flowDistance,
            bool isFalling
        )
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy);
            if (c == null) return false;

            c.SetLiquidState(lx, ly, id, amount, isSource, flowDistance, isFalling);
            return true;
        }

        public bool SetLiquidSource(int worldX, int worldY, ushort id)
        {
            return SetLiquidState(worldX, worldY, id, 8, true, 0, false);
        }

        public bool SetLiquid(int worldX, int worldY, ushort id, byte amount)
        {
            GetChunkCoordinates(worldX, worldY, out int cx, out int cy, out int lx, out int ly);
            Chunk c = GetChunk(cx, cy); if (c == null) return false;
            c.SetLiquid(lx, ly, id, amount); return true;
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


            // =================================================
            // LIGHT REBUILD
            // =================================================

            lightEngine.RebuildAfterBlockChanged(
                worldX,
                worldY,
                worldHeight
            );


            return true;
        }


        // =====================================================
        // GET BACKGROUND
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


            return chunk.GetBackground(
                localX,
                localY
            );
        }


        // =====================================================
        // SET BACKGROUND
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
        // IS LOADED
        // =====================================================

        public bool IsLoaded(
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


            return
                GetChunk(
                    chunkX,
                    chunkY
                ) != null;
        }


        // =====================================================
        // GET LIGHT DATA
        // =====================================================

        public ChunkLightData GetLightData(
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
                return null;
            }


            return chunk.GetLightData();
        }


        // =====================================================
        // GET LIGHT
        // =====================================================

        public LightNode GetLight(
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
                return LightNode.None;
            }


            ChunkLightData data =
                chunk.GetLightData();


            return data.Get(
                localX,
                localY
            );
        }


        // =====================================================
        // SET LIGHT
        // =====================================================

        public void SetLight(
            int worldX,
            int worldY,
            byte sun,
            byte red,
            byte green,
            byte blue
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
                return;
            }


            ChunkLightData data =
                chunk.GetLightData();


            data.Set(
                localX,
                localY,
                new LightNode(
                    sun,
                    red,
                    green,
                    blue
                )
            );
        }


        // =====================================================
        // BLOCK DEFINITION
        // =====================================================

        public BlockDefinition GetBlockDefinition(
            ushort blockID
        )
        {
            // ID 0 = AIR.
            // Никакого JSON для воздуха.

            if (
                blockID == 0
            )
            {
                return null;
            }


            ContentID contentID =
                BlockIDRegistry.GetContentID(
                    blockID
                );


            if (
                !BlockRegistry.Contains(
                    contentID
                )
            )
            {
                return null;
            }


            return BlockRegistry.Get(
                contentID
            );
        }


        // =====================================================
        // ENSURE LIGHT DATA
        // =====================================================

        private ChunkLightData EnsureLightData(
            Vector2Int position
        )
        {
            Chunk chunk =
                GetChunk(
                    position.x,
                    position.y
                );


            if (
                chunk == null
            )
            {
                return null;
            }


            return chunk.GetLightData();
        }


        // =====================================================
        // COORDINATES
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
                result += divisor;
            }


            return result;
        }
    }
}