using UnityEngine;

namespace Game.World.Structures.EditorRuntime
{
    /// <summary>
    /// T-Editor start screen. It lives beside the existing structure editor so
    /// the structure editor can be opened without replacing its data model.
    /// </summary>
    public sealed class TEditorMenuController : MonoBehaviour
    {
        public static TEditorMenuController Instance { get; private set; }

        private StructureEditorController structureEditor;
        private TEditorContentEditorController contentEditor;
        private bool showHome = true;
        private string notice = string.Empty;
        private GUIStyle backgroundStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle secondaryButtonStyle;
        private GUIStyle logoStyle;
        private Texture2D backgroundTexture;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D hoverTexture;
        private Texture2D accentTexture;
        private static readonly System.Collections.Generic.Dictionary<string, GUIStyle> sharedLabelStyles =
            new System.Collections.Generic.Dictionary<string, GUIStyle>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstalledForExistingStructureEditorScene()
        {
            if (UnityEngine.Object.FindObjectOfType<TEditorMenuController>() != null)
                return;

            StructureEditorController existingStructureEditor =
                UnityEngine.Object.FindObjectOfType<StructureEditorController>();
            if (existingStructureEditor == null)
                return;

            GameObject host = existingStructureEditor.gameObject;
            TEditorContentEditorController content = host.GetComponent<TEditorContentEditorController>();
            if (content == null)
            {
                content = host.AddComponent<TEditorContentEditorController>();
                content.enabled = false;
            }
            host.AddComponent<TEditorMenuController>();
        }

        private void Awake()
        {
            Instance = this;
            structureEditor = GetComponent<StructureEditorController>();
            if (structureEditor == null)
                structureEditor = FindObjectOfType<StructureEditorController>();

            contentEditor = GetComponent<TEditorContentEditorController>();
            if (contentEditor == null)
                contentEditor = gameObject.AddComponent<TEditorContentEditorController>();

            if (structureEditor != null)
                structureEditor.enabled = false;
            if (contentEditor != null)
                contentEditor.enabled = false;

            showHome = true;
        }

        private void Update()
        {
            if (!showHome && Input.GetKeyDown(KeyCode.Escape))
                ReturnHome();
        }

        public void OpenStructures()
        {
            if (structureEditor == null)
            {
                notice = "Редактор структур не найден в этой сцене. Создай сцену через Tools / Game / Create Structure Editor Scene.";
                return;
            }

            showHome = false;
            notice = string.Empty;
            if (contentEditor != null)
                contentEditor.enabled = false;
            structureEditor.enabled = true;
        }

        public void OpenContentEditor(bool mobs)
        {
            if (structureEditor != null)
                structureEditor.enabled = false;

            if (contentEditor == null)
                contentEditor = GetComponent<TEditorContentEditorController>();
            if (contentEditor == null)
                contentEditor = gameObject.AddComponent<TEditorContentEditorController>();

            showHome = false;
            notice = string.Empty;
            contentEditor.OpenCatalog(mobs);
        }

        public void ReturnHome()
        {
            showHome = true;
            if (structureEditor != null)
                structureEditor.enabled = false;
            if (contentEditor != null)
                contentEditor.enabled = false;
        }

        private void OnGUI()
        {
            if (!showHome)
                return;

            EnsureStyles();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);

