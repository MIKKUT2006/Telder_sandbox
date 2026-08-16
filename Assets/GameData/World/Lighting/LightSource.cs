namespace Game.World.Lighting
{
    public class LightSource
    {
        public int X;
        public int Y;

        public byte R;
        public byte G;
        public byte B;

        public bool Enabled;

        public LightSource(
            int x,
            int y,
            byte r,
            byte g,
            byte b
        )
        {
            X = x;
            Y = y;

            R = r;
            G = g;
            B = b;

            Enabled = true;
        }

        public LightNode ToLight()
        {
            return new LightNode(
                0,
                R,
                G,
                B
            );
        }
    }
}