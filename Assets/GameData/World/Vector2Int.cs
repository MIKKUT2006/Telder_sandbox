using System;


namespace Game.World
{

    [Serializable]
    public struct Vector2Int
    {

        public int x;

        public int y;


        public Vector2Int(
            int x,
            int y
        )
        {

            this.x =
                x;

            this.y =
                y;

        }


        public override bool Equals(
            object obj
        )
        {

            if (
                obj is not Vector2Int
            )
            {
                return false;
            }


            Vector2Int other =
                (Vector2Int)obj;


            return
                x == other.x &&
                y == other.y;

        }


        public override int GetHashCode()
        {

            return HashCode.Combine(
                x,
                y
            );

        }


        public static bool operator ==(
            Vector2Int a,
            Vector2Int b
        )
        {

            return
                a.x == b.x &&
                a.y == b.y;

        }


        public static bool operator !=(
            Vector2Int a,
            Vector2Int b
        )
        {

            return !(a == b);

        }

    }

}