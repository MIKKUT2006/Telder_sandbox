
namespace Game.World
{

    public sealed class ChunkData
    {

        public readonly int X;

        public readonly int Y;


        public readonly ushort[] Blocks;


        public ChunkData(
            int x,
            int y
        )
        {

            X =
                x;

            Y =
                y;


            Blocks =
                new ushort[
                    Chunk.SizeX *
                    Chunk.SizeY
                ];

        }


        public ushort GetBlock(
            int x,
            int y
        )
        {

            return Blocks[
                x +
                y *
                Chunk.SizeX
            ];

        }


        public void SetBlock(
            int x,
            int y,
            ushort blockID
        )
        {

            Blocks[
                x +
                y *
                Chunk.SizeX
            ] =
                blockID;

        }

    }

}

