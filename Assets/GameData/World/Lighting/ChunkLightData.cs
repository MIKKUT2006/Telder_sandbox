using System;

namespace Game.World.Lighting
{
    public class ChunkLightData
    {
        public const int Width = Chunk.SizeX;
        public const int Height = Chunk.SizeY;

        private readonly LightNode[] lights;

        public bool IsDirty { get; private set; }

        public ChunkLightData()
        {
            lights = new LightNode[Width * Height];
            IsDirty = true;
        }

        private int Index(int x, int y)
        {
            return x + y * Width;
        }

        public LightNode Get(int x, int y)
        {
            if (x < 0 || x >= Width ||
                y < 0 || y >= Height)
            {
                return LightNode.None;
            }

            return lights[Index(x, y)];
        }

        public void Set(int x, int y, LightNode value)
        {
            if (x < 0 || x >= Width ||
                y < 0 || y >= Height)
            {
                return;
            }

            int index = Index(x, y);

            LightNode old = lights[index];

            if (old.Sun == value.Sun &&
                old.R == value.R &&
                old.G == value.G &&
                old.B == value.B)
            {
                return;
            }

            lights[index] = value;

            IsDirty = true;
        }

        public byte GetSunlight(int x, int y)
        {
            return Get(x, y).Sun;
        }

        public void SetSunlight(int x, int y, byte value)
        {
            LightNode node = Get(x, y);

            if (node.Sun == value)
                return;

            node.Sun = value;

            Set(x, y, node);
        }

        public byte GetRed(int x, int y)
        {
            return Get(x, y).R;
        }

        public byte GetGreen(int x, int y)
        {
            return Get(x, y).G;
        }

        public byte GetBlue(int x, int y)
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

            IsDirty = true;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void MarkClean()
        {
            IsDirty = false;
        }
    }
}