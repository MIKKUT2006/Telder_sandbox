
using System;


namespace Game.World
{

    public class BlockStorage
    {

        private readonly ushort[] blocks;


        public int Width
        {
            get;
        }


        public int Height
        {
            get;
        }


        public BlockStorage(
            int width,
            int height
        )
        {

            Width =
                width;

            Height =
                height;


            blocks =
                new ushort[
                    width *
                    height
                ];

        }


        private int GetIndex(
            int x,
            int y
        )
        {

            return
                x +
                y *
                Width;

        }


        public ushort Get(
            int x,
            int y
        )
        {

            return blocks[
                GetIndex(
                    x,
                    y
                )
            ];

        }


        public void Set(
            int x,
            int y,
            ushort id
        )
        {

            blocks[
                GetIndex(
                    x,
                    y
                )
            ] =
                id;

        }


        public void Fill(
            ushort id
        )
        {

            if (
                id == 0
            )
            {

                Clear();

                return;

            }


            for (
                int i = 0;
                i < blocks.Length;
                i++
            )
            {

                blocks[i] =
                    id;

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


        public ushort[] GetRawData()
        {

            return blocks;

        }

    }

}

