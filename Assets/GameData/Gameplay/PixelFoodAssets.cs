using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // Small code-drawn placeholders. Resource-pack PNGs always take priority.
    public static class PixelFoodAssets
    {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static Texture2D Texture(string name)
        {
            if (string.IsNullOrEmpty(name) || !(name == "cup" || name == "stew" || name.StartsWith("crop_stage_") || name.StartsWith("sapling_stage_")))
                return null;
            if (textures.TryGetValue(name, out var cached) && cached != null)
                return cached;
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var p = new Color32[256];
            Color32 green = new Color32(77, 145, 58, 255), light = new Color32(134, 189, 76, 255), stem = new Color32(95, 105, 39, 255);
            if (name == "cup" || name == "stew")
            {
                for (int y = 2; y <= 10; y++)
                    for (int x = 3; x <= 12; x++)
                        if (y > 3 || x > 4 && x < 11)
                            p[x + y * 16] = new Color32((byte)(x < 5 ? 115 : 178), (byte)(x < 5 ? 73 : 121), (byte)(x < 5 ? 48 : 73), 255);
                for (int x = 3; x <= 12; x++)
                    p[x + 11 * 16] = new Color32(213, 160, 103, 255);
                for (int y = 5; y < 10; y++)
                    for (int x = 13; x <= 14; x++)
                        if (x == 14 || y == 5 || y == 9)
                            p[x + y * 16] = new Color32(155, 99, 61, 255);
                if (name == "stew")
                    for (int y = 9; y <= 11; y++)
                        for (int x = 4; x <= 11; x++)
                            p[x + y * 16] = (x + y) % 4 == 0 ? green : new Color32(217, 144, 56, 255);
            }
            else
            {
                int stage = 0;
                int.TryParse(name.Substring(name.LastIndexOf('_') + 1), out stage);
                stage = Mathf.Clamp(stage, 0, 3);
                int top = 4 + stage * 3;
                for (int y = 0; y < top; y++)
                {
                    p[7 + y * 16] = stem;
                    p[8 + y * 16] = green;
                }

                for (int y = 2; y < top; y += 3)
                    for (int x = 0; x < 3 + stage; x++)
                    {
                        int yy = Mathf.Min(15, y + x / 2);
                        p[7 - x + yy * 16] = green;
                        p[8 + x + yy * 16] = light;
                    }

                if (stage == 3 && !name.StartsWith("sapling"))
                    for (int y = top - 2; y <= top; y++)
                        for (int x = 5; x <= 10; x++)
                            if ((x + y) % 2 == 0)
                                p[x + y * 16] = new Color32(225, 192, 71, 255);
            }

            t.SetPixels32(p);
            t.Apply(false, false);
            textures[name] = t;
            return t;
        }

        public static Sprite Sprite(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (sprites.TryGetValue(name, out var s) && s != null && s.texture != null)
                return s;
            var t = Texture(name);
            if (t == null)
                return null;
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16);
            sprites[name] = s;
            return s;
        }
    }
}