            float center = Screen.width * 0.5f;
            float titleSize = Mathf.Clamp(Screen.height / 25, 28, 48);
            titleStyle.fontSize = Mathf.RoundToInt(titleSize);
            subtitleStyle.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height / 17, 28, 54));

            GUI.Label(new Rect(center - 340, Screen.height * 0.09f, 680, 68), "WELCOME", titleStyle);

            float logoY = Screen.height * 0.245f;
            GUI.Label(new Rect(center - 250, logoY, 110, 74), "TO", subtitleStyle);
            GUI.Box(new Rect(center - 120, logoY + 2, 68, 68), "T", logoStyle);
            GUI.Label(new Rect(center - 30, logoY, 300, 74), "EDITOR", subtitleStyle);
            GUI.Label(
                new Rect(center - 300, logoY + 78, 600, 28),
                "РЕДАКТОР ИГРОВЫХ РЕСУРСОВ",
                MakeLabelStyle(14, new Color(0.57f, 0.62f, 0.70f), TextAnchor.MiddleCenter)
            );

            float cardGap = Mathf.Clamp(Screen.width * 0.025f, 18f, 44f);
            float cardWidth = Mathf.Min(340f, (Screen.width - 80f - cardGap * 2f) / 3f);
            float totalWidth = cardWidth * 3f + cardGap * 2f;
            float firstX = center - totalWidth * 0.5f;
            float cardY = Mathf.Max(Screen.height * 0.57f, Screen.height - 225f);
            float cardHeight = Mathf.Clamp(Screen.height * 0.24f, 150f, 205f);

            DrawMenuCard(
                new Rect(firstX, cardY, cardWidth, cardHeight),
                "01",
                "СТРУКТУРЫ",
                "Создание и редактирование структур",
                () => OpenStructures()
            );
            DrawMenuCard(
                new Rect(firstX + cardWidth + cardGap, cardY, cardWidth, cardHeight),
                "02",
                "ПРЕДМЕТЫ",
                "Блоки, предметы и их JSON-данные",
                () => OpenContentEditor(false)
            );
            DrawMenuCard(
                new Rect(firstX + (cardWidth + cardGap) * 2f, cardY, cardWidth, cardHeight),
                "03",
                "МОБЫ",
                "Параметры существ и их поведение",
                () => OpenContentEditor(true)
            );

            if (!Application.isEditor)
            {
                GUI.Label(
                    new Rect(20, Screen.height - 36, Screen.width - 40, 24),
                    "T-Editor работает в редакторе Unity и редактирует JSON внутри Assets/GameData.",
                    MakeLabelStyle(12, new Color(0.9f, 0.65f, 0.35f), TextAnchor.MiddleCenter)
                );
            }

            if (!string.IsNullOrEmpty(notice))
            {
                GUI.Label(
                    new Rect(30, Screen.height - 64, Screen.width - 60, 30),
                    notice,
                    MakeLabelStyle(13, new Color(1f, 0.45f, 0.42f), TextAnchor.MiddleCenter)
                );
            }

            GUI.Label(
                new Rect(0, Screen.height - 26, Screen.width, 20),
                "TELDER  •  T-EDITOR  •  JSON RESOURCE WORKBENCH",
                MakeLabelStyle(10, new Color(0.35f, 0.39f, 0.46f), TextAnchor.MiddleCenter)
            );
        }

        private void DrawMenuCard(Rect rect, string number, string title, string description, System.Action onClick)
        {
            GUI.Box(rect, GUIContent.none, backgroundStyle);
            GUI.Label(new Rect(rect.x + 18, rect.y + 14, rect.width - 36, 24), number, MakeLabelStyle(13, new Color(0.38f, 0.67f, 0.90f), TextAnchor.MiddleLeft));
            GUI.Label(new Rect(rect.x + 18, rect.y + 45, rect.width - 36, 42), title, MakeLabelStyle(Mathf.Clamp(Screen.height / 48, 18, 25), Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(rect.x + 18, rect.y + 91, rect.width - 36, 40), description, MakeLabelStyle(13, new Color(0.63f, 0.67f, 0.73f), TextAnchor.UpperLeft));

            Rect buttonRect = new Rect(rect.x + 18, rect.yMax - 52, rect.width - 36, 36);
            if (GUI.Button(buttonRect, "ОТКРЫТЬ  →", buttonStyle) && onClick != null)
                onClick();
        }

        private void EnsureStyles()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = MakeTexture(new Color(0.055f, 0.063f, 0.078f));
                panelTexture = MakeTexture(new Color(0.085f, 0.098f, 0.12f));
                buttonTexture = MakeTexture(new Color(0.13f, 0.15f, 0.18f));
                hoverTexture = MakeTexture(new Color(0.20f, 0.34f, 0.43f));
                accentTexture = MakeTexture(new Color(0.19f, 0.39f, 0.51f));

                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 40;
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = Color.white;

                subtitleStyle = new GUIStyle(GUI.skin.label);
                subtitleStyle.fontSize = 44;
                subtitleStyle.alignment = TextAnchor.MiddleCenter;
                subtitleStyle.fontStyle = FontStyle.Bold;
                subtitleStyle.normal.textColor = Color.white;

                backgroundStyle = new GUIStyle(GUI.skin.box);
                backgroundStyle.normal.background = panelTexture;
                backgroundStyle.border = new RectOffset(1, 1, 1, 1);

                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 15;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                buttonStyle.normal.background = accentTexture;
                buttonStyle.hover.background = hoverTexture;
                buttonStyle.active.background = panelTexture;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.hover.textColor = Color.white;
                buttonStyle.padding = new RectOffset(10, 10, 6, 6);

                secondaryButtonStyle = new GUIStyle(buttonStyle);
                secondaryButtonStyle.normal.background = buttonTexture;

                logoStyle = new GUIStyle(GUI.skin.box);
                logoStyle.fontSize = 30;
                logoStyle.fontStyle = FontStyle.Bold;
                logoStyle.alignment = TextAnchor.MiddleCenter;
                logoStyle.normal.background = buttonTexture;
                logoStyle.normal.textColor = Color.white;
            }
        }

        internal static GUIStyle MakeLabelStyle(int fontSize, Color color, TextAnchor alignment)
        {
            string key = fontSize + "|" + color.GetHashCode() + "|" + (int)alignment;
            GUIStyle cached;
            if (sharedLabelStyles.TryGetValue(key, out cached))
                return cached;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.alignment = alignment;
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.wordWrap = true;
            sharedLabelStyles[key] = style;
            return style;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            DestroyTexture(backgroundTexture);
            DestroyTexture(panelTexture);
            DestroyTexture(buttonTexture);
            DestroyTexture(hoverTexture);
            DestroyTexture(accentTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
