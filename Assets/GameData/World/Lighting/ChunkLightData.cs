namespace Game.World.Lighting
{
    public class ChunkLightData
    {
        private readonly int width;
        private readonly int height;

        private readonly LightValue[] values;

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public ChunkLightData(
            int width,
            int height
        )
        {
            this.width = width;
            this.height = height;

            values =
                new LightValue[
                    width *
                    height
                ];
        }

        public LightValue Get(
            int x,
            int y
        )
        {
            if (
                x < 0 ||
                y < 0 ||
                x >= width ||
                y >= height
            )
            {
                return LightValue.Black;
            }

            return values[
                y * width + x
            ];
        }

        public void Set(
            int x,
            int y,
            LightValue value
        )
        {
            if (
                x < 0 ||
                y < 0 ||
                x >= width ||
                y >= height
            )
            {
                return;
            }

            values[
                y * width + x
            ] = value;
        }

        public void Clear()
        {
            for (
                int i = 0;
                i < values.Length;
                i++
            )
            {
                values[i] =
                    LightValue.Black;
            }
        }
    }
}