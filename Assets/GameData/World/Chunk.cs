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
        private bool[,] liquidSources;
        private byte[,] liquidFlowDistances;
        private bool[,] liquidFalling;


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
            liquidSources = new bool[SizeX, SizeY];
            liquidFlowDistances = new byte[SizeX, SizeY];
            liquidFalling = new bool[SizeX, SizeY];


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
        public bool GetLiquidIsSource(int x, int y) { return liquidSources[x, y]; }
        public byte GetLiquidFlowDistance(int x, int y) { return liquidFlowDistances[x, y]; }
        public bool GetLiquidIsFalling(int x, int y) { return liquidFalling[x, y]; }

        public void SetLiquidSource(int x, int y, ushort id)
        {
            SetLiquidState(x, y, id, 8, true, 0, false);
        }

        public void SetLiquidState(
            int x,
            int y,
            ushort id,
            byte amount,
            bool isSource,
            byte flowDistance,
            bool isFalling
        )
        {
            amount = System.Math.Min((byte)8, amount);

            if (id == 0 || amount == 0)
            {
                liquidIDs[x, y] = 0;
                liquidAmounts[x, y] = 0;
                liquidSources[x, y] = false;
                liquidFlowDistances[x, y] = 0;
                liquidFalling[x, y] = false;
                return;
            }

            liquidIDs[x, y] = id;
            liquidAmounts[x, y] = isSource ? (byte)8 : amount;
            liquidSources[x, y] = isSource;
            liquidFlowDistances[x, y] = isSource ? (byte)0 : flowDistance;
            liquidFalling[x, y] = !isSource && isFalling;
        }

        public void SetLiquid(int x, int y, ushort id, byte amount)
        {
            if (amount == 0 || id == 0)
            {
                SetLiquidState(x, y, 0, 0, false, 0, false);
                return;
            }

            byte clamped = System.Math.Min((byte)8, amount);
            bool source = clamped >= 8;
            byte distance = source ? (byte)0 : (byte)System.Math.Max(1, 8 - clamped);
            SetLiquidState(x, y, id, clamped, source, distance, false);
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

                    SetLiquidState(
                        x,
                        y,
                        data.GetLiquidID(x, y),
                        data.GetLiquidAmount(x, y),
                        data.GetLiquidIsSource(x, y),
                        data.GetLiquidFlowDistance(x, y),
                        data.GetLiquidIsFalling(x, y)
                    );
                }
            }
        }
    }
}