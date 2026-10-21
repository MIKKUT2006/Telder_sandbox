using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Content;
using Game.Resources;

namespace Game.Entities.Boss
{
    /// <summary>
    /// Ground burst that is triggered ONCE at the Slam impact frame (after the
    /// Slam animation). Each particle is a SpriteRenderer, so it is visible
    /// with the same 2D renderer as block fragments and supports custom PNGs.
    /// Particles are reused instead of instantiated for every attack.
    /// </summary>
    public sealed class BossSlamGroundParticles : MonoBehaviour
    {
        private const int PoolSize = 160;
        private struct Shard
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Spin;
            public float Age;
            public float Lifetime;
            public bool Active;
        }

        private static BossSlamGroundParticles instance;
        private readonly Shard[] shards = new Shard[PoolSize];
        private readonly Dictionary<Texture2D, Sprite> sprites = new Dictionary<Texture2D, Sprite>();
        private readonly Dictionary<string, Texture2D> loadedFiles =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private int cursor;
        private Sprite fallbackSprite;
        private Texture2D fallbackTexture;
        private string lastMissingTexture;

        public static void Emit(Vector2 atFeet, EntityBossDefinition settings)
        {
            if (settings == null || settings.SlamParticleCount <= 0) return;
            if (instance == null)
            {
                GameObject go = new GameObject("[Runtime] Ancient golem ground shards");
                instance = go.AddComponent<BossSlamGroundParticles>();
            }
            instance.Burst(atFeet, settings);
        }

