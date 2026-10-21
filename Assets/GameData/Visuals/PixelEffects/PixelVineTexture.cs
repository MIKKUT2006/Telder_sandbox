using UnityEngine;

namespace Game.Visuals.PixelEffects
{
    // A code-drawn fallback only. A resource-pack texture named vines takes priority.
    public static class PixelVineTexture
    {
        public static Texture2D Create()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.name = "Pixel Vines Fallback";
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[256];
            for (int y = 0; y < 16; y++)
            {
                int x = 7 + (y >= 4 && y < 12 ? 1 : 0);
                pixels[x + y * 16] = new Color32(72, 110, 38, 255);
                if (y % 4 != 1) continue;
                int side = (y / 4) % 2 == 0 ? -1 : 1;
                for (int leaf = 1; leaf <= 3; leaf++)
                {
                    int px = x + side * leaf;
                    pixels[px + y * 16] = new Color32(84, 146, 45, 255);
                    pixels[px + (y + 1) * 16] = new Color32(111, 166, 57, 255);
                }
            }
            texture.SetPixels32(pixels); texture.Apply(false, false);
            return texture;
        }
    }
}
