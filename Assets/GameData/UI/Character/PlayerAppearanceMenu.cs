using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Character
{
    /// <summary>Full runtime character editor, independently overlaid on the main menu.
    /// All data is plain JSON under persistentDataPath, and works in standalone builds.</summary>
    public sealed class PlayerAppearanceMenu : MonoBehaviour
    {
        private static PlayerAppearanceMenu current;
        // Keep the original enum values for saved presets; only the visible tab order changes.
        private static readonly PartCategory[] Tabs = {
            PartCategory.Legs, PartCategory.Boots, PartCategory.Arms,
            PartCategory.Sleeves, PartCategory.Body, PartCategory.Outerwears,
            PartCategory.Head, PartCategory.Eyes, PartCategory.Hair, PartCategory.Jewelry
        };
        private static readonly string[] TabLabels = {
            "НОГИ", "БОТИНКИ", "РУКИ", "РУКАВА", "ТОРС",
            "ОДЕЖДА", "ГОЛОВА", "ГЛАЗА", "ВОЛОСЫ", "УКРАШЕНИЯ"
        };
        private static readonly Color Dark = new Color(.075f, .084f, .105f, .98f);
        private static readonly Color Panel = new Color(.11f, .125f, .153f, .97f);
        private static readonly Color Border = new Color(.42f, .47f, .54f, 1);
        private static readonly Color Selected = new Color(.26f, .42f, .52f, 1);
        private const int PreviewLayer = 30;
        private Action onClosed;
        private TMP_FontAsset font;
        private AppearanceSaveData library;
        private AppearancePreset draft;
        private PartCategory tab = PartCategory.Hair;
        private Image[] tabImages;
        private RectTransform catalogContent;
        private ScrollRect catalogScroll;
        private TMP_Text paletteTitle;
        private TMP_Text warning;
        private RectTransform windowRect;
        private bool fitWindowPending = true;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private TMP_InputField presetName;
        private Button dropdownButton;
        private TMP_Text dropdownLabel;
        private GameObject dropdownPanel;
        private RectTransform dropdownContent;
        private GameObject palettePanel;
        private Image colorSwatch;
        private Slider valueSlider;
        private float hue;
        private float sat;
        private float brightness = 1f;
        private AppearanceRig previewRig;
        private GameObject previewObject;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private readonly List<Material> createdMaterials = new List<Material>();
        private Texture2D wheelTexture;
        private Sprite wheelSprite;
        private Texture2D markerTexture;
        private Sprite markerSprite;
        private RectTransform wheelMarker;
        private Image wheelMarkerFill;
        private RectTransform wheelRect;

        public static bool IsOpen { get { return current != null; } }

        public static void Open(Action closed = null)
        {
            if (current != null) { current.onClosed = closed; return; }
            GameObject go = new GameObject("Telder_Character_Editor", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            current = go.AddComponent<PlayerAppearanceMenu>();
            current.onClosed = closed;
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2100;
            CanvasScaler scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280f, 800f);
            scale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scale.matchWidthOrHeight = .5f;
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("CharacterEditorEventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            current.Build();
        }
        public static void Close(bool returnToMenu = false)
        {
            if (current == null) return;
            Action cb = returnToMenu ? current.onClosed : null;
            var go = current.gameObject;
            current = null;
            Destroy(go);
            if (cb != null) cb();
        }
        private RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
        private Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, pos, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }
        private TMP_Text Label(Transform parent, string name, string text, Vector2 pos,
            Vector2 size, float fontSize = 18, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rt = Rect(parent, name, pos, size);
            TextMeshProUGUI label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = new Color(.93f, .95f, .99f);
            label.alignment = alignment;
            if (font != null) label.font = font;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
        private Button ActionButton(Transform parent, string name, string caption,
            Vector2 pos, Vector2 size, Action callback, Color? background = null)
        {
            Image image = Box(parent, name, pos, size, background ?? Panel);
            Button b = image.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.24f, 1.24f, 1.24f, 1f);
            colors.pressedColor = new Color(.7f, .8f, .9f, 1);
            b.colors = colors;
            b.onClick.AddListener(() => callback());
            Label(image.transform, "Text", caption, Vector2.zero, size - new Vector2(10, 4), 16);
            return b;
        }
        private void Build()
        {
            var existing = FindObjectOfType<TMP_Text>();
            font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
            library = PlayerAppearanceStorage.Load();
            var active = PlayerAppearanceStorage.Active();
            Image overlay = Box(transform, "Dim", Vector2.zero, new Vector2(4000, 4000), new Color(0, 0, 0, .78f));
            overlay.raycastTarget = true;
            windowRect = Box(transform, "Appearance_Window", Vector2.zero,
                new Vector2(1190, 745), Dark).rectTransform;
            RectTransform window = windowRect;
            Box(window, "Border_Left", new Vector2(-205, 0), new Vector2(6, 700), Border);
            Label(window, "Title", "РЕДАКТОР ПЕРСОНАЖА", new Vector2(0, 330), new Vector2(700, 42), 28);
            // Player and preset list on the left.
            Box(window, "Preview_Background", new Vector2(-394, -10), new Vector2(360, 548),
                new Color(.035f, .044f, .058f, 1));
            RawImage raw = Rect(window, "Animated_Player_Preview", new Vector2(-394, -11),
                new Vector2(300, 405)).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            dropdownButton = ActionButton(window, "Preset_Dropdown", "", new Vector2(-422, 300),
                new Vector2(267, 39), TogglePresetDropdown);
            dropdownLabel = dropdownButton.GetComponentInChildren<TMP_Text>();
            ActionButton(window, "New_Preset", "+", new Vector2(-263, 300), new Vector2(45, 39), NewPreset);
            warning = Label(window, "Preview_Info", "", new Vector2(-394, -245), new Vector2(340, 52), 14);
            Image dye = Box(window, "Tint_Button", new Vector2(-394, -321), new Vector2(112, 49),
                new Color(1f, .85f, .1f));
            colorSwatch = Box(dye.transform, "Selected_Tint", new Vector2(-39, 0),
                new Vector2(18, 18), Color.white);
            colorSwatch.raycastTarget = false;
            dye.gameObject.AddComponent<Button>().onClick.AddListener(OpenPalette);
            Label(dye.transform, "Palette_Label", "ПОКРАСКА", new Vector2(13, 0),
                new Vector2(75, 42), 12).color = new Color(.15f, .10f, .02f);
            // Variants and category tabs at right.
            RectTransform viewport = Box(window, "Variants_Viewport", new Vector2(189, 68),
                new Vector2(734, 410), new Color(.055f, .07f, .09f, 1)).rectTransform;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform content = Rect(viewport, "Variants_Grid", Vector2.zero, new Vector2(700, 400));
            content.anchorMin = content.anchorMax = new Vector2(.5f, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = new Vector2(-12f, -10f);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(124, 116);
            grid.spacing = new Vector2(12, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            catalogContent = content;
            catalogScroll = viewport.gameObject.AddComponent<ScrollRect>();
            catalogScroll.viewport = viewport;
            catalogScroll.content = content;
            catalogScroll.horizontal = false;
            catalogScroll.vertical = true;
            catalogScroll.scrollSensitivity = 25;
            CreateScrollbar(window);
            // Ten categories are arranged in two rows of five so the category controls
            // do not overlap the preset-name row or the save/cancel buttons.
            tabImages = new Image[Tabs.Length];
            const float tabWidth = 136f;
            const float tabStep = 145f;
            const float tabStartX = -101f;
            const float firstTabRowY = -164f;
            const float secondTabRowY = -211f;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                float y = i < 5 ? firstTabRowY : secondTabRowY;
                Button b = ActionButton(window, "Tab_" + Tabs[i], TabLabels[i],
                    new Vector2(tabStartX + (i % 5) * tabStep, y), new Vector2(tabWidth, 40),
                    () => SelectTab(Tabs[index]));
                tabImages[i] = b.GetComponent<Image>();
                TMP_Text tabText = b.GetComponentInChildren<TMP_Text>();
                if (tabText != null) tabText.fontSize = 13f;
            }
            // The name row has its own vertical band above the action buttons.
            Label(window, "Name_Label", "НАЗВАНИЕ ПРЕСЕТА:", new Vector2(48, -280),
                new Vector2(230, 28), 14, TextAlignmentOptions.MidlineRight);
            BuildNameInput(window);
            ActionButton(window, "Cancel", "ОТМЕНА", new Vector2(308, -329),
                new Vector2(150, 43), () => Close(true));
            ActionButton(window, "Save", "СОХРАНИТЬ", new Vector2(489, -329),
                new Vector2(165, 43), SavePreset, new Color(.18f, .39f, .28f));
            BuildDropdown(window);
            BuildPalette(window);
            CreatePreview(raw);
            fitWindowPending = true;
            draft = active != null ? active : previewRig != null ? previewRig.Default("Новый персонаж") : EmptyPreset();
            presetName.text = draft.Name;
            RefreshPreview();
            RefreshGrid();
            RefreshPresetCaption();
        }
        private void CreateScrollbar(Transform parent)
        {
            Image track = Box(parent, "Scroll_Track", new Vector2(563, 68),
                new Vector2(14, 410), new Color(.13f, .16f, .2f));
            RectTransform slide = Rect(track.transform, "Sliding_Area", Vector2.zero, new Vector2(12, 406));
            slide.anchorMin = Vector2.zero;
            slide.anchorMax = Vector2.one;
            slide.offsetMin = new Vector2(2, 2);
            slide.offsetMax = new Vector2(-2, -2);
            Image handle = Box(slide, "Handle", Vector2.zero,
                new Vector2(10, 64), new Color(.54f, .62f, .67f));
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            bar.targetGraphic = handle;
            bar.handleRect = handle.rectTransform;
            bar.direction = Scrollbar.Direction.BottomToTop;
            catalogScroll.verticalScrollbar = bar;
            catalogScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }
        private void BuildNameInput(Transform parent)
        {
            Image frame = Box(parent, "Preset_Name_Input", new Vector2(301, -280),
                new Vector2(277, 37), new Color(.14f, .16f, .19f));
            RectTransform textRect = Rect(frame.transform, "TextArea", Vector2.zero, new Vector2(250, 35));
            var text = Label(textRect, "Text", "", Vector2.zero, new Vector2(250, 35), 17,
                TextAlignmentOptions.MidlineLeft) as TextMeshProUGUI;
            var placeholder = Label(textRect, "Placeholder", "Название...", Vector2.zero,
                new Vector2(250, 35), 17, TextAlignmentOptions.MidlineLeft);
            placeholder.color = new Color(.65f, .7f, .75f);
            presetName = frame.gameObject.AddComponent<TMP_InputField>();
            presetName.textViewport = textRect;
            presetName.textComponent = text;
            presetName.placeholder = placeholder;
            presetName.characterLimit = 32;
            presetName.lineType = TMP_InputField.LineType.SingleLine;
        }
        private void BuildDropdown(Transform parent)
        {
            dropdownPanel = Box(parent, "Preset_List", new Vector2(-394, 139),
                new Vector2(320, 275), new Color(.14f, .17f, .21f, 1)).gameObject;
            dropdownPanel.transform.SetAsLastSibling();
            RectTransform body = Rect(dropdownPanel.transform, "PresetScrollContent", Vector2.zero, new Vector2(300, 270));
            body.anchorMin = body.anchorMax = new Vector2(.5f, 1f);
            body.pivot = new Vector2(.5f, 1f);
            body.anchoredPosition = new Vector2(0, -4);
            dropdownContent = body;
            var mask = dropdownPanel.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var scroll = dropdownPanel.AddComponent<ScrollRect>();
            scroll.content = body;
            scroll.viewport = dropdownPanel.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 28f;
            dropdownPanel.SetActive(false);
        }
        private void TogglePresetDropdown()
        {
            bool opening = !dropdownPanel.activeSelf;
            dropdownPanel.SetActive(opening);
            if (!opening) return;
            Transform holder = dropdownContent;
            for (int i = holder.childCount - 1; i >= 0; --i) Destroy(holder.GetChild(i).gameObject);
            int count = library.Presets != null ? library.Presets.Count : 0;
            if (count == 0) Label(holder, "No_presets", "Пресеты ещё не сохранены", Vector2.zero,
                new Vector2(300, 40), 15);
            for (int i = 0; i < count; i++)
            {
                if (library.Presets[i] == null) continue;
                string id = library.Presets[i].Id;
                string text = library.Presets[i].Name;
                Button b = ActionButton(holder, "Preset_" + i, text,
                    new Vector2(0, -21 - i * 40), new Vector2(295, 35), () => LoadPreset(id));
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f);
            }
            dropdownContent.sizeDelta = new Vector2(300, Mathf.Max(270f, count * 40f + 8f));
        }
        private void LoadPreset(string id)
        {
            foreach (var p in library.Presets)
                if (p != null && p.Id == id)
                {
                    draft = p.Clone();
                    presetName.text = draft.Name;
                    dropdownPanel.SetActive(false);
                    RefreshPreview();
                    RefreshGrid();
                    RefreshPresetCaption();
                    return;
                }
        }
        private AppearancePreset EmptyPreset()
        {
            PartCategory[] categories = (PartCategory[])Enum.GetValues(typeof(PartCategory));
            var p = new AppearancePreset { Id = Guid.NewGuid().ToString("N"), Name = "Новый персонаж", Parts = new AppearancePart[categories.Length] };
            for (int i = 0; i < categories.Length; ++i)
            {
                PartCategory category = categories[i];
                Sprite[] sprites = PlayerAppearanceStorage.GetVariants(category);
                p.Parts[i] = new AppearancePart { Category = category.ToString(),
                    Sprite = sprites.Length > 0 && category != PartCategory.Hair ? sprites[0].name : "", Tint = Color.white };
            }
            return p;
        }
        private void NewPreset()
        {
            draft = previewRig != null ? previewRig.Default("Новый персонаж") : EmptyPreset();
            presetName.text = draft.Name;
            dropdownPanel.SetActive(false);
            RefreshPresetCaption();
            RefreshPreview();
            RefreshGrid();
        }
        private void RefreshPresetCaption()
        {
            if (dropdownLabel != null) dropdownLabel.text = (draft != null ? draft.Name : "Новый") + "  ▾";
        }
        private void SelectTab(PartCategory next)
        {
            tab = next;
            RefreshGrid();
            RefreshSwatch();
        }
        private void RefreshGrid()
        {
            for (int i = catalogContent.childCount - 1; i >= 0; i--) Destroy(catalogContent.GetChild(i).gameObject);
            for (int i = 0; i < Tabs.Length; i++) tabImages[i].color = tab == Tabs[i] ? Selected : Panel;
            Sprite[] sprites = PlayerAppearanceStorage.GetVariants(tab);
            int num = sprites.Length + (tab == PartCategory.Hair ? 1 : 0);
            for (int i = 0; i < num; i++)
            {
                Sprite icon = tab == PartCategory.Hair && i == 0 ? null : sprites[i - (tab == PartCategory.Hair ? 1 : 0)];
                string key = icon != null ? icon.name : "";
                AppearancePart part = draft.Part(tab);
                bool picked = part != null && part.Sprite == key;
                Image tile = Box(catalogContent, "Part_" + i, Vector2.zero, new Vector2(120, 110), picked ? Selected : Panel);
                var b = tile.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => PickSprite(key));
                if (icon != null)
                {
                    Image portrait = Box(tile.transform, "Sprite", new Vector2(0, 13), new Vector2(78, 78), Color.white);
                    portrait.sprite = icon;
                    portrait.preserveAspect = true;
                    portrait.raycastTarget = false;
                }
                else Label(tile.transform, "None", "×", new Vector2(0, 13), new Vector2(80, 80), 42);
                Label(tile.transform, "Name", icon != null ? icon.name : "БЕЗ ВОЛОС",
                    new Vector2(0, -44), new Vector2(114, 20), 12);
            }
            if (num == 0)
                Label(catalogContent, "Empty", "Нет PNG в ResourcePacks/Default/textures/Entity/Player/" + PlayerAppearanceStorage.Folder(tab), Vector2.zero,
                    new Vector2(550, 80), 15);
            int rows = Mathf.Max(1, Mathf.CeilToInt(num / 5f));
            catalogContent.sizeDelta = new Vector2(700, Mathf.Max(410, rows * 130 + 30));
            catalogScroll.verticalNormalizedPosition = 1f;
            RefreshSwatch();
        }
        private void PickSprite(string sprite)
        {
            AppearancePart part = draft.Part(tab);
            if (part != null) part.Sprite = sprite;
            RefreshPreview();
            RefreshGrid();
        }
        private void RefreshSwatch()
        {
            AppearancePart part = draft != null ? draft.Part(tab) : null;
            if (colorSwatch != null) colorSwatch.color = part != null ? part.Tint : Color.yellow;
        }
        private void RefreshPreview()
        {
            if (previewRig != null) previewRig.Set(draft);
            if (draft != null) RefreshSwatch();
        }
        private void SavePreset()
        {
            string name = presetName.text.Trim();
            if (string.IsNullOrEmpty(name)) name = "Персонаж " + (library.Presets.Count + 1);
            draft.Name = name;
            if (string.IsNullOrEmpty(draft.Id)) draft.Id = Guid.NewGuid().ToString("N");
            int previous = library.Presets.FindIndex(p => p != null && p.Id == draft.Id);
            if (previous < 0) library.Presets.Add(draft.Clone());
            else library.Presets[previous] = draft.Clone();
            library.ActiveId = draft.Id;
            if (!PlayerAppearanceStorage.Save(library))
            {
                warning.text = "Не удалось сохранить пресет. Проверь Console.";
                return;
            }
            PlayerAppearanceRuntime.Refresh();
            Close(true);
        }
        private void CreatePreview(RawImage target)
        {
            GameObject prefab = UnityEngine.Resources.Load<GameObject>("PlayerCustomizer/PlayerPreview");
            if (prefab == null)
            {
                warning.text = "Префаб: Assets/GameData/Resources/PlayerCustomizer/PlayerPreview.prefab";
                return;
            }
            // Spawn into an inactive staging root: combat/world MonoBehaviours never
            // receive OnEnable before they are disabled, even for complex prefabs.
            previewObject = new GameObject("CharacterEditor_Preview_ONLY");
            previewObject.transform.position = new Vector3(50000f, 50000f, 0);
            previewObject.SetActive(false);
            previewObject.AddComponent<AppearancePreviewMarker>();
            GameObject character = Instantiate(prefab, previewObject.transform, false);
            character.name = "Animated_Player_Rig";
            character.transform.localPosition = Vector3.zero;
            foreach (Transform child in previewObject.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
            foreach (MonoBehaviour script in previewObject.GetComponentsInChildren<MonoBehaviour>(true))
                if (script != null && !(script is AppearancePreviewMarker)) script.enabled = false;
            foreach (Rigidbody2D rb in previewObject.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
            foreach (Collider2D coll in previewObject.GetComponentsInChildren<Collider2D>(true)) coll.enabled = false;
            foreach (Camera camera in previewObject.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (AudioListener listener in previewObject.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (AudioSource source in previewObject.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
            Shader unlit = Shader.Find("Sprites/Default");
            if (unlit != null)
            {
                foreach (SpriteRenderer renderer in previewObject.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Material mat = new Material(unlit);
                    createdMaterials.Add(mat);
                    renderer.sharedMaterial = mat;
                }
            }
            Animator animator = previewObject.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.runtimeAnimatorController != null)
                {
                    int idle = Animator.StringToHash("Base Layer.Idle");
                    if (animator.HasState(0, idle)) animator.Play(idle, 0, 0f);
                    else if (animator.HasState(0, Animator.StringToHash("Idle"))) animator.Play("Idle", 0, 0f);
                }
            }
            previewRig = new AppearanceRig(previewObject.transform);
            previewTexture = new RenderTexture(480, 640, 16, RenderTextureFormat.ARGB32);
            previewTexture.filterMode = FilterMode.Point;
            previewTexture.Create();
            GameObject camObject = new GameObject("CharacterEditor_IsolatedCamera");
            camObject.transform.SetParent(previewObject.transform, false);
            previewCamera = camObject.AddComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0, 0, 0, 0);
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.targetTexture = previewTexture;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            Bounds bounds = new Bounds(previewObject.transform.position, new Vector3(1, 2, .01f));
            bool first = true;
            foreach (SpriteRenderer sr in previewObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.sprite == null) continue;
                if (first) { bounds = sr.bounds; first = false; }
                else bounds.Encapsulate(sr.bounds);
            }
            previewCamera.orthographicSize = Mathf.Max(1.25f, bounds.extents.y * 1.6f,
                bounds.extents.x / (480f / 640f) * 1.5f);
            Vector3 center = first ? previewObject.transform.position : bounds.center;
            camObject.transform.position = new Vector3(center.x, center.y, center.z - 15f);
            target.texture = previewTexture;
            previewObject.SetActive(true);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                int idleState = Animator.StringToHash("Base Layer.Idle");
                if (animator.HasState(0, idleState)) animator.Play(idleState, 0, 0f);
                else if (animator.HasState(0, Animator.StringToHash("Idle"))) animator.Play("Idle", 0, 0f);
            }
            warning.text = "ПЕРСОНАЖ — АНИМАЦИЯ IDLE";
        }
        private void BuildPalette(Transform parent)
        {
            palettePanel = Box(parent, "Color_Wheel_Panel", new Vector2(0, 0),
                new Vector2(420, 490), new Color(.12f, .14f, .18f, 1)).gameObject;
            palettePanel.transform.SetAsLastSibling();
            paletteTitle = Label(palettePanel.transform, "Color_Title", "ЦВЕТ: " + TabLabels[Array.IndexOf(Tabs, tab)],
                new Vector2(0, 209), new Vector2(360, 40), 21);
            wheelTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            wheelTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    Vector2 point = new Vector2((x - 127.5f) / 127.5f, (y - 127.5f) / 127.5f);
                    float r = point.magnitude;
                    Color c = Color.HSVToRGB(Mathf.Repeat(Mathf.Atan2(point.y, point.x) / (2f * Mathf.PI), 1f), Mathf.Clamp01(r), 1f);
                    c.a = r <= 1f ? 1f : 0f;
                    wheelTexture.SetPixel(x, y, c);
                }
            wheelTexture.Apply(false, true);
            wheelSprite = Sprite.Create(wheelTexture, new Rect(0, 0, 256, 256),
                new Vector2(.5f, .5f), 100);
            Image wheel = Box(palettePanel.transform, "Hue_Saturation_Wheel", new Vector2(0, 43),
                new Vector2(286, 286), Color.white);
            wheel.sprite = wheelSprite;
            wheelRect = wheel.rectTransform;
            CreateWheelMarker(wheel.transform);
            var handler = wheel.gameObject.AddComponent<AppearanceColorWheelInput>();
            handler.Connect((h, s) =>
            {
                hue = h;
                sat = s;
                RefreshWheelMarker();
                ApplyCurrentColor();
            });
            Label(palettePanel.transform, "Brightness", "ЯРКОСТЬ", new Vector2(0, -143),
                new Vector2(170, 25), 16);
            Image track = Box(palettePanel.transform, "Brightness_Slider", new Vector2(0, -179),
                new Vector2(280, 22), new Color(.3f, .32f, .34f));
            valueSlider = track.gameObject.AddComponent<Slider>();
            valueSlider.direction = Slider.Direction.LeftToRight;
            RectTransform handleArea = Rect(track.transform, "HandleArea", Vector2.zero, new Vector2(280, 22));
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = Vector2.zero;
            handleArea.offsetMax = Vector2.zero;
            Image handle = Box(handleArea, "Handle", Vector2.zero, new Vector2(18, 29), Color.white);
            valueSlider.handleRect = handle.rectTransform;
            valueSlider.targetGraphic = handle;
            valueSlider.minValue = 0.05f;
            valueSlider.maxValue = 1f;
            valueSlider.value = 1f;
            valueSlider.onValueChanged.AddListener(v =>
            {
                brightness = v;
                RefreshWheelMarker();
                ApplyCurrentColor();
            });
            ActionButton(palettePanel.transform, "ConfirmColor", "ГОТОВО", new Vector2(0, -225),
                new Vector2(160, 38), () => palettePanel.SetActive(false));
            palettePanel.SetActive(false);
        }
        private void OpenPalette()
        {
            AppearancePart part = draft.Part(tab);
            if (part == null) return;
            Color.RGBToHSV(part.Tint, out hue, out sat, out brightness);
            valueSlider.SetValueWithoutNotify(Mathf.Clamp(brightness, .05f, 1f));
            palettePanel.SetActive(true);
            palettePanel.transform.SetAsLastSibling();
            if (paletteTitle != null)
                paletteTitle.text = "ЦВЕТ: " + TabLabels[Array.IndexOf(Tabs, tab)];
            RefreshWheelMarker();
        }
        private void CreateWheelMarker(Transform wheelTransform)
        {
            markerTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            markerTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = (x + .5f - 16f) / 16f;
                float dy = (y + .5f - 16f) / 16f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                markerTexture.SetPixel(x, y, radius <= 1f ? Color.white : Color.clear);
            }
            markerTexture.Apply(false, true);
            markerSprite = Sprite.Create(markerTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 100);

            Image outline = Box(wheelTransform, "Color_Selection_Outline", Vector2.zero,
                new Vector2(22, 22), Color.black);
            outline.sprite = markerSprite;
            outline.raycastTarget = false;
            wheelMarker = outline.rectTransform;
            Image fill = Box(outline.transform, "Color_Selection_Fill", Vector2.zero,
                new Vector2(13, 13), Color.white);
            fill.sprite = markerSprite;
            fill.raycastTarget = false;
            wheelMarkerFill = fill;
            RefreshWheelMarker();
        }

        private void RefreshWheelMarker()
        {
            if (wheelMarker == null || wheelRect == null) return;
            float radius = Mathf.Max(0f, wheelRect.rect.width * .5f - 11f) * Mathf.Clamp01(sat);
            float angle = hue * Mathf.PI * 2f;
            wheelMarker.anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            if (wheelMarkerFill != null)
                wheelMarkerFill.color = Color.HSVToRGB(hue, sat, Mathf.Clamp01(brightness));
        }

        private void ApplyCurrentColor()
        {
            AppearancePart part = draft.Part(tab);
            if (part == null) return;
            part.Tint = Color.HSVToRGB(hue, sat, brightness);
            RefreshPreview();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (palettePanel != null && palettePanel.activeSelf) palettePanel.SetActive(false);
                else if (dropdownPanel != null && dropdownPanel.activeSelf) dropdownPanel.SetActive(false);
                else Close(true);
            }
        }
        private void LateUpdate()
        {
            if (fitWindowPending || lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
                fitWindowPending = !FitWindowToCanvas();
            if (previewRig != null) previewRig.Tick();
        }

        private bool FitWindowToCanvas()
        {
            if (windowRect == null) return false;
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect == null) return false;
            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            if (width <= 0f || height <= 0f) return false;
            const float margin = 28f;
            float fitX = (width - margin * 2f) / 1190f;
            float fitY = (height - margin * 2f) / 745f;
            float scale = Mathf.Clamp(Mathf.Min(1f, fitX, fitY), .55f, 1f);
            windowRect.localScale = new Vector3(scale, scale, 1f);
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            return true;
        }
        private void OnDestroy()
        {
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (previewTexture != null) { previewTexture.Release(); Destroy(previewTexture); }
            if (previewObject != null) Destroy(previewObject);
            foreach (Material mat in createdMaterials) if (mat != null) Destroy(mat);
            if (wheelSprite != null) Destroy(wheelSprite);
            if (wheelTexture != null) Destroy(wheelTexture);
            if (markerSprite != null) Destroy(markerSprite);
            if (markerTexture != null) Destroy(markerTexture);
            if (current == this) current = null;
        }
    }

    public sealed class AppearanceColorWheelInput : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private Action<float, float> change;
        public void Connect(Action<float, float> callback) { change = callback; }
        public void OnPointerDown(PointerEventData e) { Read(e); }
        public void OnDrag(PointerEventData e) { Read(e); }
        private void Read(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            Vector2 pos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out pos)) return;
            float x = pos.x / Mathf.Max(1, rt.rect.width * .5f);
            float y = pos.y / Mathf.Max(1, rt.rect.height * .5f);
            Vector2 p = new Vector2(x, y);
            if (p.magnitude > 1.08f) return;
            if (change != null) change(Mathf.Repeat(Mathf.Atan2(y, x) / (2f * Mathf.PI), 1), Mathf.Clamp01(p.magnitude));
        }
    }
}