        /// <summary>
        /// Loads a custom PNG or a block texture through the same resource resolver
        /// used by the ground particles. Returns null on failure, allowing the
        /// shockwave to keep its original generated sprite.
        /// </summary>
        public static Sprite TryGetConfiguredSprite(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName)) return null;
            if (instance == null)
            {
                GameObject go = new GameObject("[Runtime] Ancient golem ground shards");
                instance = go.AddComponent<BossSlamGroundParticles>();
            }
            return instance.ResolveSprite(textureName, false);
        }

        private void Burst(Vector2 feet, EntityBossDefinition settings)
        {
            Sprite sprite = ResolveSprite(settings.SlamParticleTexture);
            float velocity = Mathf.Max(0.1f, settings.SlamParticleSpeed);
            float size = Mathf.Clamp(settings.SlamParticleSize, .02f, 1f);
            int count = Mathf.Clamp(settings.SlamParticleCount, 0, PoolSize);

            for (int i = 0; i < count; i++)
            {
                int index = cursor++ % PoolSize;
                Shard shard = shards[index];
                if (shard.Renderer == null)
                {
                    GameObject go = new GameObject("Golem ground particle");
                    go.transform.SetParent(transform, false);
                    shard.Renderer = go.AddComponent<SpriteRenderer>();
                    shard.Renderer.sortingLayerName = "Default";
                    shard.Renderer.sortingOrder = 8100;
                }

                shard.Renderer.sprite = sprite;
                shard.Renderer.color = Color.white;
                shard.Renderer.enabled = true;
                shard.Age = 0f;
                shard.Lifetime = UnityEngine.Random.Range(.48f, 1.05f);
                shard.Position = feet + new Vector2(UnityEngine.Random.Range(-.85f, .85f),
                    UnityEngine.Random.Range(.04f, .28f));
                shard.Velocity = new Vector2(UnityEngine.Random.Range(-velocity, velocity),
                    UnityEngine.Random.Range(velocity * .45f, velocity * 1.2f));
                shard.Spin = UnityEngine.Random.Range(-280f, 280f);
                shard.Active = true;
                shard.Renderer.transform.localScale = Vector3.one *
                    (size * UnityEngine.Random.Range(.7f, 1.3f));
                shard.Renderer.transform.rotation = Quaternion.Euler(0f, 0f,
                    UnityEngine.Random.Range(0f, 360f));
                shard.Renderer.transform.position =
                    new Vector3(shard.Position.x, shard.Position.y, -.29f);
                shards[index] = shard;
            }
        }

        private Sprite ResolveSprite(string requested, bool useFallback = true)
        {
            if (!string.IsNullOrWhiteSpace(requested))
            {
                string key = requested.Trim().Replace('\\', '/');
                // Explicit paths always win over same-named block textures.
                bool isExplicitPath = key.Contains("/") ||
                    key.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
                if (isExplicitPath)
                {
                    Sprite direct = LoadFileSprite(key);
                    if (direct != null) return direct;
                }
                if (TextureManager.TryGet(key, out Texture2D blockTexture) && blockTexture != null)
                    return SpriteFromTexture(blockTexture);

                string resourceName = key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? key.Substring(0, key.Length - 4) : key;
                Sprite resourceSprite = UnityEngine.Resources.Load<Sprite>(resourceName);
                if (resourceSprite != null) return resourceSprite;
                Texture2D resourceTexture = UnityEngine.Resources.Load<Texture2D>(resourceName);
                if (resourceTexture != null) return SpriteFromTexture(resourceTexture);

                if (!isExplicitPath)
                {
                    Sprite fileSprite = LoadFileSprite(key);
                    if (fileSprite != null) return fileSprite;
                }

                if (lastMissingTexture != key)
                {
                    lastMissingTexture = key;
                    Debug.LogWarning("GOLEM SLAM: particle texture not found: " + key +
                        ". Use a block texture name or a path relative to Assets/GameData.");
                }
            }
            // Guaranteed visible fallback even when resource packs are absent.
            return useFallback ? MakeFallbackSprite() : null;
        }

        private Sprite LoadFileSprite(string key)
        {
            // Works in Editor and in builds with the GameData resource packs.
            string actualPath = ResolveFilePath(key);
            if (actualPath == null) return null;
            if (loadedFiles.TryGetValue(actualPath, out Texture2D existing) && existing != null)
                return SpriteFromTexture(existing);

            Texture2D texture = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = Path.GetFileNameWithoutExtension(actualPath);
                if (!texture.LoadImage(File.ReadAllBytes(actualPath), false))
                {
                    Destroy(texture);
                    return null;
                }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                loadedFiles[actualPath] = texture;
                return SpriteFromTexture(texture);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("GOLEM SLAM: could not load texture " +
                    actualPath + ": " + ex.Message);
                if (texture != null) Destroy(texture);
                return null;
            }
        }

        private static string ResolveFilePath(string requested)
        {
            // Paths in JSON are project-relative; do not allow walking outside
            // GameData. Also accept an unqualified texture name from a pack.
            string normalized = requested.Replace('\\', '/').TrimStart('/');
            if (Path.IsPathRooted(requested) || normalized.Contains("../") || normalized == "..")
                return null;
            if (!normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                normalized += ".png";

            string inGameData = Path.Combine(DataPaths.GameDataFolder,
                normalized.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(inGameData)) return inGameData;

            string basename = Path.GetFileName(normalized);
            string packRoot = Path.Combine(DataPaths.ResourcePacksFolder, "Default", "textures");
            string[] folders = { "particles", "blocks", "effects" };
            for (int i = 0; i < folders.Length; i++)
            {
                string found = Path.Combine(packRoot, folders[i], basename);
                if (File.Exists(found)) return found;
            }
            return null;
        }

        private Sprite SpriteFromTexture(Texture2D texture)
        {
            if (sprites.TryGetValue(texture, out Sprite sprite) && sprite != null)
                return sprite;
            sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), Mathf.Max(texture.width, texture.height),
                0, SpriteMeshType.FullRect);
            sprites[texture] = sprite;
            return sprite;
        }

        private Sprite MakeFallbackSprite()
        {
            if (fallbackSprite != null) return fallbackSprite;
            fallbackTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            fallbackTexture.filterMode = FilterMode.Point;
            fallbackTexture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (i % 4 == 0) ? new Color32(113, 104, 113, 255)
                    : new Color32(161, 143, 152, 255);
            fallbackTexture.SetPixels32(pixels);
            fallbackTexture.Apply();
            fallbackSprite = SpriteFromTexture(fallbackTexture);
            return fallbackSprite;
        }

        private void Update()
        {
            float delta = Mathf.Min(.05f, Time.deltaTime);
            for (int i = 0; i < PoolSize; i++)
            {
                Shard shard = shards[i];
                if (!shard.Active) continue;
                shard.Age += delta;
                if (shard.Age >= shard.Lifetime || shard.Renderer == null)
                {
                    shard.Active = false;
                    if (shard.Renderer != null) shard.Renderer.enabled = false;
                    shards[i] = shard;
                    continue;
                }
                shard.Velocity.y -= 13f * delta;
                shard.Position += shard.Velocity * delta;
                shard.Renderer.transform.position =
                    new Vector3(shard.Position.x, shard.Position.y, -.29f);
                shard.Renderer.transform.Rotate(0f, 0f, shard.Spin * delta);
                Color color = shard.Renderer.color;
                color.a = Mathf.Clamp01((shard.Lifetime - shard.Age) / .32f);
                shard.Renderer.color = color;
                shards[i] = shard;
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (Sprite sprite in sprites.Values)
                if (sprite != null) Destroy(sprite);
            sprites.Clear();
            foreach (Texture2D texture in loadedFiles.Values)
                if (texture != null) Destroy(texture);
            loadedFiles.Clear();
            if (fallbackTexture != null) Destroy(fallbackTexture);
        }
    }
}
