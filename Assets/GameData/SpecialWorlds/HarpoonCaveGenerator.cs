using System;
using System.Collections.Generic;
using System.IO;
using Game.Content;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.SpecialWorlds
{
    /// <summary>
    /// Infinite bright corridor generated in 32-column segments. Only this
    /// challenge owns the Tilemaps; the regular World/WorldManager is untouched.
    /// </summary>
    public sealed class HarpoonCaveGenerator : MonoBehaviour
    {
        private const int Width = 32;
        private const int MinY = -25;
        private const int MaxY = 25;
        private const int Ahead = 2;
        private const int Behind = 1;

        private static readonly string[][] Themes =
        {
            new [] { "stone", "cobblestone", "stone_brick" },
            new [] { "Leaves", "leaves_2", "green_stone" },
            new [] { "snow_brick", "snow_cobblestone", "ice_horn" },
            new [] { "red_stone", "coal_block", "stone" },
            new [] { "crystal", "green_stone", "stone_brick" },
            new [] { "Yellow_leaves_2", "Leaves", "oak_wood_planks" }
        };
        private readonly Dictionary<int, GameObject> segments = new Dictionary<int, GameObject>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>(StringComparer.OrdinalIgnoreCase);
        private readonly List<int> remove = new List<int>();
        private int seed;
        private int timeThemeShift;
        private float nextThemeSwitch;
        public int Seed { get { return seed; } }
        public string CurrentTheme { get { return Themes[Mathf.Abs(timeThemeShift % Themes.Length)][0]; } }

        public void ResetCourse(int newSeed)
        {
            foreach (var pair in segments) if (pair.Value != null) Destroy(pair.Value);
            segments.Clear();
            seed = newSeed;
            timeThemeShift = 0;
            nextThemeSwitch = Time.time + 70f;
            Stream(0);
        }

        public void Stream(float playerX)
        {
            if (Time.time >= nextThemeSwitch)
            {
                timeThemeShift++;
                nextThemeSwitch = Time.time + 70f;
                // Existing colliders stay in place; upcoming segments take the
                // new palette without rebuilding the corridor under the player.
            }
            int current = Mathf.FloorToInt(playerX / Width);
            for (int i = current - Behind; i <= current + Ahead; i++)
                if (!segments.ContainsKey(i)) BuildSegment(i);
            remove.Clear();
            foreach (var pair in segments)
                if (pair.Key < current - Behind || pair.Key > current + Ahead)
                    remove.Add(pair.Key);
            foreach (int i in remove)
            {
                if (segments[i] != null) Destroy(segments[i]);
                segments.Remove(i);
            }
        }

        private int MaterialIndex(int worldX)
        {
            int index = Mathf.FloorToInt(Mathf.Max(0, worldX) / 118f) + timeThemeShift;
            return ((index % Themes.Length) + Themes.Length) % Themes.Length;
        }

        private float Noise(float x, float scale, float offset)
        {
            return Mathf.PerlinNoise(x * scale + seed * .0017f + offset,
                seed * .0023f + offset * .31f);
        }

        public int GetFloorHeight(int x)=>Floor(x);
        public int GetCeilingHeight(int x)=>Ceiling(x);

        private int Floor(int x)
        {
            if (x >= -8 && x < 22) return -4;
            float baseY = -4f + (Noise(x, .035f, 7f) - .5f) * 5f
                + (Noise(x, .12f, 12f) - .5f) * 1.4f;
            int phase = Mathf.FloorToInt((x + seed % 23) / 45f);
            float gapCenter = phase * 45f + 31f - seed % 23;
            float gapHalf = 3.3f + Noise(phase * 19, .5f, 4f) * 4.2f;
            if (x > 28 && Mathf.Abs(x - gapCenter) < gapHalf)
                return MinY - 1;
            return Mathf.RoundToInt(baseY);
        }

        private int Ceiling(int x)
        {
            if (x >= -8 && x < 22) return 13;
            float y = 12.5f + (Noise(x, .04f, 20f) - .5f) * 6f
                + (Noise(x, .13f, 23f) - .5f) * 2f;
            // Distinct tapered hanging stalactites of irregular widths/heights.
            int group = Mathf.FloorToInt((x + seed % 13) / 13f);
            float center = group * 13f + 6.5f - seed % 13 +
                (Noise(group, .7f, 11f) - .5f) * 4f;
            float radius = 1.8f + Noise(group, .56f, 35f) * 2.1f;
            float depth = 2.2f + Noise(group, .8f, 72f) * 5.5f;
            y -= Mathf.Max(0f, 1f - Mathf.Abs(x - center) / radius) * depth;
            return Mathf.Max(Floor(x) + 5, Mathf.RoundToInt(y));
        }

        private int Hash(int x, int y)
        {
            unchecked
            {
                int n = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                n ^= n >> 13; n *= 1274126177;
                return n & 0x7fffffff;
            }
        }

        private Tile TileFor(string name)
        {
            if (tiles.TryGetValue(name, out Tile tile)) return tile;
            Sprite sprite = GetSprite("blocks/" + name);
            if (sprite == null) sprite = GenerateFallback();
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            tiles.Add(name, tile);
            return tile;
        }

        public Sprite GetSprite(string path)
        {
            if (sprites.TryGetValue(path, out Sprite sprite)) return sprite;
            string full = Path.Combine(DataPaths.ResourcePacksFolder, "Default", "textures",
                path.Replace('/', Path.DirectorySeparatorChar) + ".png");
            if (!File.Exists(full)) return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(full))) { Destroy(tex); return null; }
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(.5f, .5f), 16f);
                sprites.Add(path, sprite);
                return sprite;
            }
            catch (Exception e) { Debug.LogWarning("SPECIAL HARPOON texture: " + e.Message); return null; }
        }

        private Sprite GenerateFallback()
        {
            const string key = "__fallback";
            if (sprites.TryGetValue(key, out Sprite sprite)) return sprite;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.SetPixels(new[] { Color.gray, Color.gray, Color.gray, Color.gray });
            t.filterMode = FilterMode.Point; t.Apply();
            sprite = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2f);
            sprites[key] = sprite;
            return sprite;
        }

        private Tilemap NewMap(string name, Transform root, int sorting, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var map = go.AddComponent<Tilemap>();
            map.color = tint;
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sorting;
            return map;
        }

        private void BuildSegment(int section)
        {
            var root = new GameObject("HarpoonCave_" + section);
            root.transform.SetParent(transform, false);
            var grid = root.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            Tilemap background = NewMap("BrightCaveBackground", root.transform, -15,
                new Color(.65f, .73f, .84f, .25f));
            Tilemap foreground = NewMap("CaveBlocks", root.transform, 3, Color.white);
            var foregroundCollider = foreground.gameObject.AddComponent<TilemapCollider2D>();
            var body = foreground.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = foreground.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            foregroundCollider.usedByComposite = true;
            int begin = section * Width;
            int height = MaxY - MinY + 1;
            var solids = new TileBase[Width * height];
            var backdrop = new TileBase[Width * height];
            for (int dx = 0; dx < Width; dx++)
            {
                int x = begin + dx;
                int floor = Floor(x);
                int ceiling = Ceiling(x);
                string[] palette = Themes[MaterialIndex(x)];
                for (int y = MinY; y <= MaxY; y++)
                {
                    int n = dx + (y - MinY) * Width;
                    int h = Hash(x, y);
                    if (y <= floor || y >= ceiling)
                    {
                        int textureIndex = h % 11 == 0 ? 1 : (h % 29 == 0 ? 2 : 0);
                        solids[n] = TileFor(palette[textureIndex]);
                    }
                    else
                    {
                        // Unlit and intentionally bright, including deep in the cave.
                        backdrop[n] = TileFor(palette[0]);
                    }
                }
            }
            var area = new BoundsInt(begin, MinY, 0, Width, height, 1);
            foreground.SetTilesBlock(area, solids);
            background.SetTilesBlock(area, backdrop);
            foregroundCollider.ProcessTilemapChanges();
            segments.Add(section, root);
        }

        private void OnDestroy()
        {
            foreach (var tile in tiles.Values) if (tile != null) Destroy(tile);
            foreach (var sprite in sprites.Values)
            {
                if (sprite == null) continue;
                Texture2D texture = sprite.texture;
                Destroy(sprite);
                if (texture != null) Destroy(texture);
            }
        }
    }
}
