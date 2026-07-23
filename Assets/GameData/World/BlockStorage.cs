using System;


namespace Game.World
{

    public class BlockStorage
    {

        private readonly ushort[,] blocks;



        public int Width { get; }

        public int Height { get; }



        public BlockStorage(
            int width,
            int height
        )
        {

            Width = width;

            Height = height;


            blocks =
                new ushort[width, height];

        }





        public ushort Get(
            int x,
            int y
        )
        {

            return blocks[x, y];

        }





        public void Set(
            int x,
            int y,
            ushort id
        )
        {

            blocks[x, y] = id;

        }





        public void Fill(
            ushort id
        )
        {

            for (int x = 0; x < Width; x++)
            {

                for (int y = 0; y < Height; y++)
                {

                    blocks[x, y] = id;

                }

            }

        }





        public void Clear()
        {

            Array.Clear(
                blocks,
                0,
                blocks.Length
            );

        }

    }

}