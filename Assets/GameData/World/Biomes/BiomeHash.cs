namespace Game.World.Biomes
{
    internal static class BiomeHash
    {
        public static int Hash(
            int seed,
            int x,
            int salt
        )
        {
            unchecked
            {
                uint h = (uint)seed;

                h ^=
                    (uint)x +
                    0x9E3779B9u +
                    (h << 6) +
                    (h >> 2);

                h ^=
                    (uint)salt +
                    0x85EBCA6Bu +
                    (h << 6) +
                    (h >> 2);

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;

                return
                    (int)(
                        h &
                        0x7FFFFFFF
                    );
            }
        }


        public static int HashString(
            int seed,
            string value,
            int salt
        )
        {
            unchecked
            {
                uint h =
                    (uint)(
                        seed ^
                        salt
                    );

                if (value != null)
                {
                    for (
                        int i = 0;
                        i < value.Length;
                        i++
                    )
                    {
                        h ^=
                            char.ToUpperInvariant(
                                value[i]
                            );

                        h *=
                            16777619u;
                    }
                }

                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;

                return
                    (int)(
                        h &
                        0x7FFFFFFF
                    );
            }
        }


        public static float To01(
            int hash
        )
        {
            return
                (
                    hash &
                    0x7FFFFFFF
                )
                /
                2147483647f;
        }
    }
}