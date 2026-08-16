using System;

namespace Game.World.Lighting
{
    public class ChunkLightData
    {
        public const int Width = Chunk.SizeX;
        public const int Height = Chunk.SizeY;

        private readonly LightNode[,] lights;

        public ChunkLightData()
        {
            lights =
                new LightNode[
                    Width,
                    Height
                ];
        }

        public LightNode Get(
            int x,
            int y
        )
        {
            if (
                x < 0 ||
                x >= Width ||
                y < 0 ||
                y >= Height
            )
            {
                return LightNode.None;
            }

            return lights[x, y];
        }

        public void Set(
            int x,
            int y,
            LightNode value
        )
        {
            if (
                x < 0 ||
                x >= Width ||
                y < 0 ||
                y >= Height
            )
            {
                return;
            }

            lights[x, y] = value;
        }

        public byte GetSunlight(
            int x,
            int y
        )
        {
            return Get(x, y).Sun;
        }

        public void SetSunlight(
            int x,
            int y,
            byte value
        )
        {
            LightNode light =
                Get(x, y);

            light.Sun = value;

            Set(
                x,
                y,
                light
            );
        }

        public byte GetRed(
            int x,
            int y
        )
        {
            return Get(x, y).R;
        }

        public byte GetGreen(
            int x,
            int y
        )
        {
            return Get(x, y).G;
        }

        public byte GetBlue(
            int x,
            int y
        )
        {
            return Get(x, y).B;
        }

        public void Clear()
        {
            Array.Clear(
                lights,
                0,
                lights.Length
            );
        }
    }
}