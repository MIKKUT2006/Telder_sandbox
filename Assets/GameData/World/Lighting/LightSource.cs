namespace Game.World.Lighting
{
    public struct LightSource
    {
        public int X;
        public int Y;

        public byte R;
        public byte G;
        public byte B;

        public byte Sunlight;


        public LightSource(
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


        public bool HasRGB
        {
            get
            {
                return
                    R > 0 ||
                    G > 0 ||
                    B > 0;
            }
        }


        public bool HasSunlight
        {
            get
            {
                return Sunlight > 0;
            }
        }
    }
}