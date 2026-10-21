using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Resources;
using Game.World.Rendering;

namespace Game.World.Effects
{
    /// <summary>Pixel-perfect sub-rectangles of the ACTUAL block texture.
    /// Sprites share the source texture and fragments use a bounded renderer pool.</summary>
    public sealed class BlockTextureFragments : MonoBehaviour
    {
        private const int Columns = 4;
        private const int Capacity = 256;
        private struct Fragment
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public float Spin;
            public bool Active;
            public Color Albedo;
            public Game.World.World World;
        }
        private static BlockTextureFragments instance;
        private readonly Dictionary<Texture2D, Sprite[]> cachedSprites = new Dictionary<Texture2D, Sprite[]>();
        private readonly Fragment[] fragments = new Fragment[Capacity];
        private int cursor;
        private int emittedFrame=-1,emittedThisFrame,prewarmIndex;
        private const int MaxEmittedPerFrame=48;
        public static void Prewarm(){Ensure();}
        private SpriteRenderer MakeRenderer()
        {
            var go=new GameObject("Block pixel shard");go.transform.SetParent(transform,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sortingLayerName="Default";renderer.sortingOrder=8000;renderer.enabled=false;return renderer;
        }
        private static BlockTextureFragments Ensure()
        {
            if (instance != null) return instance;
            GameObject go = new GameObject("[World] Block texture fragments");
            instance = go.AddComponent<BlockTextureFragments>();
            return instance;
        }
        public static bool Emit(Game.World.World world, int x, int y, ushort id, bool background, bool hit)
        {
            if (world == null || id == 0) return false;
            var block = world.GetBlockDefinition(id);
            if (block == null) return false;
            string key = BlockVisualResolver.ResolveTextureName(block, x, y,
                background ? BlockVisualLayer.Background : BlockVisualLayer.Foreground);
            if (string.IsNullOrEmpty(key) || !TextureManager.TryGet(key, out Texture2D texture) || texture == null)
                return false;
            Ensure().Burst(world, texture, x, y, hit, background);
            return true;
        }
        public static bool EmitFurniture(int x, int y, string id, bool hit)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            try
            {
                var contentId = Game.Content.ContentID.Parse(id);
                if (!Game.Content.BlockIDRegistry.Contains(contentId)) return false;
                ushort blockId = Game.Content.BlockIDRegistry.GetID(contentId);
                var manager = Game.World.WorldManager.Instance;
                return manager != null && Emit(manager.GetWorld(), x, y, blockId, false, hit);
            }
            catch (Exception) { return false; }
        }
        private Sprite[] GetFragments(Texture2D texture)
        {
            if (cachedSprites.TryGetValue(texture, out Sprite[] sprites)) return sprites;
            sprites = new Sprite[Columns * Columns];
            int w = Mathf.Max(1, texture.width / Columns);
            int h = Mathf.Max(1, texture.height / Columns);
            for (int j = 0; j < Columns; j++)
                for (int i = 0; i < Columns; i++)
                {
                    int startX = i * w, startY = j * h;
                    int cw = Mathf.Min(w, texture.width - startX);
                    int ch = Mathf.Min(h, texture.height - startY);
                    sprites[i + j * Columns] = Sprite.Create(texture,
                        new Rect(startX, startY, cw, ch), new Vector2(.5f, .5f),
                        Mathf.Max(1, texture.width), 0, SpriteMeshType.FullRect);
                }
            cachedSprites[texture] = sprites;
            return sprites;
        }
        private void Burst(Game.World.World world, Texture2D texture, int x, int y, bool hit, bool background)
        {
            if(emittedFrame!=Time.frameCount){emittedFrame=Time.frameCount;emittedThisFrame=0;}
            int count=Mathf.Min(hit?7:19,MaxEmittedPerFrame-emittedThisFrame);if(count<=0)return;
            emittedThisFrame+=count;Sprite[] sprites=GetFragments(texture);
            for (int i = 0; i < count; i++)
            {
                int index = cursor++ % Capacity;
                Fragment f = fragments[index];
                if (f.Renderer == null)
                {
                    f.Renderer=MakeRenderer();
                }
                f.Renderer.sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
                f.Renderer.enabled = true;
                f.Albedo = background ? new Color(.72f, .72f, .72f, 1f) : Color.white;
                f.World = world;
                f.Position = new Vector2(x + UnityEngine.Random.Range(.13f,.87f),
                    y + UnityEngine.Random.Range(.12f,.86f));
                f.Velocity = new Vector2(UnityEngine.Random.Range(hit ? -1.8f : -3.5f,
                    hit ? 1.8f : 3.5f), UnityEngine.Random.Range(.6f,hit ? 2.1f : 3.8f));
                f.Age = 0;
                f.Lifetime = UnityEngine.Random.Range(hit ? .32f : .65f, hit ? .55f : 1.15f);
                f.Spin = UnityEngine.Random.Range(-210f, 210f);
                f.Active = true;
                f.Renderer.transform.position = new Vector3(f.Position.x, f.Position.y, -.3f);
                f.Renderer.transform.rotation = Quaternion.identity;
                Color light = Game.World.Lighting.WorldVisualLightSampler.SampleParticle(world, f.Position);
                f.Renderer.color = new Color(f.Albedo.r * light.r, f.Albedo.g * light.g, f.Albedo.b * light.b, 1f);
                fragments[index] = f;
            }
        }
        private void Update()
        {
            // Amortize the renderer pool instead of instantiating it on a boss hit.
            int budget=8;
            while(budget-->0&&prewarmIndex<Capacity)
            {int i=prewarmIndex++;var slot=fragments[i];if(slot.Renderer==null){slot.Renderer=MakeRenderer();fragments[i]=slot;}}
            float dt = Mathf.Min(.05f, Time.deltaTime);
            for (int i = 0; i < Capacity; i++)
            {
                Fragment f = fragments[i];
                if (!f.Active) continue;
                f.Age += dt;
                if (f.Age >= f.Lifetime || f.Renderer == null)
                {
                    f.Active = false;
                    if (f.Renderer != null) f.Renderer.enabled = false;
                    fragments[i] = f;
                    continue;
                }
                f.Velocity.y -= 9f * dt;
                f.Position += f.Velocity * dt;
                f.Renderer.transform.position = new Vector3(f.Position.x, f.Position.y, -.3f);
                f.Renderer.transform.Rotate(0, 0, f.Spin * dt);
                Color light = Game.World.Lighting.WorldVisualLightSampler.SampleParticle(f.World, f.Position);
                Color c = new Color(f.Albedo.r * light.r, f.Albedo.g * light.g, f.Albedo.b * light.b, 1f);
                c.a = Mathf.Clamp01((f.Lifetime - f.Age) / Mathf.Min(.25f, f.Lifetime));
                f.Renderer.color = c;
                fragments[i] = f;
            }
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (var pair in cachedSprites)
                foreach (Sprite sprite in pair.Value)
                    if (sprite != null) Destroy(sprite);
        }
    }
}
