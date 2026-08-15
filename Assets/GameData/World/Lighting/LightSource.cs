using UnityEngine;

namespace Game.World.Lighting
{
    public struct LightSource
    {
        public bool Enabled;

        public float R;
        public float G;
        public float B;

        public float Intensity;

        public LightSource(
            Color color,
            float intensity
        )
        {
            Enabled =
                true;

            R =
                color.r;

            G =
                color.g;

            B =
                color.b;

            Intensity =
                intensity;
        }

        public LightValue ToLight()
        {
            if (!Enabled)
            {
                return LightValue.Black;
            }

            return new LightValue(
                R * Intensity,
                G * Intensity,
                B * Intensity,
                0f
            );
        }
    }
}