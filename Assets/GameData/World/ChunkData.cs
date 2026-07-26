using System;


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

            if (
                x < 0 ||
                x >= Chunk.SizeX ||
                y < 0 ||
                y >= Chunk.SizeY
            )
            {
                return 0;
            }


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

            if (
                x < 0 ||
                x >= Chunk.SizeX ||
                y < 0 ||
                y >= Chunk.SizeY
            )
            {
                return;
            }


            Blocks[
                x +
                y *
                Chunk.SizeX
            ] =
                blockID;

        }

    }

}