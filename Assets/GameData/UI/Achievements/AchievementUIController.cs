using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Achievements;
using Game.Inventory.UI;
using Game.UI.Pause;

namespace Game.UI.Achievements
{
    public sealed class AchievementUIController : MonoBehaviour
    {
        private static AchievementUIController instance;
        private GameObject panel;
        private RectTransform tree;
        private TMP_FontAsset font;
        private PauseMenuController pause;
        private readonly Dictionary<string, RectTransform> nodes = new Dictionary<string, RectTransform>(StringComparer.OrdinalIgnoreCase);

        public static void EnsureInstalled(PauseMenuController pause, GameObject pausePanel, TMP_FontAsset font)
        {
            if (pause == null || pausePanel == null) return;
            if (instance != null) return;

            GameObject go = new GameObject("AchievementUIRuntime");
            go.transform.SetParent(pause.transform, false);
            instance = go.AddComponent<AchievementUIController>();
            instance.pause = pause;
            instance.font = font;
            instance.BuildPanel(pausePanel.transform.parent as RectTransform);
            instance.AddPauseButton(pausePanel);
        }

        private void OnEnable() { AchievementRuntime.Unlocked += OnUnlocked; }
        private void OnDisable() { AchievementRuntime.Unlocked -= OnUnlocked; }

