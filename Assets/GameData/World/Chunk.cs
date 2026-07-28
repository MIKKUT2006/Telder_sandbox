using Game.World;

namespace Game.World
{

    public class Chunk
    {

        // =====================================================
        // SIZE
        // =====================================================

        public const int SizeX =
            32;


        public const int SizeY =
            32;


        // =====================================================
        // POSITION
        // =====================================================

        public int X;

        public int Y;


        // =====================================================
        // BLOCK STORAGE
        // =====================================================

        // Передний слой.
        //
        // Здесь находятся:
        // stone
        // dirt
        // grass
        // ores
        // и другие обычные блоки.
        //
        // Этот слой участвует в коллизии.

        private BlockStorage foregroundBlocks;


        // =====================================================
        // BACKGROUND STORAGE
        // =====================================================

        // Задний слой.
        //
        // Например:
        // stone_wall
        // dirt_wall
        // cave_wall
        //
        // Этот слой НЕ участвует
        // в физической коллизии.

        private BlockStorage backgroundBlocks;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public Chunk(
            int x,
            int y
        )
        {

            X =
                x;


            Y =
                y;


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


            Initialize();

        }


        // =====================================================
        // INITIALIZE
        // =====================================================

        private void Initialize()
        {

            // =================================================
            // FOREGROUND
            // =================================================

            foregroundBlocks.Fill(
                0
            );


            // =================================================
            // BACKGROUND
            // =================================================

            backgroundBlocks.Fill(
                0
            );

        }


        // =====================================================
        // FOREGROUND
        // GET
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
        // FOREGROUND
        // SET
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
        // BACKGROUND
        // GET
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
        // BACKGROUND
        // SET
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

                    // =========================================
                    // FOREGROUND
                    // =========================================

                    SetBlock(
                        x,
                        y,
                        data.GetBlock(
                            x,
                            y
                        )
                    );


                    // =========================================
                    // BACKGROUND
                    // =========================================

                    SetBackground(
                        x,
                        y,
                        data.GetBackground(
                            x,
                            y
                        )
                    );

                }

            }

        }

    }

}