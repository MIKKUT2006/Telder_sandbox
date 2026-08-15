using System;
using UnityEngine;

namespace Game.World.Lighting
{
    [Serializable]
    public class DayNightSettings
    {
        // ѕолный цикл в секундах.
        public float DayLength = 600f;

        // —колько времени занимает переход
        // день -> ночь и ночь -> день.
        public float TransitionLength = 90f;

        public Color DayColor =
            Color.white;

        public Color NightColor =
            new Color(
                0.08f,
                0.10f,
                0.20f
            );

        public float DayIntensity =
            1f;

        public float NightIntensity =
            0f;
    }
}