using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI.Character;
using Game.Networking;


namespace Game.UI.MainMenu
{
    public class MainMenuController :
        MonoBehaviour
    {
        [SerializeField]
        private GameObject homePanel;


        [SerializeField]
        private GameObject saveSelectionPanel;


        [SerializeField]
        private GameObject settingsPanel;

        [SerializeField]
        private GameObject infoPanel;


        [SerializeField]
        private SaveSelectionController
            saveSelection;


        private void Start()
        {
            Time.timeScale =
                1f;


            ShowHome();
            EnsureMenuAudioListener();
            EnsureCharacterEditorButton();
            EnsureNetworkGameButton();
            EnsureSpecialWorldsButton();
            if (Game.SpecialWorlds.SpecialWorldsMenu.ConsumeReturnToSelector())
                OpenSpecialWorlds();
        }
        private void EnsureNetworkGameButton()
        {
            if (homePanel == null) return;
            Button[] buttons = homePanel.GetComponentsInChildren<Button>(true);
            Button play = null;
            foreach (Button b in buttons)
            {
                var label = b.GetComponentInChildren<TMP_Text>(true);
                if (b.name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (label != null && (label.text.IndexOf("ИГРАТЬ", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       label.text.IndexOf("ИГРА", System.StringComparison.OrdinalIgnoreCase) >= 0)))
                { play = b; break; }
            }
            if (play != null)
            {
                Transform parent = play.transform.parent;
                if (parent.Find("NetworkGameButton") == null)
                {
                    GameObject go = new GameObject("NetworkGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    go.transform.SetParent(parent, false);
                    go.transform.SetSiblingIndex(play.transform.GetSiblingIndex() + 1);
                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.sizeDelta = ((RectTransform)play.transform).sizeDelta;
                    Image image = go.GetComponent<Image>();
                    Image sample = play.GetComponent<Image>();
                    if (sample != null) { image.sprite = sample.sprite; image.color = sample.color; }
                    Button button = go.GetComponent<Button>();
                    button.targetGraphic = image;
                    button.onClick.AddListener(OpenNetworkGame);
                    GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelObject.transform.SetParent(go.transform, false);
                    RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
                    var text = labelObject.GetComponent<TextMeshProUGUI>();
                    
                    text.text = "СЕТЕВАЯ ИГРА";
                    text.alignment = TextAlignmentOptions.Center;
                    text.raycastTarget = false;
                }
            }
            NetworkMenuPanel.Ensure(homePanel);
        }

        private void OpenNetworkGame()
        {
            if (homePanel != null) homePanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            var panel = FindObjectOfType<NetworkMenuPanel>(true);
            if (panel != null) panel.Open();
        }

        private void EnsureSpecialWorldsButton()
        {
            if (homePanel == null) return;
            Button target = null;
            Button template = null;
            foreach (Button button in homePanel.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                string content = label != null ? label.text : "";
                if (button.name.IndexOf("Special", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    content.IndexOf("ОСОБЫЕ МИРЫ", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    content.IndexOf("SPECIAL WORLDS", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    target = button;
                if (template == null || button.name == "SettingsButton") template = button;
            }
            if (target == null && template != null)
            {
                // A fresh Button is essential: cloning serialized buttons also
                // clones their persistent OnClick callbacks (e.g. OpenSettings).
                GameObject go = new GameObject("SpecialWorldsButton", typeof(RectTransform),
                    typeof(Image), typeof(Button));
                go.transform.SetParent(template.transform.parent, false);
                go.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                ((RectTransform)go.transform).sizeDelta =
                    ((RectTransform)template.transform).sizeDelta;
                var img = go.GetComponent<Image>();
                var originalImage = template.GetComponent<Image>();
                if (originalImage != null)
                {
                    img.color = originalImage.color;
                    img.sprite = originalImage.sprite;
                }
                target = go.GetComponent<Button>();
                target.targetGraphic = img;
                GameObject textGO = new GameObject("Text", typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                textGO.transform.SetParent(go.transform, false);
                var rect = (RectTransform)textGO.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var text = textGO.GetComponent<TextMeshProUGUI>();
                var sample = template.GetComponentInChildren<TMP_Text>(true);
                if (sample != null)
                {
                    text.font = sample.font;
                    text.fontSize = sample.fontSize;
                    text.color = sample.color;
                }
                text.text = "ОСОБЫЕ МИРЫ";
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }
            if (target != null)
            {
                target.onClick = new Button.ButtonClickedEvent();
                target.onClick.AddListener(OpenSpecialWorlds);
            }
        }

        public void OpenSpecialWorlds()
        {
            PlayerAppearanceMenu.Close();
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            Game.SpecialWorlds.SpecialWorldsMenu.Open(this, homePanel, ShowHome);
        }

        private void EnsureMenuAudioListener()
        {
            // The existing menu scene wizard does not always create a listener.
            // Background music can continue playing silently without one.
            foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                    return;
            Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (camera != null)
            {
                AudioListener listener = camera.GetComponent<AudioListener>();
                if (listener == null) camera.gameObject.AddComponent<AudioListener>();
                else listener.enabled = true;
            }
        }
        private void EnsureCharacterEditorButton()
        {
            if (homePanel == null) return;
            // Support existing main menu scenes without editing their prefab/YAML.
            Button[] buttons = homePanel.GetComponentsInChildren<Button>(true);
            Button settings = null;
            foreach (Button b in buttons)
            {
                if (b.name == "SettingsButton") { settings = b; break; }
                var buttonText = b.GetComponentInChildren<TMP_Text>(true);
                if (buttonText != null && buttonText.text.IndexOf("НАСТРОЙКИ", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    settings = b;
            }
            if (settings == null) return;
            Transform parent = settings.transform.parent;
            if (parent.Find("CharacterEditorButton") != null) return;
            GameObject go = new GameObject("CharacterEditorButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(settings.transform.GetSiblingIndex() + 1);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = ((RectTransform)settings.transform).sizeDelta;
            Image image = go.GetComponent<Image>();
            Image sample = settings.GetComponent<Image>();
            if (sample != null) { image.sprite = sample.sprite; image.color = sample.color; }
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OpenCharacterEditor);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1, 1, 1, .25f);
            outline.effectDistance = new Vector2(1f, -1f);
            var layout = parent.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                RectTransform parentRect = parent as RectTransform;
                if (parentRect != null && parentRect.sizeDelta.y < 370f)
                    parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, 370f);
            }
            var original = settings.GetComponentInChildren<TMP_Text>();
            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            if (original != null)
            {
                text.fontSize = original.fontSize;
                text.color = original.color;
            }
            text.text = "РЕДАКТОР";
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
        public void OpenCharacterEditor()
        {
            if (homePanel != null) homePanel.SetActive(false);
            if (saveSelectionPanel != null) saveSelectionPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (infoPanel != null) infoPanel.SetActive(false);
            PlayerAppearanceMenu.Open(ShowHome);
        }


        public void Play()
        {
            PlayerAppearanceMenu.Close();
            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    false
                );
            }


            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    true
                );
            }


            saveSelection?.Refresh();
        }


        public void OpenSettings()
        {
            PlayerAppearanceMenu.Close();
            Game.UI.Settings.SettingsTabsController.Open(ShowHome);

            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    false
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    false
                );
            }


            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        public void OpenInfo()
        {
            PlayerAppearanceMenu.Close();
            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    false
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    false
                );
            }


            if (
                infoPanel != null
            )
            {
                infoPanel.SetActive(
                    true
                );
            }
        }
        public void ShowHome()
        {
            PlayerAppearanceMenu.Close();
            Game.UI.Settings.SettingsTabsController.Close();

            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    true
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    false
                );
            }


            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }

            if (
                infoPanel != null
            )
            {
                infoPanel.SetActive(
                    false
                );
            }
        }


        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication
                .isPlaying =
                false;
#else
            Application.Quit();
#endif
        }
    }
}
