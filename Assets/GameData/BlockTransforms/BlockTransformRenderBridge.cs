using UnityEngine;

namespace Game.BlockTransforms
{
    public static class BlockTransformRenderBridge
    {
        public static void ApplyToCell(
            Texture2D texture,
            int cellX,
            int cellY,
            int pixelSize,
            int worldX,
            int worldY)
        {
            if (texture == null || pixelSize <= 0)
                return;

            BlockTransformState state =
                BlockTransformRegistry.Get(worldX, worldY);

            if (state.Value == 0)
                return;

            int startX = cellX * pixelSize;
            int startY = cellY * pixelSize;

            if (startX < 0 || startY < 0 ||
                startX + pixelSize > texture.width ||
                startY + pixelSize > texture.height)
                return;

            Color[] source = texture.GetPixels(
                startX, startY, pixelSize, pixelSize);
            Color[] result = new Color[source.Length];
            int max = pixelSize - 1;

            for (int y = 0; y < pixelSize; y++)
            {
                for (int x = 0; x < pixelSize; x++)
                {
                    int sx;
                    int sy;

                    switch (state.Rotation)
                    {
                        default:
                        case 0: sx = x; sy = y; break;
                        case 1: sx = max - y; sy = x; break;
                        case 2: sx = max - x; sy = max - y; break;
                        case 3: sx = y; sy = max - x; break;
                    }

                    if (state.Mirrored)
                        sx = max - sx;

                    result[x + y * pixelSize] =
                        source[sx + sy * pixelSize];
                }
            }

            texture.SetPixels(
                startX, startY, pixelSize, pixelSize, result);
        }

        public static Vector2 TransformPoint01(
            Vector2 point,
            BlockTransformState state)
        {
            float x = point.x;
            float y = point.y;

            if (state.Mirrored)
                x = 1f - x;

            switch (state.Rotation)
            {
                case 1: return new Vector2(y, 1f - x);
                case 2: return new Vector2(1f - x, 1f - y);
                case 3: return new Vector2(1f - y, x);
                default: return new Vector2(x, y);
            }
        }
    }
}
