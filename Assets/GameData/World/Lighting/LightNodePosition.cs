namespace Game.World.Lighting
{
    public struct LightNodePosition
    {
        public int X;
        public int Y;
        public LightNode Light;

        public LightNodePosition(
            int x,
            int y,
            LightNode light
        )
        {
            X = x;
            Y = y;
            Light = light;
        }
    }
}