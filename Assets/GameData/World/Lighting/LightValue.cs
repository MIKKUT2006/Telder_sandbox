using UnityEngine;

namespace Game.World.Lighting
{
    public struct LightValue
    {
        public float R;
        public float G;
        public float B;

        // Солнечная составляющая.
        // 0 = нет солнечного света.
        // 1 = полный солнечный свет.
        public float Sun;

        public LightValue(
            float r,
            float g,
            float b,
            float sun
        )
        {
            R = r;
            G = g;
            B = b;
            Sun = sun;
        }

        public static LightValue Black
        {
            get
            {
                return new LightValue(
                    0f,
                    0f,
                    0f,
                    0f
                );
            }
        }

        public Color ToColor(
            float sunIntensity,
            Color sunColor
        )
        {
            float r =
                Mathf.Clamp01(
                    R +
                    sunColor.r *
                    Sun *
                    sunIntensity
                );

            float g =
                Mathf.Clamp01(
                    G +
                    sunColor.g *
                    Sun *
                    sunIntensity
                );

            float b =
                Mathf.Clamp01(
                    B +
                    sunColor.b *
                    Sun *
                    sunIntensity
                );

            return new Color(
                r,
                g,
                b,
                1f
            );
        }
    }
}