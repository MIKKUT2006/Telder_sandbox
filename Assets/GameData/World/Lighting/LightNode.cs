using UnityEngine;

namespace Game.World.Lighting
{
    public struct LightNode
    {
        public int X;
        public int Y;

        public LightValue Light;

        public LightNode(
            int x,
            int y,
            LightValue light
        )
        {
            X = x;
            Y = y;
            Light = light;
        }

        public Vector2Int Position
        {
            get
            {
                return new Vector2Int(
                    X,
                    Y
                );
            }
        }
    }
}