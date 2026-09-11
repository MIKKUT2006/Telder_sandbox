
using System;


namespace Game.World.Collision
{

    public enum BlockCollisionShape
    {

        None = 0,

        Full = 1,

        HalfBottom = 2,

        HalfTop = 3,

        StairUpRight = 4,

        StairUpLeft = 5,

        Custom = 6

    }


    public static class BlockCollisionShapeParser
    {

        public static BlockCollisionShape Parse(
            string value
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {

                return
                    BlockCollisionShape.Full;

            }


            if (
                Enum.TryParse(
                    value,
                    true,
                    out BlockCollisionShape result
                )
            )
            {

                return result;

            }


            return
                BlockCollisionShape.Full;

        }

    }

}
