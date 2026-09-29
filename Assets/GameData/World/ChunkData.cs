using System;
using Unity.VisualScripting;

namespace Game.World
{
    /// <summary>
    /// Generation-time data for one chunk.
    ///
    /// Foreground/background are copied into Chunk by Chunk.ApplyData().
    /// Furniture is a procedural generation buffer. It is committed to
    /// FurnitureLayerManager after the chunk is created.
    /// </summary>
    public class ChunkData
    {
        private readonly BlockStorage foregroundBlocks;

        private readonly BlockStorage backgroundBlocks;

        private readonly BlockStorage furnitureBlocks;

        private readonly ushort[,] liquidIDs;
        private readonly byte[,] liquidAmounts;


        public ChunkData()
        {
            foregroundBlocks =
                new BlockStorage(
                    Chunk.SizeX,
                    Chunk.SizeY
                );


            backgroundBlocks =
                new BlockStorage(
                    Chunk.SizeX,
                    Chunk.SizeY
                );


            furnitureBlocks =
                new BlockStorage(
                    Chunk.SizeX,
                    Chunk.SizeY
                );

            liquidIDs = new ushort[Chunk.SizeX, Chunk.SizeY];
            liquidAmounts = new byte[Chunk.SizeX, Chunk.SizeY];


            foregroundBlocks.Fill(
                0
            );


            backgroundBlocks.Fill(
                0
            );


            furnitureBlocks.Fill(
                0
            );
        }


        public ushort GetBlock(
            int x,
            int y
        )
        {
            return
                foregroundBlocks.Get(
                    x,
                    y
                );
        }


        public void SetBlock(
            int x,
            int y,
            ushort blockID
        )
        {
            foregroundBlocks.Set(
                x,
                y,
                blockID
            );
        }


        public ushort GetBackground(
            int x,
            int y
        )
        {
            return
                backgroundBlocks.Get(
                    x,
                    y
                );
        }


        public void SetBackground(
            int x,
            int y,
            ushort blockID
        )
        {
            backgroundBlocks.Set(
                x,
                y,
                blockID
            );
        }


        public ushort GetLiquidID(int x, int y) { return liquidIDs[x, y]; }
        public byte GetLiquidAmount(int x, int y) { return liquidAmounts[x, y]; }
        public void SetLiquid(int x, int y, ushort liquidID, byte amount)
        {
            if (x < 0 || x >= Chunk.SizeX || y < 0 || y >= Chunk.SizeY) return;
            liquidIDs[x, y] = amount == 0 ? (ushort)0 : liquidID;
            liquidAmounts[x, y] = (byte)System.Math.Min((byte)8, amount);
        }

        // =====================================================
        // PROCEDURAL FURNITURE
        // =====================================================

        public ushort GetFurniture(
            int x,
            int y
        )
        {
            return
                furnitureBlocks.Get(
                    x,
                    y
                );
        }


        public bool SetFurniture(
            int x,
            int y,
            ushort blockID
        )
        {
            if (
                x < 0 ||
                x >= Chunk.SizeX ||
                y < 0 ||
                y >= Chunk.SizeY
            )
            {
                return false;
            }


            ushort old =
                furnitureBlocks.Get(
                    x,
                    y
                );


            if (
                old ==
                blockID
            )
            {
                return false;
            }


            furnitureBlocks.Set(
                x,
                y,
                blockID
            );


            return true;
        }
    }
}
