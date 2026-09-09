using UnityEngine;

namespace Game.World.Parallax
{
    public static class ParallaxWeatherState
    {
        private static float rainIntensity;

        public static float RainIntensity
        {
            get { return rainIntensity; }
            set { rainIntensity = Mathf.Clamp01(value); }
        }

        public static void SetRainIntensity(float value)
        {
            RainIntensity = value;
        }

        public static void SetRaining(bool raining)
        {
            RainIntensity = raining ? 1f : 0f;
        }
    }
}
