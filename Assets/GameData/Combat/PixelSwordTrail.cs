using System.Collections.Generic;
using Game.Visuals.PixelEffects;
using UnityEngine;

namespace Game.Combat
{
    [DefaultExecutionOrder(61000)]
    [DisallowMultipleComponent]
    public sealed class PixelSwordTrail : MonoBehaviour
    {
        [Range(8,24)] public int PixelsPerBlock = 16;
        [Range(.05f,.3f)] public float Lifetime = .15f;
        [Range(.1f,.85f)] public float InnerRadius = .48f;
        public bool TrailEnabled = true;

        private struct BladeSample
        {
            public Vector2 Inner, Outer;
            public float Born;
        }

        private readonly List<BladeSample> samples = new List<BladeSample>(24);
        private readonly Dictionary<long, byte> pixels = new Dictionary<long, byte>(2048);
        private PixelEffectMesh draw;
        private PlayerWeaponController player;
        private PlayerWeaponRigController rig;
        private SpriteRenderer bladeRenderer;
        private float lastPhase = -1f;
        private float lastRecord = -10f;

        private void Awake()
        {
            player = GetComponent<PlayerWeaponController>();
            rig = GetComponent<PlayerWeaponRigController>();
        }

        public static Vector2 SwingDirection(Vector2 aim, bool facingRight, float angle)
        {
            Vector2 canonical = new Vector2(Mathf.Abs(aim.x), aim.y).normalized;
            if (canonical.sqrMagnitude < .0001f) canonical = Vector2.right;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(
                canonical.x * Mathf.Cos(radians) - canonical.y * Mathf.Sin(radians),
                canonical.x * Mathf.Sin(radians) + canonical.y * Mathf.Cos(radians));
            direction.x *= facingRight ? 1f : -1f;
            return direction;
        }

        public static void Record(Transform owner, Vector2 origin, Vector2 direction,
            float reach, float phase, bool thrust, string layer = "Default", int order = 100)
        {
            if (owner == null) return;
            PixelSwordTrail trail = owner.GetComponent<PixelSwordTrail>();
            if (trail == null) trail = owner.gameObject.AddComponent<PixelSwordTrail>();
            trail.AddSample(origin, direction, reach, phase, thrust, layer, order);
        }

        private void AddSample(Vector2 origin, Vector2 direction, float reach,
            float phase, bool thrust, string layer, int order)
        {
            if (!TrailEnabled || !MeleeAttackMotion.IsStrike(phase)) return;
            if (draw == null) draw = new PixelEffectMesh("White Pixel Sword Trail", null, order);
            draw.Renderer.sortingLayerName = string.IsNullOrEmpty(layer) ? "Default" : layer;
            draw.Renderer.sortingOrder = order;
            Vector2 tip = origin + direction * Mathf.Clamp(reach, .25f, 6f);
            if (phase + .08f < lastPhase || Time.time - lastRecord > .16f ||
                (samples.Count > 0 && (tip - samples[samples.Count - 1].Outer).sqrMagnitude > 16f))
                samples.Clear();
            if (Time.time == lastRecord) return;

            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * .065f;
            BladeSample sample = new BladeSample
            {
                Inner = thrust ? tip - perpendicular : origin + direction * reach * InnerRadius,
                Outer = thrust ? tip + perpendicular : tip,
                Born = Time.time
            };
            samples.Add(sample);
            if (samples.Count > 24) samples.RemoveAt(0);
            lastRecord = Time.time;
            lastPhase = phase;
        }

