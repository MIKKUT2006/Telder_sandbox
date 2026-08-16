namespace Game.World.Lighting
{
    public struct LightNode
    {
        public byte Sun;
        public byte R;
        public byte G;
        public byte B;

        public LightNode(
            byte sun,
            byte r,
            byte g,
            byte b
        )
        {
            Sun = sun;
            R = r;
            G = g;
            B = b;
        }

        public bool IsEmpty
        {
            get
            {
                return
                    Sun == 0 &&
                    R == 0 &&
                    G == 0 &&
                    B == 0;
            }
        }

        public static LightNode None
        {
            get
            {
                return new LightNode(
                    0,
                    0,
                    0,
                    0
                );
            }
        }

        public static LightNode Sunlight
        {
            get
            {
                return new LightNode(
                    15,
                    0,
                    0,
                    0
                );
            }
        }
    }
}