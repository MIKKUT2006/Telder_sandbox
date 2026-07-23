namespace Game.World
{

    public static class WorldPosition
    {


        public static Vector2Int GetChunkPosition(
            int worldX,
            int worldY,
            int chunkSize
        )
        {

            int chunkX =
                FloorDiv(
                    worldX,
                    chunkSize
                );


            int chunkY =
                FloorDiv(
                    worldY,
                    chunkSize
                );


            return new Vector2Int(
                chunkX,
                chunkY
            );

        }





        public static Vector2Int GetBlockPosition(
            int worldX,
            int worldY,
            int chunkSize
        )
        {

            int blockX =
                Mod(
                    worldX,
                    chunkSize
                );


            int blockY =
                Mod(
                    worldY,
                    chunkSize
                );


            return new Vector2Int(
                blockX,
                blockY
            );

        }





        private static int FloorDiv(
            int a,
            int b
        )
        {

            int result =
                a / b;


            if (
                (a ^ b) < 0 &&
                a % b != 0
            )
            {
                result--;
            }


            return result;

        }





        private static int Mod(
            int a,
            int b
        )
        {

            return
                ((a % b) + b) % b;

        }


    }

}