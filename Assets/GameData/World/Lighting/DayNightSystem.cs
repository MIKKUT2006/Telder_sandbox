using UnityEngine;

namespace Game.World.Lighting
{
    public class DayNightSystem
    {
        private readonly DayNightSettings settings;

        private float time;

        public float Time
        {
            get
            {
                return time;
            }
        }

        public float NormalizedTime
        {
            get
            {
                if (
                    settings.DayLength <= 0f
                )
                {
                    return 0f;
                }

                return Mathf.Repeat(
                    time /
                    settings.DayLength,
                    1f
                );
            }
        }

        public float SunIntensity
        {
            get
            {
                return CalculateSunIntensity();
            }
        }

        public Color SunColor
        {
            get
            {
                return CalculateSunColor();
            }
        }

        public DayNightSystem(
            DayNightSettings settings
        )
        {
            this.settings =
                settings;

            time = 0f;
        }

        public void Update(
            float deltaTime
        )
        {
            if (
                settings.DayLength <= 0f
            )
            {
                return;
            }

            time +=
                deltaTime;

            time =
                Mathf.Repeat(
                    time,
                    settings.DayLength
                );
        }

        public void SetTime(
            float normalizedTime
        )
        {
            normalizedTime =
                Mathf.Repeat(
                    normalizedTime,
                    1f
                );

            time =
                normalizedTime *
                settings.DayLength;
        }

        private float CalculateSunIntensity()
        {
            float t =
                NormalizedTime;

            /*
             * 0.00 = полночь
             * 0.25 = рассвет
             * 0.50 = полдень
             * 0.75 = закат
             */

            float sun =
                Mathf.Sin(
                    (t - 0.25f) *
                    Mathf.PI *
                    2f
                );

            sun =
                Mathf.Clamp01(
                    sun
                );

            /*
             * Плавная кривая.
             */
            sun =
                sun * sun *
                (3f - 2f * sun);

            return
                Mathf.Lerp(
                    settings.NightIntensity,
                    settings.DayIntensity,
                    sun
                );
        }

        private Color CalculateSunColor()
        {
            float t =
                NormalizedTime;

            float sun =
                Mathf.Sin(
                    (t - 0.25f) *
                    Mathf.PI *
                    2f
                );

            sun =
                Mathf.Clamp01(
                    sun
                );

            return
                Color.Lerp(
                    settings.NightColor,
                    settings.DayColor,
                    sun
                );
        }
    }
}