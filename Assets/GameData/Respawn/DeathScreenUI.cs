using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Pause;

namespace Game.GameplaySystems.Respawn
{
    /// <summary>
    /// Death UI intentionally reuses the live PausePanel visual hierarchy when
    /// available. Any custom sprites, colors, font and MenuButtonVisual setup on
    /// the pause menu therefore automatically carry over to the death screen.
    /// </summary>
    public static class DeathScreenUI
    {
        private static GameObject root;
        private static GameObject toast;

        public static bool IsVisible =>
            root != null;

        public static void Show(
            MonoBehaviour owner,
            Action respawn,
            Action menu)
        {
            Hide();

            PauseMenuController pause =
                FindPauseController();

            if (pause != null &&
                pause.PausePanel != null &&
                TryBuildFromPause(
                    pause.PausePanel,
                    respawn,
                    menu
                ))
            {
                return;
            }

            BuildPauseStyleFallback(
                respawn,
                menu,
                pause != null
                    ? pause.PixelFont
                    : null
            );
        }

        public static void Hide()
        {
            if (root != null)
                UnityEngine.Object.Destroy(root);

            root = null;
        }

        public static void ShowToast(string message)
        {
            if (toast != null)
                UnityEngine.Object.Destroy(toast);

            Canvas canvas =
                FindBestCanvas();

            if (canvas == null)
                return;

            toast =
                new GameObject(
                    "GameplayToast",
                    typeof(RectTransform),
                    typeof(CanvasGroup)
                );

            toast.transform.SetParent(
                canvas.transform,
                false
            );

            RectTransform rootRect =
                toast.GetComponent<RectTransform>();

            rootRect.anchorMin =
                new Vector2(0.5f, 0.15f);

            rootRect.anchorMax =
                new Vector2(0.5f, 0.15f);

            rootRect.pivot =
                new Vector2(0.5f, 0.5f);

            rootRect.sizeDelta =
                new Vector2(620f, 58f);

            Image background =
                toast.AddComponent<Image>();

            background.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.10f,
                    0.90f
                );

            Outline outline =
                toast.AddComponent<Outline>();

            outline.effectColor =
                new Color(1f, 1f, 1f, 0.20f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            GameObject textObject =
                CreateRect(
                    "Text",
                    toast.transform
                );

            Stretch(textObject.GetComponent<RectTransform>());

            TMP_Text text =
                textObject.AddComponent<TextMeshProUGUI>();

            PauseMenuController pause =
                FindPauseController();

            if (pause != null && pause.PixelFont != null)
                text.font = pause.PixelFont;

            text.text = message;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            UnityEngine.Object.Destroy(
                toast,
                2f
            );
        }

        private static bool TryBuildFromPause(
            GameObject pausePanel,
            Action respawn,
            Action menu)
        {
            if (pausePanel == null)
                return false;

            Transform rightTemplate =
                FindDeepChild(
                    pausePanel.transform,
                    "RightButtons"
                );

            if (rightTemplate == null)
                return false;

            Transform parent =
                pausePanel.transform.parent;

            if (parent == null)
                return false;

            root =
                CreateRect(
                    "DeathScreen",
                    parent
                );

            RectTransform rootRect =
                root.GetComponent<RectTransform>();

            Stretch(rootRect);
            root.transform.SetAsLastSibling();

            // Copy the same full-screen shade sprite/color used by pause.
            Image sourceShade =
                pausePanel.GetComponent<Image>();

            Image shade =
                root.AddComponent<Image>();

            if (sourceShade != null)
            {
                shade.sprite = sourceShade.sprite;
                shade.color = sourceShade.color;
                shade.type = sourceShade.type;
                shade.material = sourceShade.material;
            }
            else
            {
                shade.color =
                    new Color(0f, 0f, 0f, 0.42f);
            }

            GameObject right =
                UnityEngine.Object.Instantiate(
                    rightTemplate.gameObject,
                    root.transform,
                    false
                );

            right.name =
                "DeathButtons";

            Transform title =
                FindDeepChild(
                    right.transform,
                    "PauseTitle"
                );

            if (title != null)
            {
                TMP_Text titleText =
                    title.GetComponent<TMP_Text>();

                if (titleText != null)
                    titleText.text = "ВЫ УМЕРЛИ";
            }

            Transform respawnButton =
                FindDeepChild(
                    right.transform,
                    "ContinueButton"
                );

            Transform settingsButton =
                FindDeepChild(
                    right.transform,
                    "SettingsButton"
                );

            Transform menuButton =
                FindDeepChild(
                    right.transform,
                    "MainMenuButton"
                );

            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);

            // Runtime systems may append extra pause-menu buttons (for example
            // achievements). Death intentionally keeps only Respawn + Main Menu.
            Button[] clonedButtons =
                right.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < clonedButtons.Length; i++)
            {
                Button candidate = clonedButtons[i];
                if (candidate == null)
                    continue;

                string objectName =
                    candidate.gameObject.name;

                if (!string.Equals(
                        objectName,
                        "ContinueButton",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        objectName,
                        "MainMenuButton",
                        StringComparison.OrdinalIgnoreCase))
                {
                    candidate.gameObject.SetActive(false);
                }
            }

            if (respawnButton == null || menuButton == null)
            {
                UnityEngine.Object.Destroy(root);
                root = null;
                return false;
            }

            RebindButton(
                respawnButton.gameObject,
                "ВОЗРОДИТЬСЯ",
                respawn
            );

