using UnityEngine;
using UnityEngine.UI;

namespace Game.GameplaySystems
{
    internal static class RuntimeUI
    {
        private static Font cachedFont;

        public static readonly Color Brown = new Color32(187, 122, 87, 255);
        public static readonly Color Gray = new Color32(135, 135, 135, 255);

        public static Font GetFont()
        {
            if (cachedFont != null)
                return cachedFont;

            try { cachedFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (cachedFont == null)
            {
                try { cachedFont = Font.CreateDynamicFontFromOSFont(new [] { "Segoe UI", "Arial" }, 20); } catch { }
            }
            return cachedFont;
        }

        public static Text Text(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = GetFont();
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Brown;
            t.raycastTarget = false;
            return t;
        }

        public static Image Image(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            Image bg = go.GetComponent<Image>();
            bg.color = new Color32(250, 250, 250, 255);
            Button b = go.GetComponent<Button>();
            ColorBlock cb = b.colors;
            cb.normalColor = new Color32(250,250,250,255);
            cb.highlightedColor = new Color32(240,232,227,255);
            cb.pressedColor = new Color32(225,210,201,255);
            b.colors = cb;
            Text text = Text(go.transform, "Label", caption, 22, TextAnchor.MiddleCenter);
            RectTransform tr = (RectTransform)text.transform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            return b;
        }

        public static void AddBorder(RectTransform panel, float thickness = 4f)
        {
            AddEdge(panel, "Top", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-thickness), new Vector2(0,0), thickness, true);
            AddEdge(panel, "Bottom", new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,thickness), thickness, true);
            AddEdge(panel, "Left", new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(thickness,0), thickness, false);
            AddEdge(panel, "Right", new Vector2(1,0), new Vector2(1,1), new Vector2(-thickness,0), new Vector2(0,0), thickness, false);
        }

        private static void AddEdge(RectTransform p, string n, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax, float th, bool horizontal)
        {
            Image img = Image(p, n, Brown);
            RectTransform r = (RectTransform)img.transform;
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = omin; r.offsetMax = omax;
            if (horizontal) r.sizeDelta = new Vector2(0, th); else r.sizeDelta = new Vector2(th, 0);
            img.raycastTarget = false;
        }
    }
}
