using System.Collections.Generic;
using UnityEngine;
using Game.World.Effects;

namespace Game.World.Explosions
{
    /// <summary>
    /// Pooled pixel burst for explosions. Uses SpriteRenderer particles rather
    /// than one Rigidbody2D/GameObject physics body per pixel.
    /// </summary>
    [DefaultExecutionOrder(5100)]
    public sealed class ExplosionPixelVfx : MonoBehaviour
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
            public float StartScale;
        }

        private static ExplosionPixelVfx instance;
        private static Sprite pixelSprite;

        private readonly List<Particle> active =
            new List<Particle>(192);

        private readonly Stack<Particle> pool =
            new Stack<Particle>(192);

        private static readonly Color[] Palette =
        {
            new Color32(255, 242, 168, 255),
            new Color32(255, 194, 72, 255),
            new Color32(255, 125, 37, 255),
            new Color32(242, 58, 34, 255),
            new Color32(112, 72, 60, 255)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null)
                return;

            GameObject root =
                new GameObject("[Runtime] Explosion Pixel VFX");

            DontDestroyOnLoad(root);

            instance =
                root.AddComponent<ExplosionPixelVfx>();
        }

        public static void Spawn(Vector2 center, float radius)
        {
            EnsureInstance();

            if (instance == null)
                return;

            instance.SpawnInternal(
                center,
                Mathf.Max(0.5f, radius)
            );
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSprite();
        }

        private void Update()
        {
            float dt =
                Mathf.Min(Time.deltaTime, 0.05f);

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Particle p = active[i];

                p.Age += dt;

                if (p.Age >= p.Lifetime)
                {
                    ReleaseAt(i);
                    continue;
                }

                p.Velocity.y -= 4.5f * dt;

                Vector3 position =
                    p.Transform.position;

                position.x += p.Velocity.x * dt;
                position.y += p.Velocity.y * dt;

                p.Transform.position = position;

                p.Transform.Rotate(
                    0f,
                    0f,
                    p.AngularVelocity * dt
                );

                float t =
                    Mathf.Clamp01(p.Age / p.Lifetime);

                float scale =
                    p.StartScale *
                    Mathf.Lerp(1f, 0.25f, t);

                p.Transform.localScale =
                    new Vector3(scale, scale, 1f);

                Color color =
                    p.Renderer.color;

                color.a =
                    1f - Mathf.SmoothStep(0.58f, 1f, t);

                p.Renderer.color = color;
            }
        }

        private void SpawnInternal(Vector2 center, float radius)
        {
            int count =
                Mathf.Clamp(
                    Mathf.RoundToInt(24f + radius * 10f),
                    28,
                    96
                );

            float speedScale =
                Mathf.Lerp(
                    1.8f,
                    4.8f,
                    Mathf.Clamp01(radius / 8f)
                );

            for (int i = 0; i < count; i++)
            {
                float angle =
                    Random.Range(0f, Mathf.PI * 2f);

                Vector2 direction =
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle)
                    );

                // Slight upward bias looks more readable in a 2D sandbox.
                direction.y += Random.Range(0.05f, 0.42f);
                direction.Normalize();

                Particle p = Acquire();

                p.Age = 0f;
                p.Lifetime =
                    Random.Range(0.32f, 0.92f);

                p.StartScale =
                    Random.Range(0.65f, 1.65f);

                p.Velocity =
                    direction *
                    Random.Range(
                        speedScale * 0.65f,
                        speedScale * 1.25f
                    );

                p.AngularVelocity =
                    Random.Range(-520f, 520f);

                p.Transform.position =
                    center +
                    Random.insideUnitCircle *
                    Mathf.Min(0.28f, radius * 0.08f);

                p.Transform.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        Random.Range(0f, 360f)
                    );

                p.Transform.localScale =
                    Vector3.one * p.StartScale;

                Color color =
                    Palette[
                        Random.Range(0, Palette.Length)
                    ];

                p.Renderer.color = color;
                p.Renderer.sortingOrder =
                    WorldVfxSorting.ParticleSortingOrder;

                p.GameObject.SetActive(true);
                active.Add(p);
            }
        }

        private Particle Acquire()
        {
            EnsureSprite();

            if (pool.Count > 0)
                return pool.Pop();

            GameObject go =
                new GameObject("ExplosionPixel");

            go.transform.SetParent(
                transform,
                false
            );

            SpriteRenderer renderer =
                go.AddComponent<SpriteRenderer>();

            renderer.sprite = pixelSprite;
            renderer.sortingOrder =
                WorldVfxSorting.ParticleSortingOrder;

            return new Particle
            {
                GameObject = go,
                Transform = go.transform,
                Renderer = renderer
            };
        }

        private void ReleaseAt(int index)
        {
            Particle p = active[index];

            active.RemoveAt(index);

            p.GameObject.SetActive(false);
            pool.Push(p);
        }

        private static void EnsureSprite()
        {
            if (pixelSprite != null)
                return;

            Texture2D texture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name = "ExplosionPixelTexture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);

            // One source pixel = 1/16 world unit before localScale.
            pixelSprite =
                Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    16f
                );

            pixelSprite.name = "ExplosionPixelSprite";
        }
    }
}