            RebindButton(
                menuButton.gameObject,
                "В ГЛАВНОЕ МЕНЮ",
                menu
            );

            root.SetActive(true);
            return true;
        }

        private static void RebindButton(
            GameObject buttonObject,
            string caption,
            Action action)
        {
            if (buttonObject == null)
                return;

            TMP_Text label =
                buttonObject.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = caption;

            Button button =
                buttonObject.GetComponent<Button>();

            if (button == null)
                button = buttonObject.AddComponent<Button>();

            // Replacing the event object also removes cloned persistent scene
            // listeners. This prevents the death button from calling the old
            // PauseMenuController action inherited from PausePanel.
            button.onClick =
                new Button.ButtonClickedEvent();

            if (action != null)
            {
                button.onClick.AddListener(
                    () => action()
                );
            }
        }

        private static void BuildPauseStyleFallback(
            Action respawn,
            Action menu,
            TMP_FontAsset font)
        {
            Canvas canvas =
                FindBestCanvas();

            if (canvas == null)
            {
                GameObject canvasObject =
                    new GameObject(
                        "DeathCanvas",
                        typeof(Canvas),
                        typeof(CanvasScaler),
                        typeof(GraphicRaycaster)
                    );

                canvas =
                    canvasObject.GetComponent<Canvas>();

                canvas.renderMode =
                    RenderMode.ScreenSpaceOverlay;

                canvas.sortingOrder =
                    30000;

                CanvasScaler scaler =
                    canvasObject.GetComponent<CanvasScaler>();

                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;

                scaler.referenceResolution =
                    new Vector2(1920f, 1080f);
            }

            root =
                CreateRect(
                    "DeathScreen",
                    canvas.transform
                );

            Stretch(
                root.GetComponent<RectTransform>()
            );

            root.transform.SetAsLastSibling();

            Image shade =
                root.AddComponent<Image>();

            shade.color =
                new Color(0f, 0f, 0f, 0.42f);

            GameObject right =
                CreateRect(
                    "DeathButtons",
                    root.transform
                );

            RectTransform rightRect =
                right.GetComponent<RectTransform>();

            rightRect.anchorMin =
                new Vector2(1f, 0.5f);

            rightRect.anchorMax =
                new Vector2(1f, 0.5f);

            rightRect.pivot =
                new Vector2(1f, 0.5f);

            rightRect.anchoredPosition =
                new Vector2(-48f, 0f);

            rightRect.sizeDelta =
                new Vector2(360f, 280f);

            VerticalLayoutGroup layout =
                right.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateTitle(
                right.transform,
                "ВЫ УМЕРЛИ",
                font
            );

            CreateFallbackButton(
                right.transform,
                "ВОЗРОДИТЬСЯ",
                font,
                respawn
            );

            CreateFallbackButton(
                right.transform,
                "В ГЛАВНОЕ МЕНЮ",
                font,
                menu
            );
        }

        private static void CreateTitle(
            Transform parent,
            string value,
            TMP_FontAsset font)
        {
            GameObject go =
                CreateRect(
                    "DeathTitle",
                    parent
                );

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.sizeDelta =
                new Vector2(330f, 58f);

            TMP_Text text =
                go.AddComponent<TextMeshProUGUI>();

            if (font != null)
                text.font = font;

            text.text = value;
            text.fontSize = 34f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static void CreateFallbackButton(
            Transform parent,
            string value,
            TMP_FontAsset font,
            Action action)
        {
            GameObject go =
                CreateRect(
                    value,
                    parent
                );

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.sizeDelta =
                new Vector2(330f, 64f);

            Image image =
                go.AddComponent<Image>();

            image.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.10f,
                    0.94f
                );

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(1f, 1f, 1f, 0.24f);

            outline.effectDistance =
                new Vector2(1f, -1f);

            Button button =
                go.AddComponent<Button>();

            if (action != null)
                button.onClick.AddListener(() => action());

            GameObject textObject =
                CreateRect(
                    "Text",
                    go.transform
                );

            Stretch(
                textObject.GetComponent<RectTransform>()
            );

            TMP_Text text =
                textObject.AddComponent<TextMeshProUGUI>();

            if (font != null)
                text.font = font;

            text.text = value;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static PauseMenuController FindPauseController()
        {
            PauseMenuController[] all =
                UnityEngine.Resources.FindObjectsOfTypeAll<PauseMenuController>();

            for (int i = 0; i < all.Length; i++)
            {
                PauseMenuController candidate = all[i];

                if (candidate != null &&
                    candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Canvas FindBestCanvas()
        {
            Canvas[] canvases =
                UnityEngine.Resources.FindObjectsOfTypeAll<Canvas>();

            Canvas best = null;
            int bestOrder = int.MinValue;

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];

                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (candidate.sortingOrder > bestOrder)
                {
                    best = candidate;
                    bestOrder = candidate.sortingOrder;
                }
            }

            return best;
        }

        private static Transform FindDeepChild(
            Transform rootTransform,
            string name)
        {
            if (rootTransform == null)
                return null;

            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform child =
                    rootTransform.GetChild(i);

                if (string.Equals(
                    child.name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform nested =
                    FindDeepChild(
                        child,
                        name
                    );

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static GameObject CreateRect(
            string name,
            Transform parent)
        {
            GameObject go =
                new GameObject(
                    name,
                    typeof(RectTransform)
                );

            go.transform.SetParent(
                parent,
                false
            );

            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
