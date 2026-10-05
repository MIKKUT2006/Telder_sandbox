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
        private readonly bool[,] liquidSources;
        private readonly byte[,] liquidFlowDistances;
        private readonly bool[,] liquidFalling;


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
            liquidSources = new bool[Chunk.SizeX, Chunk.SizeY];
            liquidFlowDistances = new byte[Chunk.SizeX, Chunk.SizeY];
            liquidFalling = new bool[Chunk.SizeX, Chunk.SizeY];


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
        public bool GetLiquidIsSource(int x, int y) { return liquidSources[x, y]; }
        public byte GetLiquidFlowDistance(int x, int y) { return liquidFlowDistances[x, y]; }
        public bool GetLiquidIsFalling(int x, int y) { return liquidFalling[x, y]; }

        public void SetLiquidSource(int x, int y, ushort liquidID)
        {
            SetLiquidState(x, y, liquidID, 8, true, 0, false);
        }

        public void SetLiquidState(
            int x,
            int y,
            ushort liquidID,
            byte amount,
            bool isSource,
            byte flowDistance,
            bool isFalling
        )
        {
            if (x < 0 || x >= Chunk.SizeX || y < 0 || y >= Chunk.SizeY)
                return;

            amount = System.Math.Min((byte)8, amount);

            if (liquidID == 0 || amount == 0)
            {
                liquidIDs[x, y] = 0;
                liquidAmounts[x, y] = 0;
                liquidSources[x, y] = false;
                liquidFlowDistances[x, y] = 0;
                liquidFalling[x, y] = false;
                return;
            }

            liquidIDs[x, y] = liquidID;
            liquidAmounts[x, y] = isSource ? (byte)8 : amount;
            liquidSources[x, y] = isSource;
            liquidFlowDistances[x, y] = isSource ? (byte)0 : flowDistance;
            liquidFalling[x, y] = !isSource && isFalling;
        }

        // Compatibility API. A full cell placed by old generation code is
        // interpreted as a source; partial cells are ordinary flowing liquid.
        public void SetLiquid(int x, int y, ushort liquidID, byte amount)
        {
            if (amount == 0 || liquidID == 0)
            {
                SetLiquidState(x, y, 0, 0, false, 0, false);
                return;
            }

            byte clamped = System.Math.Min((byte)8, amount);
            bool source = clamped >= 8;
            byte distance = source ? (byte)0 : (byte)System.Math.Max(1, 8 - clamped);
            SetLiquidState(x, y, liquidID, clamped, source, distance, false);
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
