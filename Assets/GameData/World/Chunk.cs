namespace Game.World
{

    public class Chunk
    {

        // Размер чанка по X
        public const int SizeX = 32;


        // Размер чанка по Y
        public const int SizeY = 32;



        // Координаты чанка в мире
        public int X;

        public int Y;



        // Блоки внутри чанка
        private BlockStorage blocks;



        public Chunk(
            int x,
            int y
        )
        {

            X = x;

            Y = y;


            blocks = new BlockStorage(SizeX, SizeY);


            Initialize();

        }





        private void Initialize()
        {

            ushort airID = 0;


            blocks.Fill(
                airID
            );

        }





        public ushort GetBlock(int x, int y)
        {

            return blocks.Get(
                x,
                y
            );

        }





        public void SetBlock(int x,int y,ushort blockID)
        {

            blocks.Set(
                x,
                y,
                blockID
            );

        }


    }

}