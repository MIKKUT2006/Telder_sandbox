using System;
using UnityEngine;

namespace Game.World.Lighting
{
    [Serializable]
    public class DayNightSettings
    {
        // ѕолный цикл в секундах.
        public float DayLength = 3600f;

        // —колько времени занимает переход
        // день -> ночь и ночь -> день.
        public float TransitionLength = 300f;

        public Color DayColor =
            Color.white;

        public Color NightColor =
            new Color(
                0.05f,
                0.05f,
                0.08f
            );

        public float DayIntensity =
            1f;

        public float NightIntensity =
            0f;
    }
}