        private void LateUpdate()
        {
            if (player != null && TrailEnabled &&
                player.TryGetArmPoseRequest(out PlayerWeaponPoseRequest pose) &&
                pose.Kind == WeaponKind.Sword && pose.Weapon != null)
            {
                if (rig == null) rig = GetComponent<PlayerWeaponRigController>();
                Vector2 origin = rig != null ? rig.GetFrontShoulderWorldPosition() : (Vector2)transform.position;
                float phase = Mathf.Clamp01(pose.Phase);
                float smooth = phase * phase * (3f - 2f * phase);
                bool thrust = pose.SwordAttack == SwordAttackKind.Thrust;
                Vector2 direction = thrust ? pose.AimDirection.normalized :
                    SwingDirection(pose.AimDirection, pose.FacingRight,
                        MeleeAttackMotion.SwingAngle(pose.Weapon, phase));
                float reach = pose.Weapon.SwingReach;
                if (thrust)
                {
                    Transform handPoint = rig != null ? rig.GetRuntimeHandPoint() : null;
                    if (handPoint != null)
                    {
                        origin = handPoint.position;
                        reach = pose.Weapon.Range;
                    }
                    else
                    {
                        reach = pose.Weapon.Range + MeleeAttackMotion.Thrust(phase) * pose.Weapon.ThrustDistance;
                    }
                }
                if (bladeRenderer == null && rig != null)
                {
                    Transform hand = rig.GetRuntimeHandPoint();
                    if (hand != null) bladeRenderer = hand.GetComponentInChildren<SpriteRenderer>(true);
                }
                string layer = bladeRenderer != null ? bladeRenderer.sortingLayerName : "Default";
                int order = bladeRenderer != null ? bladeRenderer.sortingOrder - 1 : 3;
                AddSample(origin, direction, reach, phase, thrust, layer, order);
            }

            if (draw == null) return;
            if (!TrailEnabled) samples.Clear();
            while (samples.Count > 0 && Time.time - samples[0].Born > Lifetime)
                samples.RemoveAt(0);
            pixels.Clear();
            draw.Begin();
            if (samples.Count > 1)
            {
                for (int i = 1; i < samples.Count; i++)
                {
                    BladeSample a = samples[i - 1], b = samples[i];
                    float alphaA = Fade(a.Born), alphaB = Fade(b.Born);
                    RasterTriangle(a.Inner, a.Outer, b.Outer, alphaA * .18f, alphaA, alphaB);
                    RasterTriangle(a.Inner, b.Outer, b.Inner, alphaA * .18f, alphaB, alphaB * .18f);
                }
            }
            float size = 1f / Mathf.Clamp(PixelsPerBlock, 8, 24);
            foreach (KeyValuePair<long, byte> pixel in pixels)
            {
                int x = (int)(pixel.Key >> 32);
                int y = unchecked((int)(uint)pixel.Key);
                draw.Quad(x * size, y * size, size, size, new Color32(255,255,255,pixel.Value));
            }
            draw.Upload();
        }

        private float Fade(float born)
        {
            float fade = Mathf.Clamp01(1f - (Time.time - born) / Mathf.Max(.02f, Lifetime));
            return Mathf.Ceil(fade * 6f) / 6f * .92f;
        }

        private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }

        private void RasterTriangle(Vector2 a, Vector2 b, Vector2 c, float aa, float ab, float ac)
        {
            float area = Cross(b - a, c - a);
            if (Mathf.Abs(area) < .000001f) return;
            int ppb = Mathf.Clamp(PixelsPerBlock, 8, 24);
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x,c.x)) * ppb);
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x,c.x)) * ppb);
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y,c.y)) * ppb);
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y,c.y)) * ppb);
            // Guard pathological authored ranges and teleports before rasterization.
            if (maxX - minX > ppb * 12 || maxY - minY > ppb * 12) return;
            for (int y = minY; y < maxY; y++)
                for (int x = minX; x < maxX; x++)
                {
                    Vector2 point = new Vector2((x + .5f) / ppb, (y + .5f) / ppb);
                    float wb = Cross(point - a, c - a) / area;
                    float wc = Cross(b - a, point - a) / area;
                    float wa = 1f - wb - wc;
                    if (wa < 0f || wb < 0f || wc < 0f) continue;
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt((wa * aa + wb * ab + wc * ac) * 255f),0,255);
                    long key = ((long)x << 32) ^ (uint)y;
                    if (pixels.TryGetValue(key, out byte old)) pixels[key] = (byte)Mathf.Max(old,alpha);
                    else if (pixels.Count < 4096) pixels.Add(key,alpha);
                }
        }

        private void OnDisable()
        {
            samples.Clear();
            lastPhase = -1f;
            if (draw != null) { draw.Begin(); draw.Upload(); }
        }

        private void OnDestroy() { if (draw != null) draw.Dispose(); }
    }
}

