using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Effects
{
    public sealed class FoodEffectsHUD : MonoBehaviour
    {
        private GameObject root;
        private readonly GameObject[] cards = new GameObject[6];
        private readonly Text[] labels = new Text[6];
        private readonly ClockwiseEffectBorder[] borders = new ClockwiseEffectBorder[6];
        private PlayerFoodEffects owner;
        private readonly Texture2D[] textures = new Texture2D[6];
        private readonly Sprite[] icons = new Sprite[6];
        public static Color EffectColor(FoodEffectType type)
        {
            switch (type)
            {
                case FoodEffectType.Freeze:
                    return new Color(.55f, .87f, 1);
                case FoodEffectType.Poison:
                    return new Color(.5f, .85f, .18f);
                case FoodEffectType.Speed:
                    return new Color(1, .87f, .25f);
                case FoodEffectType.HighJump:
                    return new Color(.6f, 1, .7f);
                case FoodEffectType.Strength:
                    return new Color(1, .4f, .28f);
                default:
                    return new Color(.62f, .65f, 1);
            }
        }

        private void Start()
        {
            owner = GetComponent<PlayerFoodEffects>();
        }

        private static RectTransform Rect(GameObject go, Transform parent, Vector2 size, Vector2 pos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, 0);
            rt.pivot = new Vector2(.5f, 0);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private void Build()
        {
            root = new GameObject("Food effect HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            string[] glyphs =
            {
                "0010010000100100|0001100000011000|1111111111111111|0001100000011000|0010010000100100",
                "0000111111110000|0011111111111100|0011001100111100|0001111111111000|0000010110100000",
                "0000011100000000|0001111100000000|0011111111110000|0000001111000000|0000011100000000",
                "0000001100000000|0000111111000000|0011111111110000|0000001100000000|0000001100000000",
                "0001111000000000|0011111110000000|0011111111100000|0001111111100000|0000011111000000",
                "0011111111111100|0011000000001100|0011000000001100|0001110000111000|0000011111100000"
            };
            for (int i = 0; i < 6; i++)
            {
                cards[i] = new GameObject("Effect " + (FoodEffectType)i, typeof(RectTransform), typeof(Image));
                Rect(cards[i], root.transform, new Vector2(46, 60), Vector2.zero);
                cards[i].GetComponent<Image>().color = new Color(.06f, .07f, .1f, .9f);
                cards[i].GetComponent<Image>().raycastTarget = false;
                textures[i] = new Texture2D(16, 10, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                var pixels = new Color32[160];
                string[] lines = glyphs[i].Split('|');
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 16; x++)
                        if (lines[y][x] == '1')
                        {
                            pixels[x + (9 - y * 2) * 16] = EffectColor((FoodEffectType)i);
                            pixels[x + (8 - y * 2) * 16] = EffectColor((FoodEffectType)i);
                        }

                textures[i].SetPixels32(pixels);
                textures[i].Apply(false, false);
                icons[i] = Sprite.Create(textures[i], new Rect(0, 0, 16, 10), new Vector2(.5f, .5f), 16);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                Rect(icon, cards[i].transform, new Vector2(32, 25), new Vector2(0, 25));
                icon.GetComponent<Image>().sprite = icons[i];
                icon.GetComponent<Image>().raycastTarget = false;
                var text = new GameObject("Duration", typeof(RectTransform), typeof(Text));
                Rect(text, cards[i].transform, new Vector2(44, 22), new Vector2(0, 2));
                labels[i] = text.GetComponent<Text>();
                labels[i].font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labels[i].fontSize = 11;
                labels[i].alignment = TextAnchor.MiddleCenter;
                labels[i].color = Color.white;
                labels[i].raycastTarget = false;
                var border = new GameObject("Clockwise duration", typeof(RectTransform), typeof(ClockwiseEffectBorder));
                Rect(border, cards[i].transform, new Vector2(46, 60), Vector2.zero);
                borders[i] = border.GetComponent<ClockwiseEffectBorder>();
                borders[i].color = EffectColor((FoodEffectType)i);
                borders[i].raycastTarget = false;
            }
        }

        private void Update()
        {
            if (owner == null)
                return;
            if (!owner.IsLocalPlayer)
            {
                if (root != null)
                    root.SetActive(false);
                return;
            }

            if (root == null)
                Build();
            bool alive = GetComponent<Game.PlayerStats.PlayerStats>()?.IsDead != true;
            root.SetActive(alive && isActiveAndEnabled);
            int count = owner.State.Entries.Count, index = 0;
            for (int i = 0; i < 6; i++)
            {
                var e = owner.State.Entries.Find(a => (int)a.Type == i);
                cards[i].SetActive(e != null);
                if (e == null)
                    continue;
                ((RectTransform)cards[i].transform).anchoredPosition = new Vector2((index++ - (count - 1) * .5f) * 52, 104);
                float t = Mathf.Max(0, e.Remaining);
                labels[i].text = e.Level + " · " + (t >= 60 ? Mathf.CeilToInt(t / 60) + "м" : Mathf.CeilToInt(t) + "с");
                borders[i].Remaining = e.Total <= 0 ? 0 : t / e.Total;
                borders[i].SetVerticesDirty();
            }
        }

        private void OnDisable()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (root != null)
                Destroy(root);
            for (int i = 0; i < 6; i++)
            {
                if (icons[i] != null)
                    Destroy(icons[i]);
                if (textures[i] != null)
                    Destroy(textures[i]);
            }
        }
    }
}
