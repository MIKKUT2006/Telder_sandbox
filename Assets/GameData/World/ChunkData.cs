namespace Game.World
{
    public class ChunkData
    {
        private readonly BlockStorage foregroundBlocks;

        private readonly BlockStorage backgroundBlocks;


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
    }
}