using System;
using UnityEngine;
using Game.World.Dimensions;

namespace Game.World.Parallax
{
    public sealed class DimensionAtmosphereProfile
    {
        public Color CloudColor;
        public Color StormCloudColor;

        public static DimensionAtmosphereProfile Create()
        {
            DimensionDefinition d = DimensionTravelRuntime.Current;
            int seed = d != null ? d.Seed : 1;
            string type = d != null && d.Type != null ? d.Type.Id : string.Empty;

            float a = Hash01(seed, 0x3117);
            float b = Hash01(seed, 0x9213);
            float c = Hash01(seed, 0x4821);

            float h;
            float s;
            float v;

            if (string.Equals(type, "distorted", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Lerp(0.57f, 0.76f, a);
                s = Mathf.Lerp(0.28f, 0.58f, b);
                v = Mathf.Lerp(0.62f, 0.92f, c);
            }
            else if (string.Equals(type, "volcanic", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Repeat(Mathf.Lerp(-0.04f, 0.07f, a), 1f);
                s = Mathf.Lerp(0.10f, 0.32f, b);
                v = Mathf.Lerp(0.48f, 0.72f, c);
            }
            else if (string.Equals(type, "frozen", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Lerp(0.51f, 0.61f, a);
                s = Mathf.Lerp(0.08f, 0.28f, b);
                v = Mathf.Lerp(0.78f, 1.0f, c);
            }
            else if (string.Equals(type, "toxic", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Lerp(0.20f, 0.37f, a);
                s = Mathf.Lerp(0.24f, 0.54f, b);
                v = Mathf.Lerp(0.58f, 0.86f, c);
            }
            else if (string.Equals(type, "cosmic", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Lerp(0.60f, 0.88f, a);
                s = Mathf.Lerp(0.20f, 0.48f, b);
                v = Mathf.Lerp(0.60f, 0.90f, c);
            }
            else if (string.Equals(type, "alien", StringComparison.OrdinalIgnoreCase))
            {
                h = Mathf.Lerp(0.34f, 0.72f, a);
                s = Mathf.Lerp(0.18f, 0.52f, b);
                v = Mathf.Lerp(0.62f, 0.94f, c);
            }
            else
            {
                h = a;
                s = Mathf.Lerp(0.08f, 0.36f, b);
                v = Mathf.Lerp(0.68f, 0.96f, c);
            }

            Color cloud = Color.HSVToRGB(h, s, v);
            cloud.a = 1f;

            Color storm = Color.Lerp(cloud * 0.42f, new Color(0.11f, 0.13f, 0.18f, 1f), 0.46f);
            storm.a = 1f;

            DimensionAtmosphereProfile result = new DimensionAtmosphereProfile();
            result.CloudColor = cloud;
            result.StormCloudColor = storm;
            return result;
        }

        private static float Hash01(int seed, int salt)
        {
            unchecked
            {
                uint x = (uint)(seed ^ salt);
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return (x & 0x00FFFFFFu) / 16777215f;
            }
        }
    }
}