        private void BuildPanel(RectTransform parent)
        {
            Canvas canvas = pause.GetComponentInParent<Canvas>();
            if (parent == null && canvas != null) parent = canvas.transform as RectTransform;

            panel = new GameObject("AchievementsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent != null ? parent : pause.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.055f, 0.06f, 0.075f, 0.97f);

            GameObject title = Text("ДОСТИЖЕНИЯ", panel.transform, 34, TextAlignmentOptions.Top);
            RectTransform tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 1); tr.anchorMax = new Vector2(0.5f, 1); tr.sizeDelta = new Vector2(500, 60); tr.anchoredPosition = new Vector2(0, -26);

            GameObject scrollGo = new GameObject("TreeScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            RectTransform sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(.04f, .08f); sr.anchorMax = new Vector2(.96f, .88f); sr.offsetMin = sr.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, .18f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform vr = viewport.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one; vr.offsetMin = vr.offsetMax = Vector2.zero;

            GameObject content = new GameObject("Tree", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            tree = content.GetComponent<RectTransform>(); tree.anchorMin = new Vector2(0, 1); tree.anchorMax = new Vector2(0, 1); tree.pivot = new Vector2(0, 1); tree.sizeDelta = new Vector2(1800, 1000);

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = tree; scroll.horizontal = true; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject close = Button("НАЗАД", panel.transform, Close);
            RectTransform cr = close.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(1, 0); cr.anchorMax = new Vector2(1, 0); cr.pivot = new Vector2(1, 0); cr.sizeDelta = new Vector2(180, 44); cr.anchoredPosition = new Vector2(-34, 24);
            panel.SetActive(false);
        }

        private void AddPauseButton(GameObject pausePanel)
        {
            Button template = pausePanel.GetComponentInChildren<Button>(true);
            if (template == null) return;
            GameObject go = Instantiate(template.gameObject, template.transform.parent);
            go.name = "AchievementsButton";
            TMP_Text t = go.GetComponentInChildren<TMP_Text>(true); if (t != null) t.text = "Достижения";
            Button b = go.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(Open);
            go.transform.SetSiblingIndex(Mathf.Min(template.transform.GetSiblingIndex() + 1, go.transform.parent.childCount - 1));
        }

        public static bool IsOpen => instance != null && instance.panel != null && instance.panel.activeSelf;
        public static bool CloseIfOpen()
        {
            if (!IsOpen) return false;
            instance.Close();
            return true;
        }

        public void Open() { panel.SetActive(true); BuildTree(); }
        public void Close() { if (panel != null) panel.SetActive(false); }

        private void BuildTree()
        {
            for (int i = tree.childCount - 1; i >= 0; i--) Destroy(tree.GetChild(i).gameObject);
            nodes.Clear();

            List<AchievementDefinition> all = AchievementRegistry.GetAll().Where(x => x != null).OrderBy(x => x.SortOrder).ThenBy(x => x.ID.ToString()).ToList();
            if (all.Count == 0) return;

            Dictionary<string, int> depth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int maxD = 0;
            for (int i = 0; i < all.Count; i++) maxD = Mathf.Max(maxD, Depth(all[i], all, depth, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            int[] rows = new int[maxD + 1];

            for (int i = 0; i < all.Count; i++)
            {
                AchievementDefinition a = all[i]; string id = a.ID.ToString(); int d = depth[id]; int r = rows[d]++;
                RectTransform n = CreateNode(a); n.anchoredPosition = new Vector2(120 + d * 250, -100 - r * 150); nodes[id] = n;
            }

            int maxRows = 1; for (int i = 0; i < rows.Length; i++) maxRows = Mathf.Max(maxRows, rows[i]);
            tree.sizeDelta = new Vector2(Mathf.Max(1200, (maxD + 1) * 280 + 300), Mathf.Max(700, maxRows * 160 + 220));

            for (int i = 0; i < all.Count; i++)
                if (!string.IsNullOrWhiteSpace(all[i].Parent) && nodes.TryGetValue(all[i].Parent, out RectTransform p) && nodes.TryGetValue(all[i].ID.ToString(), out RectTransform c))
                    CreateLine(p.anchoredPosition, c.anchoredPosition);
        }

        private int Depth(AchievementDefinition a, List<AchievementDefinition> all, Dictionary<string, int> cache, HashSet<string> visiting)
        {
            string id = a.ID.ToString();
            if (cache.TryGetValue(id, out int v)) return v;
            if (!visiting.Add(id)) { cache[id] = 0; return 0; }
            if (string.IsNullOrWhiteSpace(a.Parent)) { cache[id] = 0; return 0; }
            AchievementDefinition p = all.FirstOrDefault(x => string.Equals(x.ID.ToString(), a.Parent, StringComparison.OrdinalIgnoreCase));
            int d = p == null ? 0 : Depth(p, all, cache, visiting) + 1;
            cache[id] = d; visiting.Remove(id); return d;
        }

        private RectTransform CreateNode(AchievementDefinition a)
        {
            bool unlocked = AchievementRuntime.Instance.IsUnlocked(a.ID.ToString());
            GameObject go = new GameObject(a.ID.ToString(), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(tree, false);
            RectTransform r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(210, 112);
            go.GetComponent<Image>().color = unlocked ? new Color(.28f, .18f, .12f, .98f) : new Color(.08f, .08f, .09f, .96f);

            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            RectTransform ir = icon.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0, .5f); ir.anchorMax = new Vector2(0, .5f); ir.sizeDelta = new Vector2(64, 64); ir.anchoredPosition = new Vector2(42, 0);
            Image im = icon.GetComponent<Image>(); im.sprite = ItemIconProvider.GetIcon(a.Icon); im.preserveAspect = true; im.color = unlocked ? Color.white : new Color(.22f, .22f, .22f, 1);

            string title = (!unlocked && a.HiddenUntilUnlocked) ? "???" : a.Title;
            GameObject txt = Text(title, go.transform, 18, TextAlignmentOptions.MidlineLeft);
            RectTransform tr = txt.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(82, 8); tr.offsetMax = new Vector2(-8, -8);
            TMP_Text tt = txt.GetComponent<TMP_Text>(); tt.color = unlocked ? new Color(1, .76f, .55f) : new Color(.38f, .38f, .38f); tt.enableWordWrapping = true;

            AchievementNodeTooltip tip = go.AddComponent<AchievementNodeTooltip>(); tip.Initialize(a, unlocked, font);
            return r;
        }

        private void CreateLine(Vector2 a, Vector2 b)
        {
            GameObject go = new GameObject("Link", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(tree, false); go.transform.SetAsFirstSibling();
            RectTransform r = go.GetComponent<RectTransform>(); Vector2 d = b - a;
            r.sizeDelta = new Vector2(d.magnitude, 8); r.anchoredPosition = (a + b) * .5f; r.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            go.GetComponent<Image>().color = new Color(0f, .0f, .0f, .9f);
        }

        private void OnUnlocked(AchievementDefinition a)
        {
            if (panel != null && panel.activeSelf) BuildTree();
            StartCoroutine(Popup(a));
        }

        private IEnumerator Popup(AchievementDefinition a)
        {
            Canvas canvas = pause != null ? pause.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
            if (canvas == null) yield break;
            GameObject pop = new GameObject("AchievementPopup", typeof(RectTransform), typeof(Image));
            pop.transform.SetParent(canvas.transform, false);
            RectTransform r = pop.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 1); r.sizeDelta = new Vector2(430, 120); r.anchoredPosition = new Vector2(-24, -24);
            pop.GetComponent<Image>().color = new Color(.08f, .08f, .09f, .97f);

            GameObject h = Text("Достижение получено!", pop.transform, 24, TextAlignmentOptions.TopLeft);
            RectTransform hr = h.GetComponent<RectTransform>(); hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.offsetMin = new Vector2(110, 14); hr.offsetMax = new Vector2(-12, -14);
            GameObject n = Text(a.Title, pop.transform, 20, TextAlignmentOptions.BottomLeft);
            RectTransform nr = n.GetComponent<RectTransform>(); nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one; nr.offsetMin = new Vector2(110, 12); nr.offsetMax = new Vector2(-12, -52);
            n.GetComponent<TMP_Text>().color = new Color(1, .65f, .43f);

            GameObject io = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            io.transform.SetParent(pop.transform, false);
            RectTransform ir = io.GetComponent<RectTransform>(); ir.anchorMin = ir.anchorMax = new Vector2(0, .5f); ir.sizeDelta = new Vector2(76, 76); ir.anchoredPosition = new Vector2(58, 0);
            io.GetComponent<Image>().sprite = ItemIconProvider.GetIcon(a.Icon); io.GetComponent<Image>().preserveAspect = true;

            yield return new WaitForSecondsRealtime(4f);
            Destroy(pop);
        }

        private GameObject Text(string value, Transform parent, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text t = go.GetComponent<TMP_Text>(); t.text = value; t.fontSize = size; t.alignment = align; if (font != null) t.font = font;
            return go;
        }

        private GameObject Button(string label, Transform parent, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false); go.GetComponent<Image>().color = new Color(.16f, .16f, .18f, .95f);
            Button b = go.GetComponent<Button>(); b.onClick.AddListener(action);
            GameObject tx = Text(label, go.transform, 20, TextAlignmentOptions.Center);
            RectTransform tr = tx.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
            return go;
        }
    }

    public sealed class AchievementNodeTooltip : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private AchievementDefinition def; private bool unlocked; private TMP_FontAsset font; private GameObject tip;
        public void Initialize(AchievementDefinition d, bool u, TMP_FontAsset f) { def = d; unlocked = u; font = f; }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            if (def == null) return;
            tip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image)); tip.transform.SetParent(transform, false);
            RectTransform r = tip.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(320, 110); r.anchoredPosition = new Vector2(250, 0);
            tip.GetComponent<Image>().color = new Color(.03f, .03f, .04f, .98f);
            GameObject t = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); t.transform.SetParent(tip.transform, false);
            RectTransform tr = t.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(10, 8); tr.offsetMax = new Vector2(-10, -8);
            TMP_Text tx = t.GetComponent<TMP_Text>(); tx.fontSize = 17; if (font != null) tx.font = font;
            tx.text = (!unlocked && def.HiddenUntilUnlocked) ? "???" : (def.Title + "\n" + def.Description);
        }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { if (tip != null) Destroy(tip); }
    }
}
