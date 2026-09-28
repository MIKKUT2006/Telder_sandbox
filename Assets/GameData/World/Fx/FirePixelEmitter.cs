using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Fx
{
    public sealed class FirePixelEmitter : MonoBehaviour
    {
        [System.Serializable]
        public struct Settings
        {
            public float SpawnRate;
            public Vector2 VelocityMin;
            public Vector2 VelocityMax;
            public Vector2 HorizontalRandom;
            public Vector2 LifetimeRange;
            public Vector2 SizeRange;
            public Vector2 SpawnArea;
            public float FadeIn;
            public float FadeOut;
            public bool MoreSparks;
        }

        private struct Particle
        {
            public bool Active;
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Lifetime;
            public float Age;
            public float Size;
            public int ColorIndex;
        }

        private static Sprite particleSprite;

        [SerializeField]
        private Settings settings;

        [SerializeField]
        private int poolSize = 32;

        [SerializeField]
        private bool playOnEnable = true;

        private readonly List<Particle> particles =
            new List<Particle>();

        private float spawnAccumulator;

        private static readonly Color[] Colors =
        {
            new Color32(255, 196, 72, 255),
            new Color32(255, 136, 38, 255),
            new Color32(255, 90, 32, 255),
            new Color32(245, 40, 32, 255),
            new Color32(255, 222, 120, 255)
        };

        public void Configure(Settings value)
        {
            settings = value;
        }

        private void Awake()
        {
            EnsureSprite();
            EnsurePool();
        }

        private void OnEnable()
        {
            spawnAccumulator = 0f;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            UpdateParticles(dt);

            if (!playOnEnable)
                return;

            spawnAccumulator += settings.SpawnRate * dt;

            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                SpawnOne();
            }
        }

        private void EnsurePool()
        {
            while (particles.Count < poolSize)
            {
                GameObject go = new GameObject(
                    "FirePixel"
                );

                go.transform.SetParent(
                    transform,
                    false
                );

                SpriteRenderer renderer =
                    go.AddComponent<SpriteRenderer>();

                renderer.sprite = particleSprite;
                renderer.sortingOrder = 500;
                renderer.sharedMaterial = null;

                particles.Add(
                    new Particle
                    {
                        Active = false,
                        Transform = go.transform,
                        Renderer = renderer
                    }
                );

                go.SetActive(false);
            }
        }

        private void SpawnOne()
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if (particles[i].Active)
                    continue;

                Particle p = particles[i];
                p.Active = true;
                p.Age = 0f;
                p.Lifetime = Random.Range(
                    settings.LifetimeRange.x,
                    settings.LifetimeRange.y
                );
                p.Size = Random.Range(
                    settings.SizeRange.x,
                    settings.SizeRange.y
                );

                p.ColorIndex = Random.Range(
                    0,
                    Colors.Length
                );

                float spawnX = Random.Range(
                    -settings.SpawnArea.x,
                    settings.SpawnArea.x
                );

                float spawnY = Random.Range(
                    -settings.SpawnArea.y,
                    settings.SpawnArea.y
                );

                p.Transform.localPosition = new Vector3(
                    spawnX,
                    spawnY,
                    0f
                );

                float vx = Random.Range(
                    settings.HorizontalRandom.x,
                    settings.HorizontalRandom.y
                );

                float vy = Random.Range(
                    settings.VelocityMin.y,
                    settings.VelocityMax.y
                );

                p.Velocity = new Vector2(vx, vy);

                p.Transform.localScale = Vector3.one * p.Size;
                p.Renderer.color = Colors[p.ColorIndex];
                p.Transform.gameObject.SetActive(true);

                particles[i] = p;
                return;
            }
        }

        private void UpdateParticles(float dt)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if (!particles[i].Active)
                    continue;

                Particle p = particles[i];
                p.Age += dt;

                if (p.Age >= p.Lifetime)
                {
                    p.Active = false;
                    p.Transform.gameObject.SetActive(false);
                    particles[i] = p;
                    continue;
                }

                Vector3 pos = p.Transform.localPosition;
                pos.x += p.Velocity.x * dt;
                pos.y += p.Velocity.y * dt;
                p.Transform.localPosition = pos;

                // slight upward acceleration
                p.Velocity.y += 0.18f * dt;

                float alpha = 1f;

                if (settings.FadeIn > 0f)
                    alpha *= Mathf.Clamp01(p.Age / settings.FadeIn);

                float remaining = p.Lifetime - p.Age;

                if (settings.FadeOut > 0f)
                    alpha *= Mathf.Clamp01(remaining / settings.FadeOut);

                Color color = Colors[p.ColorIndex];
                color.a = alpha;
                p.Renderer.color = color;

                particles[i] = p;
            }
        }

        private static void EnsureSprite()
        {
            if (particleSprite != null)
                return;

            Texture2D texture = new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false
            );

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            particleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
            particleSprite.name = "FirePixelSprite";
        }

        public static Settings CreateTorchSettings()
        {
            return new Settings
            {
                SpawnRate = 10f,
                VelocityMin = new Vector2(-0.015f, 0.42f),
                VelocityMax = new Vector2(0.015f, 0.72f),
                HorizontalRandom = new Vector2(-0.06f, 0.06f),
                LifetimeRange = new Vector2(0.33f, 0.65f),
                SizeRange = new Vector2(0.045f, 0.085f),
                SpawnArea = new Vector2(0.04f, 0.03f),
                FadeIn = 0.05f,
                FadeOut = 0.18f,
                MoreSparks = true
            };
        }

        public static Settings CreateCampfireSettings()
        {
            return new Settings
            {
                SpawnRate = 18f,
                VelocityMin = new Vector2(-0.03f, 0.36f),
                VelocityMax = new Vector2(0.03f, 0.88f),
                HorizontalRandom = new Vector2(-0.10f, 0.10f),
                LifetimeRange = new Vector2(0.38f, 0.80f),
                SizeRange = new Vector2(0.05f, 0.11f),
                SpawnArea = new Vector2(0.12f, 0.04f),
                FadeIn = 0.04f,
                FadeOut = 0.22f,
                MoreSparks = true
            };
        }
    }
}
