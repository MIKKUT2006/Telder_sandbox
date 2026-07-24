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

        private BlockStorage blocks;


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


            blocks =
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

            blocks.Fill(
                0
            );

        }


        // =====================================================
        // GET BLOCK
        // =====================================================

        public ushort GetBlock(
            int x,
            int y
        )
        {

            return blocks.Get(
                x,
                y
            );

        }


        // =====================================================
        // SET BLOCK
        // =====================================================

        public void SetBlock(
            int x,
            int y,
            ushort blockID
        )
        {

            blocks.Set(
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

                    SetBlock(
                        x,
                        y,
                        data.GetBlock(
                            x,
                            y
                        )
                    );

                }

            }

        }

    }

}