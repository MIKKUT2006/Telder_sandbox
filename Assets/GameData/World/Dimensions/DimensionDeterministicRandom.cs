namespace Game.World.Dimensions
{
    /*
     * Свой маленький PRNG нужен для того, чтобы результат выбора
     * типа и модификаторов был одинаковым на любой машине и после
     * любого перезапуска игры.
     *
     * Не используем UnityEngine.Random для детерминированных свойств мира.
     */
    internal struct DimensionDeterministicRandom
    {
        private uint state;

        public DimensionDeterministicRandom(int seed, uint salt)
        {
            uint value =
                unchecked((uint)seed) ^
                salt ^
                0x9E3779B9u;

            if (value == 0)
                value = 0xA341316Cu;

            state = value;

            // Немного перемешиваем начальное состояние.
            NextUInt();
            NextUInt();
            NextUInt();
        }

        public uint NextUInt()
        {
            uint x = state;

            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;

            state = x == 0
                ? 0xA341316Cu
                : x;

            return state;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1)
                return 0;

            return (int)(
                NextUInt() %
                (uint)maxExclusive
            );
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            return minInclusive +
                NextInt(
                    maxExclusive -
                    minInclusive
                );
        }
    }
}