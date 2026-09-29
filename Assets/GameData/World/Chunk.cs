using Game.World.Lighting;

namespace Game.World
{
    public class Chunk
    {
        // =====================================================
        // SIZE
        // =====================================================

        public const int SizeX = 32;

        public const int SizeY = 32;


        // =====================================================
        // POSITION
        // =====================================================

        public int X;

        public int Y;


        // =====================================================
        // BLOCK STORAGE
        // =====================================================

        private BlockStorage foregroundBlocks;


        // =====================================================
        // BACKGROUND STORAGE
        // =====================================================

        private BlockStorage backgroundBlocks;

        private ushort[,] liquidIDs;
        private byte[,] liquidAmounts;


        // =====================================================
        // LIGHT STORAGE
        // =====================================================

        private readonly ChunkLightData lightData;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Chunk(
            int x,
            int y
        )
        {
            X = x;

            Y = y;


            // =================================================
            // FOREGROUND
            // =================================================

            foregroundBlocks =
                new BlockStorage(
                    SizeX,
                    SizeY
                );


            // =================================================
            // BACKGROUND
            // =================================================

            backgroundBlocks =
                new BlockStorage(
                    SizeX,
                    SizeY
                );

            liquidIDs = new ushort[SizeX, SizeY];
            liquidAmounts = new byte[SizeX, SizeY];


            // =================================================
            // LIGHT
            // =================================================

            lightData =
                new ChunkLightData();


            Initialize();
        }


        // =====================================================
        // INITIALIZE
        // =====================================================

        private void Initialize()
        {
            foregroundBlocks.Fill(0);

            backgroundBlocks.Fill(0);

            lightData.Clear();
        }


        // =====================================================
        // FOREGROUND GET
        // =====================================================

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


        // =====================================================
        // FOREGROUND SET
        // =====================================================

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


        // =====================================================
        // BACKGROUND GET
        // =====================================================

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


        // =====================================================
        // BACKGROUND SET
        // =====================================================

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
        public void SetLiquid(int x, int y, ushort id, byte amount)
        {
            liquidIDs[x, y] = amount == 0 ? (ushort)0 : id;
            liquidAmounts[x, y] = (byte)System.Math.Min((byte)8, amount);
        }

        // =====================================================
        // LIGHT DATA
        // =====================================================

        public ChunkLightData GetLightData()
        {
                return lightData;
        }


        // =====================================================
        // APPLY CHUNK DATA
        // =====================================================

        public void ApplyData(
            ChunkData data
        )
        {
            if (
                data == null
            )
            {
                return;
            }


            for (
                int x = 0;
                x < SizeX;
                x++
            )
            {
                for (
                    int y = 0;
                    y < SizeY;
                    y++
                )
                {
                    SetBlock(
                        x,
                        y,
                        data.GetBlock(
                            x,
                            y
                        )
                    );


                    SetBackground(
                        x,
                        y,
                        data.GetBackground(
                            x,
                            y
                        )
                    );

                    SetLiquid(x, y, data.GetLiquidID(x, y), data.GetLiquidAmount(x, y));
                }
            }
        }
    }
}