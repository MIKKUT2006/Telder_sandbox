using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Effects
{
    // Continuous rectangular perimeter: the removed part grows clockwise from noon.
    public sealed class ClockwiseEffectBorder : MaskableGraphic
    {
        public float Remaining = 1;
        public float Thickness = 3;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float w = r.width, h = r.height, perimeter = 2 * (w + h);
            if (w <= 0 || h <= 0)
                return;
            Vector2[] points =
            {
                new Vector2(r.center.x, r.yMax),
                new Vector2(r.xMax, r.yMax),
                new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMin, r.yMin),
                new Vector2(r.xMin, r.yMax),
                new Vector2(r.center.x, r.yMax)
            };
            float elapsed = (1 - Mathf.Clamp01(Remaining)) * perimeter, position = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                float length = Vector2.Distance(a, b);
                float skip = Mathf.Clamp(elapsed - position, 0, length);
                position += length;
                if (skip >= length)
                    continue;
                a = Vector2.Lerp(a, b, skip / length);
                Vector2 inward = i == 0 || i == 4 ? Vector2.down : i == 1 ? Vector2.left : i == 2 ? Vector2.up : Vector2.right;
                int n = vh.currentVertCount;
                vh.AddVert(a, color, Vector2.zero);
                vh.AddVert(b, color, Vector2.zero);
                vh.AddVert(b + inward * Thickness, color, Vector2.zero);
                vh.AddVert(a + inward * Thickness, color, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2);
                vh.AddTriangle(n, n + 2, n + 3);
            }
        }
    }
}
