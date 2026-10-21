using UnityEngine;

namespace Game.Combat
{
    // Shared by damage, the arm rig and the pixel trail.
    public static class MeleeAttackMotion
    {
        public const float StrikeStart = .22f;
        public static bool IsStrike(float phase) { return phase >= StrikeStart && phase <= .78f; }

        public static float SwingAngle(WeaponMetadata weapon, float phase)
        {
            if (phase < StrikeStart)
                return Mathf.LerpAngle(weapon.SwordSwingStartAngle * .35f, weapon.SwordSwingStartAngle,
                    Smooth(phase / StrikeStart));
            return Mathf.LerpAngle(weapon.SwordSwingStartAngle, weapon.SwordSwingEndAngle,
                Smooth(Mathf.InverseLerp(StrikeStart, .72f, phase)));
        }

        public static float Thrust(float phase)
        {
            if (phase < StrikeStart) return -.15f * Smooth(phase / StrikeStart);
            if (phase < .48f) return Mathf.Lerp(-.15f, 1f, Smooth(Mathf.InverseLerp(StrikeStart, .48f, phase)));
            if (phase < .62f) return 1f;
            return 1f - Smooth(Mathf.InverseLerp(.62f, 1f, phase));
        }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    }
}
