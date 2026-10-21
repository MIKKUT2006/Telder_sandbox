using System;
using System.Collections.Generic;
using Game.InputSupport;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Settings
{
    // Runtime panel installed by both main-menu and pause menu. Keeps all authored
    // legacy UI intact. It lives on its own screen-space canvas and is editable
    // using the inspector theme below, not baked sprites / images.
    public sealed class SettingsTabsController : MonoBehaviour
    {
        private static SettingsTabsController active;
        public static bool IsOpen { get { return active != null && active.gameObject.activeSelf; } }
        public static Transform Root { get { return active != null ? active.transform : null; } }
        [Header("Custom look (optional)")]
        public Sprite WindowTexture;
        public Sprite TabTexture;
        public Sprite ButtonTexture;
        public TMP_FontAsset Font;

        // Палитра в стилистике #1a1a1f (тёмный с синеватым отливом).
        public Color WindowColor = new Color(0.145f, 0.145f, 0.180f);       // фон окна
        public Color ContentColor = new Color(0.115f, 0.115f, 0.145f);      // фон контентной панели
        public Color ButtonColor = new Color(0.102f, 0.102f, 0.122f);       // #1a1a1f — кнопки
        public Color SelectedColor = new Color(0.180f, 0.210f, 0.260f);     // выделенная кнопка/вкладка
        public Color TextColor = new Color(0.90f, 0.94f, 0.98f);
        public Color TrackColor = new Color(0.075f, 0.075f, 0.095f);        // фон слайдера
        public Color FillColor = new Color(0.33f, 0.64f, 0.76f);            // заливка слайдера

        public Color OutlineColor = new Color(0.78f, 0.86f, 0.92f, 0.85f);
        public Color OutlineSelectedColor = new Color(0.85f, 0.95f, 1f, 1f);
        public Color OutlineDimColor = new Color(0.50f, 0.58f, 0.68f, 0.55f);

        public Color ScrollbarBgColor = new Color(0.06f, 0.06f, 0.08f, 0.80f);
        public Color ScrollbarHandleColor = new Color(0.65f, 0.72f, 0.82f, 0.85f);


        private Action onClosed;
        private RectTransform window;
        private RectTransform content;
        private int tab;
        private bool gamepadTab;
        private GameAction? bindingCapture;
        private float captureStart;
        private readonly List<GameObject> contentItems = new List<GameObject>();
        private Button[] tabs;
        private float navDelay;

        private RectTransform contentPanel;   // визуальная панель с фоном и обводкой
        private RectTransform viewport;       // маска
        private ScrollRect scroll;

        private static readonly string[] Names = {
    // 0..3
    "Прыжок", "Атака / копать", "Использовать / ставить", "Взаимодействие",
    // 4..7
    "Инвентарь", "Пауза", "Хотбар назад", "Хотбар вперёд",
    // 8..11
    "Выбросить", "Быстрый перенос", "Разделить стак", "Поворот блока",
    // 12..15
    "Слой блоков", "Движение влево", "Движение вправо", "Подтвердить",
    // 16..20
    "Отмена", "Меню вверх", "Меню вниз", "Меню влево", "Меню вправо",
    // 21..22
    "Меню измерений", "Вид от 3-го лица",
    // 23..31
    "Хотбар 1", "Хотбар 2", "Хотбар 3", "Хотбар 4", "Хотбар 5",
    "Хотбар 6", "Хотбар 7", "Хотбар 8", "Хотбар 9",
    // СЮДА ДОБАВЬ всё, что есть в GameAction после 32-го
};
        public static void Open(Action closed = null)
        {
            if (active == null)
            {
                GameObject root = new GameObject("TelderSettingsTabs", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                active = root.AddComponent<SettingsTabsController>();
                var pause = FindObjectOfType<Game.UI.Pause.PauseMenuController>();
                if (pause != null) active.Font = pause.PixelFont;
                if (active.Font == null)
                {
                    var existing = FindObjectOfType<TMP_Text>();
                    if (existing != null) active.Font = existing.font;
                }
                Canvas c = root.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 1500;
                CanvasScaler scale = root.GetComponent<CanvasScaler>();
                scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scale.referenceResolution = new Vector2(1280, 800);
                scale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scale.matchWidthOrHeight = .5f;
                active.Build();
            }
            active.onClosed = closed;
            active.gameObject.SetActive(true);
            active.RenderContent();
        }
        public static void Close(bool callBack = false)
        {
            if (active == null) return;
            var callback = active.onClosed;
            active.bindingCapture = null;
            GameInput.IsRebinding = false;
            active.gameObject.SetActive(false);
            if (callBack) { active.onClosed = null; if (callback != null) callback(); }
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }
        private Image Surface(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite texture = null)
        {
            RectTransform r = Rect(parent, name, pos, size);
            Image i = r.gameObject.AddComponent<Image>();
            i.sprite = texture;
            i.type = texture != null && texture.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            i.color = color;
            return i;
        }

        private Image Outlined(Transform parent, string name, Vector2 pos, Vector2 size,
                       Color color, Sprite texture = null, float thickness = 2f,
                       Color? outlineColor = null)
        {
            Image img = Surface(parent, name, pos, size, color, texture);
            Outline outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor ?? OutlineColor;
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = false;
            return img;
        }

        private static void SetOutline(Image img, Color color, float thickness = 2f)
        {
            if (img == null) return;
            Outline o = img.GetComponent<Outline>();
            if (o == null) o = img.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(thickness, thickness);
            o.useGraphicAlpha = false;
        }
        private TMP_Text Text(Transform parent, string label, Vector2 pos, Vector2 size, int fontSize = 20)
        {
            RectTransform r = Rect(parent, "Label_" + label, pos, size);
            TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            // Translate before this label can be drawn, including after opening a
            // new settings tab. Keep the untranslated source for future changes.
            LocalizationSourceText source = r.gameObject.AddComponent<LocalizationSourceText>();
            source.Source = label;
            t.text = GameLocalization.UI(label);
            t.fontSize = fontSize;
            t.color = TextColor;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = false;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
        private Button AddButton(Transform parent, string caption, Vector2 pos, Vector2 size, Action onClick, bool selected = false)
        {
            Image image = Outlined(parent, "Button_" + caption, pos, size,
                selected ? SelectedColor : ButtonColor,
                ButtonTexture,
                selected ? 3f : 2f,
                selected ? OutlineSelectedColor : OutlineColor);

            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            cb.pressedColor = new Color(.75f, .82f, .9f);
            cb.selectedColor = new Color(1.15f, 1.15f, 1.15f);
            button.colors = cb;
            button.onClick.AddListener(() => onClick());

            // Текст: если выделено — темнее, чтобы контрастировал на светлой обводке
            TMP_Text t = Text(image.transform, caption, Vector2.zero, size - new Vector2(10, 4), 18);
            t.color = TextColor;
            return button;
        }
        private void Build()
        {
            RectTransform canvas = transform as RectTransform;

            Image dim = Surface(canvas, "Dimmer", Vector2.zero, new Vector2(6000, 6000),
                new Color(0, 0, 0, .68f));
            dim.raycastTarget = true;

            Image windowImg = Outlined(canvas, "Window", Vector2.zero, new Vector2(1040, 675),
                WindowColor, WindowTexture, 3f, OutlineColor);
            window = windowImg.rectTransform;

            Text(window, "НАСТРОЙКИ", new Vector2(0, 301), new Vector2(420, 40), 28);

            string[] tabNames = { "ОБЩИЕ", "ГРАФИКА", "УПРАВЛЕНИЕ" };
            tabs = new Button[3];
            for (int i = 0; i < 3; ++i)
            {
                int id = i;
                tabs[i] = AddButton(window, tabNames[i],
                    new Vector2(-335 + i * 335, 248), new Vector2(315, 52),
                    () => { tab = id; RenderContent(); },
                    i == tab);
            }

            // ----- Визуальная панель контента -----
            Image contentBg = Outlined(window, "TwoColumnContent",
                new Vector2(0, -33), new Vector2(1002, 494),
                ContentColor, null, 2f, OutlineDimColor);
            contentPanel = contentBg.rectTransform;

            // ----- ScrollRect -----
            scroll = contentBg.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.10f;

            // ----- Viewport (маска) -----
            GameObject vpGo = new GameObject("Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(contentPanel, false);
            viewport = vpGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = new Vector2(8, 8);
            viewport.offsetMax = new Vector2(-24, -8);   // место под скроллбар справа
            vpGo.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            // ----- ScrollContent (куда кладутся элементы) -----
            GameObject scGo = new GameObject("ScrollContent", typeof(RectTransform));
            scGo.transform.SetParent(viewport, false);
            RectTransform sc = scGo.GetComponent<RectTransform>();
            sc.anchorMin = new Vector2(0.5f, 1f);
            sc.anchorMax = new Vector2(0.5f, 1f);
            sc.pivot = new Vector2(0.5f, 1f);
            sc.anchoredPosition = Vector2.zero;
            sc.sizeDelta = new Vector2(960, 500);
            scroll.content = sc;
            content = sc;   // ← все Text/Entry/SliderEntry/AddButton(content, …) идут сюда

            // ----- Скроллбар справа -----
            GameObject sbGo = new GameObject("Scrollbar",
                typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(contentPanel, false);
            RectTransform sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = new Vector2(-4, 0);
            sbRt.sizeDelta = new Vector2(12, -16);

            Image sbBg = sbGo.GetComponent<Image>();
            sbBg.color = ScrollbarBgColor;
            Outline sbOutline = sbGo.AddComponent<Outline>();
            sbOutline.effectColor = new Color(0.5f, 0.58f, 0.68f, 0.5f);
            sbOutline.effectDistance = new Vector2(1f, 1f);
            sbOutline.useGraphicAlpha = false;

            Scrollbar sb = sbGo.GetComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            GameObject handleGo = new GameObject("Handle",
                typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(sbRt, false);
            RectTransform hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(8, 40);
            Image hImg = handleGo.GetComponent<Image>();
            hImg.color = ScrollbarHandleColor;
            sb.handleRect = hRt;
            sb.targetGraphic = hImg;

            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 2f;

            AddButton(window, "НАЗАД", new Vector2(0, -308), new Vector2(220, 47),
                () => Close(true));
        }
        private void ClearContent()
        {
            foreach (GameObject item in contentItems) if (item != null) Destroy(item);
            contentItems.Clear();
        }
        private void Register(GameObject o) { contentItems.Add(o); }
        private void Heading(string text, int column, int row = 0)
        {
            TMP_Text t = Text(content, text, new Vector2(column == 0 ? -252 : 252, 202 - row * 41), new Vector2(462, 32), 20);
            Register(t.gameObject);
        }
        private void Entry(string label, string current, int col, int index, Action clicked)
        {
            float x = col == 0 ? -252f : 252f;
            float y = 160 - index * 39;
            TMP_Text t = Text(content, label, new Vector2(x - 99, y), new Vector2(251, 35), 15);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            Register(t.gameObject);
            Button b = AddButton(content, current, new Vector2(x + 136, y), new Vector2(178, 34), clicked);
            Register(b.gameObject);
        }
        private Slider SliderEntry(
    string label,
    float min, float max, float value,
    int col, int index,
    Action<float> onChange,
    Func<float, string> formatter = null)
        {
            float x = col == 0 ? -252f : 252f;
            float y = 160 - index * 39;

            // Label
            TMP_Text lbl = Text(content, label, new Vector2(x - 99, y), new Vector2(220, 35), 15);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            Register(lbl.gameObject);

            RectTransform trackRect = Rect(content, label + "_Track",
    new Vector2(x + 90, y), new Vector2(150, 24));
            Image trackImg = trackRect.gameObject.AddComponent<Image>();
            trackImg.color = TrackColor;
            Outline trackOutline = trackRect.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = new Color(0.5f, 0.58f, 0.68f, 0.55f);
            trackOutline.effectDistance = new Vector2(1.5f, 1.5f);
            trackOutline.useGraphicAlpha = false;
            Register(trackRect.gameObject);

            Slider slider = trackRect.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;

            // ---- Fill Area (stretches inside track) ----
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(trackRect, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(4f, 4f);
            fillAreaRt.offsetMax = new Vector2(-4f, -4f);

            // ---- Fill (Slider drives its anchorMax.x) ----
            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillAreaRt, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = FillColor;
            slider.fillRect = fillRt;

            // ---- Handle Slide Area (stretches inside track) ----
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(trackRect, false);
            RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(4f, 0f);
            handleAreaRt.offsetMax = new Vector2(-4f, 0f);

            // ---- Handle (white) ----
            GameObject handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleAreaRt, false);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(10f, 32f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            // Set value BEFORE adding listener so onValueChanged doesn't fire.
            slider.SetValueWithoutNotify(value);

            // ---- Value readout on the right ----
            TMP_Text val = Text(content, "", new Vector2(x + 197, y), new Vector2(58, 30), 14);
            val.alignment = TextAlignmentOptions.MidlineRight;
            Register(val.gameObject);
            val.text = formatter != null ? formatter(value) : value.ToString("0.00");

            slider.onValueChanged.AddListener(v =>
            {
                val.text = formatter != null ? formatter(v) : v.ToString("0.00");
                onChange?.Invoke(v);
            });

            return slider;
        }
        private void RenderContent()
        {
            if (content == null) return;
            ClearContent();

            for (int i = 0; i < tabs.Length; ++i)
            {
                Image img = tabs[i].GetComponent<Image>();
                bool sel = i == tab;
                img.color = sel ? SelectedColor : ButtonColor;
                SetOutline(img, sel ? OutlineSelectedColor : OutlineColor, sel ? 3f : 2f);
            }

            if (tab == 0) RenderGeneral();
            else if (tab == 1) RenderGraphics();
            else RenderBindings();

            FinalizeScroll();
        }

        private void FinalizeScroll()
        {
            if (content == null || viewport == null || scroll == null) return;

            // Ищем самый удалённый от центра элемент — это определит нужную высоту.
            float maxAbs = 0f;
            foreach (var go in contentItems)
            {
                if (go == null) continue;
                RectTransform rt = go.transform as RectTransform;
                if (rt == null) continue;

                float y = rt.anchoredPosition.y;
                float h = rt.rect.height;
                float top = y + h * (1 - rt.pivot.y);
                float bottom = y - h * rt.pivot.y;

                float aTop = Mathf.Abs(top);
                float aBot = Mathf.Abs(bottom);
                if (aTop > maxAbs) maxAbs = aTop;
                if (aBot > maxAbs) maxAbs = aBot;
            }

            float viewportH = viewport.rect.height;
            float height = Mathf.Max(maxAbs * 2f + 60f, viewportH + 1f);
            content.sizeDelta = new Vector2(viewport.rect.width, height);

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;   // начинаем сверху
        }


        private void RenderGeneral()
        {
            Heading("ОБЩИЕ", 0);

            SliderEntry("Общая громкость", 0f, 1f, GameInput.Settings.MasterVolume, 0, 0,
                v => { GameInput.Settings.MasterVolume = v; GameInput.Save(); },
                v => Mathf.RoundToInt(v * 100f) + "%");

            SliderEntry("Музыка", 0f, 1f, Game.Audio.TelderAudioSystem.MusicVolume, 0, 1,
                v => { Game.Audio.TelderAudioSystem.MusicVolume = v; },
                v => Mathf.RoundToInt(v * 100f) + "%");

            SliderEntry("Звуки", 0f, 1f, Game.Audio.TelderAudioSystem.SfxVolume, 0, 2,
                v => { Game.Audio.TelderAudioSystem.SfxVolume = v; },
                v => Mathf.RoundToInt(v * 100f) + "%");

            Entry("Вибрация", GameInput.Settings.Rumble ? "ВКЛ" : "ВЫКЛ", 0, 3,
                () => { GameInput.Settings.Rumble = !GameInput.Settings.Rumble; GameInput.Save(); RenderContent(); });

            // Player-wide preference, independent of world saves; updates UI immediately.
            Entry("Язык",
                GameLocalization.CurrentDisplayName, 0, 4,
                () => { GameLocalization.NextLanguage(); RenderContent(); });

            Heading("ГЕЙМПАД", 1);

            SliderEntry("Мёртвая зона стика", 0.1f, 0.45f, GameInput.Settings.StickDeadzone, 1, 0,
                v => { GameInput.Settings.StickDeadzone = v; GameInput.Save(); },
                v => v.ToString("0.00"));

            SliderEntry("Скорость курсора", 5f, 32f, GameInput.Settings.BuildAimSpeed, 1, 1,
                v => { GameInput.Settings.BuildAimSpeed = v; GameInput.Save(); },
                v => v.ToString("0.0"));

            SliderEntry("Радиус строительства", 1.5f, 5.25f, GameInput.Settings.BuildAimRadius, 1, 2,
                v => { GameInput.Settings.BuildAimRadius = v; GameInput.Save(); },
                v => v.ToString("0.0"));

            SliderEntry("Радиус прицела оружия", 1f, 4f, GameInput.Settings.CombatAimRadius, 1, 3,
                v => { GameInput.Settings.CombatAimRadius = v; GameInput.Save(); },
                v => v.ToString("0.00"));

            Entry("Прицел геймпада", GameInput.Settings.DisableGamepadAim ? "ВЫКЛ" : "ВКЛ", 1, 4,
                () => { GameInput.Settings.DisableGamepadAim = !GameInput.Settings.DisableGamepadAim; GameInput.Save(); RenderContent(); });

            Entry("Принудительный прицел", GameInput.Settings.ForceGamepadAim ? "ВКЛ" : "ВЫКЛ", 1, 5,
                () => { GameInput.Settings.ForceGamepadAim = !GameInput.Settings.ForceGamepadAim; GameInput.Save(); RenderContent(); });

            Entry("Диагностика прицела", GameInput.Settings.ShowGamepadAimDiagnostics ? "ВКЛ" : "ВЫКЛ", 1, 6,
                () => { GameInput.Settings.ShowGamepadAimDiagnostics = !GameInput.Settings.ShowGamepadAimDiagnostics; GameInput.Save(); RenderContent(); });

            Register(Text(content,
                "Прицел появляется автоматически при обнаружении геймпада. Если Steam Input скрывает устройство, включи принудительный прицел.",
                new Vector2(0, -188), new Vector2(860, 46), 15).gameObject);
        }
        private void RenderGraphics()
        {
            Heading("ЭКРАН", 0);
            string[] mode = { "ОКНО", "БЕЗ РАМОК", "ПОЛНЫЙ" };
            Entry("Режим экрана", mode[Mathf.Clamp(GameInput.Settings.FullscreenMode,0,2)], 0, 0,
                () => { GameInput.Settings.FullscreenMode = (GameInput.Settings.FullscreenMode + 1) % 3; GameInput.Save(); RenderContent(); });
            Heading("ОСВЕЩЕНИЕ", 1);
            string[] quality = { "НИЗКАЯ", "СРЕДНЯЯ", "ВЫСОКАЯ" };
            Entry("Детализация", quality[Mathf.Clamp(GameInput.Settings.LightingQuality, 0, 2)], 1, 0,
                () => { GameInput.Settings.LightingQuality = (GameInput.Settings.LightingQuality + 1) % 3; GameInput.Save(); RenderContent(); });
            Register(Text(content, "Низкая: пиксельный свет, 2 чанка/кадр.\nСредняя: плавный свет, 4 чанка/кадр. Высокая: 6.",
                new Vector2(0, -155), new Vector2(850, 80), 17).gameObject);
        }
        private void RenderBindings()
        {
            Register(AddButton(content, "КЛАВИАТУРА",
                new Vector2(-137, 213), new Vector2(250, 38),
                () => { gamepadTab = false; RenderContent(); },
                !gamepadTab).gameObject);

            Register(AddButton(content, "ГЕЙМПАД",
                new Vector2(137, 213), new Vector2(250, 38),
                () => { gamepadTab = true; RenderContent(); },
                gamepadTab).gameObject);
            GameAction[] actions = GameInput.Actions;
            int count = actions.Length;
            int half = (count + 1) / 2;

            for (int i = 0; i < count; i++)
            {
                int id = i;

                // На случай, если GameInput.Actions длиннее, чем Names.
                string label = id < Names.Length
                    ? Names[id]
                    : id.ToString();

                int col = i < half ? 0 : 1;
                int row = i < half ? i : i - half;

                Entry(
                    label,
                    GameInput.Binding(actions[id], gamepadTab).Replace("JoystickButton", "J"),
                    col, row,
                    () =>
                    {
                        bindingCapture = actions[id];
                        captureStart = Time.unscaledTime;
                        GameInput.IsRebinding = true;
                        RenderCaptureHint(actions[id]);
                    });
            }
        }
        private void RenderCaptureHint(GameAction action)
        {
            int id = (int)action;
            string label = id >= 0 && id < Names.Length ? Names[id] : id.ToString();

            TMP_Text overlay = Text(window,
                "Нажми новую кнопку для: " + label + "  (Esc — отмена)",
                new Vector2(0, -271), new Vector2(880, 39), 18);
            overlay.color = Color.yellow;
            contentItems.Add(overlay.gameObject);
        }
        private void Update()
        {
            if (!bindingCapture.HasValue && GameInput.GamepadConnected)
            {
                if (GameInput.Pressed(GameAction.HotbarNext)) { tab = (tab + 1) % 3; RenderContent(); }
                if (GameInput.Pressed(GameAction.HotbarPrevious)) { tab = (tab + 2) % 3; RenderContent(); }
            }
            if (bindingCapture.HasValue)
            {
                if (Time.unscaledTime - captureStart < .22f) return;
                if (Input.GetKeyDown(KeyCode.Escape)) { bindingCapture = null; GameInput.IsRebinding = false; RenderContent(); return; }
                string candidate;
                if (GameInput.TryCapture(gamepadTab, out candidate))
                {
                    GameInput.Rebind(bindingCapture.Value, gamepadTab, candidate);
                    bindingCapture = null;
                    GameInput.IsRebinding = false;
                    RenderContent();
                }
            }
        }
    }
}
