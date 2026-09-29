using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Visual
{
    /// <summary>
    /// Lightweight pooled pixel hit effect for entity impacts. The effect is
    /// generated at runtime, so entity prefabs do not need ParticleSystems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityHitImpactEffect : MonoBehaviour
    {
        private sealed class Particle
        {
            public GameObject GameObject;
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float AngularVelocity;
            public float Age;
            public float Lifetime;
            public float StartSize;
            public Color StartColor;
        }

        private static EntityHitImpactEffect instance;
        private static Sprite pixelSprite;

        private readonly List<Particle> active = new List<Particle>(64);
        private readonly Stack<Particle> pool = new Stack<Particle>(64);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            pixelSprite = null;
        }

        public static void Spawn(
            Vector2 hitPoint,
            Vector2 attackDirection,
            EntityVisualDefinition visual)
        {
            if (visual == null || !visual.HitEffectEnabled)
                return;

            EnsureInstance();
            if (instance == null)
                return;

            instance.Emit(hitPoint, attackDirection, visual);
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("Entity Hit Impact FX");
            instance = go.AddComponent<EntityHitImpactEffect>();
        }

        private void Emit(
            Vector2 hitPoint,
            Vector2 attackDirection,
            EntityVisualDefinition visual)
        {
            EnsurePixelSprite();
            if (pixelSprite == null)
                return;

            Vector2 direction = attackDirection.sqrMagnitude > 0.0001f
                ? attackDirection.normalized
                : Vector2.right;

            Color primary = ParseColor(visual.HitEffectColor, new Color32(47, 200, 255, 255));
            Color secondary = ParseColor(visual.HitEffectSecondaryColor, new Color32(221, 249, 255, 255));

            int count = Mathf.Clamp(visual.HitParticleCount, 1, 24);
            float lifetime = Mathf.Max(0.03f, visual.HitParticleLifetime);
            float speed = Mathf.Max(0.1f, visual.HitParticleSpeed);
            float baseSize = Mathf.Max(0.01f, visual.HitParticleSize);
            float spread = Mathf.Clamp(visual.HitParticleSpread, 0f, 180f);

            for (int i = 0; i < count; i++)
            {
                Particle particle = GetParticle();

                float angle = Random.Range(-spread, spread);
                Vector2 flyDirection = Rotate(direction, angle);

                // A few fragments burst sideways too, making the contact point
                // readable even when the attack direction is nearly horizontal.
                if (i >= Mathf.CeilToInt(count * 0.7f))
                    flyDirection = Rotate(direction, Random.value < 0.5f ? 88f : -88f);

                float particleSpeed = speed * Random.Range(0.62f, 1.18f);
                float size = baseSize * Random.Range(0.72f, 1.28f);
                bool streak = i < Mathf.Max(2, count / 3);

                particle.Transform.position =
                    (Vector3)(hitPoint + Random.insideUnitCircle * baseSize * 0.35f);
                particle.Transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(flyDirection.y, flyDirection.x) * Mathf.Rad2Deg);
                particle.Transform.localScale = streak
                    ? new Vector3(size * 2.1f, size * 0.62f, 1f)
                    : new Vector3(size, size, 1f);

                particle.Renderer.sprite = pixelSprite;
                particle.Renderer.color = Random.value < 0.72f ? primary : secondary;
                particle.Renderer.sortingLayerName = string.IsNullOrWhiteSpace(visual.SortingLayer)
                    ? "Default"
                    : visual.SortingLayer;
                particle.Renderer.sortingOrder = visual.SortingOrder + 40;

                particle.Velocity = flyDirection * particleSpeed;
                particle.AngularVelocity = Random.Range(-240f, 240f);
                particle.Age = 0f;
                particle.Lifetime = lifetime * Random.Range(0.82f, 1.18f);
                particle.StartSize = size;
                particle.StartColor = particle.Renderer.color;
                particle.GameObject.SetActive(true);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Particle particle = active[i];
                if (particle == null || particle.GameObject == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                particle.Age += dt;
                float t = particle.Lifetime <= 0f
                    ? 1f
                    : Mathf.Clamp01(particle.Age / particle.Lifetime);

                if (t >= 1f)
                {
                    Recycle(i, particle);
                    continue;
                }

                particle.Transform.position += (Vector3)(particle.Velocity * dt);
                particle.Transform.Rotate(0f, 0f, particle.AngularVelocity * dt);

                // Quick burst then strong damping keeps the effect tight around hitPoint.
                particle.Velocity *= Mathf.Pow(0.075f, dt);

                float fade = 1f - t;
                Color color = particle.StartColor;
                color.a *= fade * fade;
                particle.Renderer.color = color;

                float scale = Mathf.Lerp(1f, 0.22f, t);
                Vector3 currentScale = particle.Transform.localScale;
                particle.Transform.localScale = new Vector3(
                    Mathf.Max(0.001f, currentScale.x * Mathf.Lerp(1f, 0.88f, t)),
                    Mathf.Max(0.001f, particle.StartSize * scale),
                    1f);
            }
        }

        private Particle GetParticle()
        {
            Particle particle;
            if (pool.Count > 0)
            {
                particle = pool.Pop();
            }
            else
            {
                GameObject go = new GameObject("Hit Pixel");
                go.transform.SetParent(transform, false);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();

                particle = new Particle
                {
                    GameObject = go,
                    Transform = go.transform,
                    Renderer = renderer
                };
            }

            active.Add(particle);
            return particle;
        }

        private void Recycle(int index, Particle particle)
        {
            active.RemoveAt(index);
            particle.GameObject.SetActive(false);
            pool.Push(particle);
        }

        private static void EnsurePixelSprite()
        {
            if (pixelSprite != null)
                return;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "EntityHitPixel";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            pixelSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            pixelSprite.name = "EntityHitPixel";
        }

        private static Color ParseColor(string value, Color fallback)
        {
            Color parsed;
            return !string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out parsed)
                ? parsed
                : fallback;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
