using System.Collections.Generic;
using Game.Visuals.PixelEffects;
using Game.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    // Trail pixels are owned by the world, so a pooled or embedded arrow can
    // disappear immediately while the last few pixels finish fading in place.
    public sealed class PixelArrowTrail : MonoBehaviour
    {
        private struct Pixel { public Vector2 Position; public float Born; public byte Shade; }
        private const int Capacity = 1024;
        private readonly List<Pixel> pixels = new List<Pixel>(Capacity);
        private PixelEffectMesh draw;
        private static PixelArrowTrail instance;
        private Game.World.World world;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            world = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            draw = new PixelEffectMesh("Pixel Arrow Streaks", transform, 99);
        }

        public static void Record(Vector2 start, Vector2 end, string layer, int order)
        {
            if ((end - start).sqrMagnitude < .0001f || (end - start).sqrMagnitude > 64f) return;
            if (instance == null) new GameObject("Pixel Arrow Trails").AddComponent<PixelArrowTrail>();
            if (instance.draw == null) return;
            instance.BindWorld();
            instance.draw.Renderer.sortingLayerName = string.IsNullOrEmpty(layer) ? "Default" : layer;
            instance.draw.Renderer.sortingOrder = order - 1;
            int steps = Mathf.Clamp(Mathf.CeilToInt((end - start).magnitude * 16), 1, 128);
            int replace = Mathf.Max(0, instance.pixels.Count + steps - Capacity);
            if (replace > 0) instance.pixels.RemoveRange(0, replace);
            for (int i = 0; i < steps; i++)
            {
                Vector2 p = start + (end - start) * (i / (float)steps);
                p.x = Mathf.Floor(p.x * 16) / 16; p.y = Mathf.Floor(p.y * 16) / 16;
                instance.pixels.Add(new Pixel { Position = p, Born = Time.time, Shade = (byte)(i % 3) });
            }
        }

        private void BindWorld()
        {
            var current = WorldManager.Instance != null ? WorldManager.Instance.GetWorld() : null;
            if (current == world) return;
            world = current; Clear();
        }

        private void LateUpdate()
        {
            if (instance != this || draw == null) return;
            BindWorld(); draw.Begin();
            for (int i = pixels.Count - 1; i >= 0; i--)
            {
                Pixel p = pixels[i];
                float age = Time.time - p.Born;
                if (age > .22f) { pixels.RemoveAt(i); continue; }
                float fade = Mathf.Ceil(Mathf.Clamp01(1 - age / .22f) * 6) / 6;
                draw.Quad(p.Position.x, p.Position.y, 1f / 16, 1f / 16,
                    new Color32((byte)(226 + p.Shade * 10), 244, 255, (byte)(fade * 185)));
            }
            draw.Upload();
        }

        private void Clear() { pixels.Clear(); if (draw != null) { draw.Begin(); draw.Upload(); } }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Clear(); }
        private void OnDisable() { Clear(); }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
            if (draw != null) draw.Dispose();
        }
    }
}
