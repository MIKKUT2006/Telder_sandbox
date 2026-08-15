namespace Game.World.Lighting
{
    public struct LightNode
    {
        public int X;
        public int Y;

        public byte R;
        public byte G;
        public byte B;
        public byte Sunlight;


        public LightNode(
            int x,
            int y,
            byte r,
            byte g,
            byte b,
            byte sunlight
        )
        {
            X = x;
            Y = y;

            R = r;
            G = g;
            B = b;

            Sunlight = sunlight;
        }
    }
}