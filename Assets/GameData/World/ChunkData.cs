namespace Game.World
{

    public class ChunkData
    {

        // =====================================================
        // STORAGE
        // =====================================================

        private readonly BlockStorage foregroundBlocks;

        private readonly BlockStorage backgroundBlocks;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

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


            foregroundBlocks.Fill(
                0
            );


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

    }

}