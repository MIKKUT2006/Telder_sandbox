#if UNITY_EDITOR

using TMPro;

using UnityEditor;
using UnityEditor.Events;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Game.UI;
using Game.UI.Pause;


namespace Game.EditorTools
{

    public static class PauseUISetupWizard
    {

        // =====================================================
        // MENU
        // =====================================================

        [MenuItem(
            "Tools/Game/Create Pause UI"
        )]
        public static void CreatePauseUI()
        {

            // =================================================
            // FIND / CREATE CANVAS
            // =================================================

            Canvas canvas =
                Object.FindFirstObjectByType<
                    Canvas
                >();


            if (
                canvas == null
            )
            {

                canvas =
                    CreateCanvas();

            }


            EnsureEventSystem();


            // =================================================
            // OLD PAUSE UI
            // =================================================

            GameObject oldRoot =
                GameObject.Find(
                    "PauseSystem"
                );


            if (
                oldRoot != null
            )
            {

                bool replace =
                    EditorUtility.DisplayDialog(
                        "Pause UI",
                        "PauseSystem уже существует.\n\nУдалить его и создать заново?",
                        "Создать заново",
                        "Отмена"
                    );


                if (
                    !replace
                )
                {

                    return;

                }


                Object.DestroyImmediate(
                    oldRoot
                );

            }


            // =================================================
            // ROOT
            // =================================================

            GameObject root =
                CreateRectObject(
                    "PauseSystem",
                    canvas.transform
                );


            RectTransform rootRect =
                root.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                rootRect
            );


            PauseMenuController controller =
                root.AddComponent<
                    PauseMenuController
                >();


            // =================================================
            // PAUSE PANEL
            // =================================================

            GameObject pausePanel =
                CreateRectObject(
                    "PausePanel",
                    root.transform
                );


            RectTransform pausePanelRect =
                pausePanel.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                pausePanelRect
            );


            // Лёгкое затемнение всего экрана.
            Image dim =
                pausePanel.AddComponent<
                    Image
                >();


            dim.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.42f
                );


            // =================================================
            // RIGHT PANEL
            // =================================================

            GameObject rightPanel =
                CreateRectObject(
                    "RightButtons",
                    pausePanel.transform
                );


            RectTransform rightRect =
                rightPanel.GetComponent<
                    RectTransform
                >();


            rightRect.anchorMin =
                new Vector2(
                    1f,
                    0.5f
                );


            rightRect.anchorMax =
                new Vector2(
                    1f,
                    0.5f
                );


            rightRect.pivot =
                new Vector2(
                    1f,
                    0.5f
                );


            rightRect.anchoredPosition =
                new Vector2(
                    -48f,
                    0f
                );


            rightRect.sizeDelta =
                new Vector2(
                    360f,
                    360f
                );


            VerticalLayoutGroup layout =
                rightPanel.AddComponent<
                    VerticalLayoutGroup
                >();


            layout.spacing =
                18f;


            layout.childAlignment =
                TextAnchor.MiddleCenter;


            layout.childControlWidth =
                false;


            layout.childControlHeight =
                false;


            layout.childForceExpandWidth =
                false;


            layout.childForceExpandHeight =
                false;


            // =================================================
            // TITLE
            // =================================================

            GameObject titleObject =
                CreateRectObject(
                    "PauseTitle",
                    rightPanel.transform
                );


            RectTransform titleRect =
                titleObject.GetComponent<
                    RectTransform
                >();


            titleRect.sizeDelta =
                new Vector2(
                    330f,
                    58f
                );


            TMP_Text title =
                titleObject.AddComponent<
                    TextMeshProUGUI
                >();


            title.text =
                "ПАУЗА";


            title.fontSize =
                34f;


            title.alignment =
                TextAlignmentOptions.Center;


            title.raycastTarget =
                false;


            // =================================================
            // BUTTONS
            // =================================================

            Button continueButton =
                CreateButton(
                    "ContinueButton",
                    "ПРОДОЛЖИТЬ",
                    rightPanel.transform
                );


            Button settingsButton =
                CreateButton(
                    "SettingsButton",
                    "НАСТРОЙКИ",
                    rightPanel.transform
                );


            Button mainMenuButton =
                CreateButton(
                    "MainMenuButton",
                    "В ГЛАВНОЕ МЕНЮ",
                    rightPanel.transform
                );


            UnityEventTools
                .AddPersistentListener(
                    continueButton.onClick,
                    controller.ContinueGame
                );


            UnityEventTools
                .AddPersistentListener(
                    settingsButton.onClick,
                    controller.OpenSettings
                );


            UnityEventTools
                .AddPersistentListener(
                    mainMenuButton.onClick,
                    controller.ExitToMainMenu
                );


            // =================================================
            // SETTINGS PANEL
            // =================================================

            GameObject settingsPanel =
                CreateRectObject(
                    "SettingsPanel",
                    root.transform
                );


            RectTransform settingsRect =
                settingsPanel.GetComponent<
                    RectTransform
                >();


            settingsRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsRect.sizeDelta =
                new Vector2(
                    620f,
                    420f
                );


            Image settingsBackground =
                settingsPanel.AddComponent<
                    Image
                >();


            settingsBackground.color =
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.96f
                );


            Outline settingsOutline =
                settingsPanel.AddComponent<
                    Outline
                >();


            settingsOutline.effectColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.22f
                );


            settingsOutline.effectDistance =
                new Vector2(
                    1f,
                    -1f
                );


            GameObject settingsTitleObject =
                CreateRectObject(
                    "Title",
                    settingsPanel.transform
                );


            RectTransform settingsTitleRect =
                settingsTitleObject.GetComponent<
                    RectTransform
                >();


            settingsTitleRect.anchorMin =
                new Vector2(
                    0.5f,
                    1f
                );


            settingsTitleRect.anchorMax =
                new Vector2(
                    0.5f,
                    1f
                );


            settingsTitleRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );


            settingsTitleRect.anchoredPosition =
                new Vector2(
                    0f,
                    -28f
                );


            settingsTitleRect.sizeDelta =
                new Vector2(
                    500f,
                    60f
                );


            TMP_Text settingsTitle =
                settingsTitleObject.AddComponent<
                    TextMeshProUGUI
                >();


            settingsTitle.text =
                "НАСТРОЙКИ";


            settingsTitle.fontSize =
                30f;


            settingsTitle.alignment =
                TextAlignmentOptions.Center;


            settingsTitle.raycastTarget =
                false;


            GameObject placeholderObject =
                CreateRectObject(
                    "PlaceholderText",
                    settingsPanel.transform
                );


            RectTransform placeholderRect =
                placeholderObject.GetComponent<
                    RectTransform
                >();


            placeholderRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            placeholderRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            placeholderRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            placeholderRect.sizeDelta =
                new Vector2(
                    520f,
                    150f
                );


            TMP_Text placeholder =
                placeholderObject.AddComponent<
                    TextMeshProUGUI
                >();


            placeholder.text =
                "Здесь позже будут настройки игры";


            placeholder.fontSize =
                20f;


            placeholder.alignment =
                TextAlignmentOptions.Center;


            placeholder.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.65f
                );


            placeholder.raycastTarget =
                false;


            Button backButton =
                CreateButton(
                    "BackButton",
                    "НАЗАД",
                    settingsPanel.transform
                );


            RectTransform backRect =
                backButton.GetComponent<
                    RectTransform
                >();


            backRect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );


            backRect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );


            backRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );


            backRect.anchoredPosition =
                new Vector2(
                    0f,
                    28f
                );


            UnityEventTools
                .AddPersistentListener(
                    backButton.onClick,
                    controller.CloseSettings
                );


            // =================================================
            // WIRE CONTROLLER
            // =================================================

            SerializedObject serializedController =
                new SerializedObject(
                    controller
                );


            serializedController
                .FindProperty(
                    "pausePanel"
                )
                .objectReferenceValue =
                pausePanel;


            serializedController
                .FindProperty(
                    "settingsPanel"
                )
                .objectReferenceValue =
                settingsPanel;


            serializedController
                .FindProperty(
                    "mainMenuSceneName"
                )
                .stringValue =
                "MainMenu";


            serializedController
                .ApplyModifiedPropertiesWithoutUndo();


            // =================================================
            // INITIAL STATE
            // =================================================

            pausePanel.SetActive(
                false
            );


            settingsPanel.SetActive(
                false
            );


            Selection.activeGameObject =
                root;


            EditorUtility.SetDirty(
                root
            );


            EditorUtility.DisplayDialog(
                "Pause UI",
                "Готово.\n\nСоздано:\n• PauseSystem\n• затемнение экрана\n• правая панель\n• Продолжить\n• Настройки\n• В главное меню\n• SettingsPanel\n• кнопка Назад\n\nЗапусти Play Mode и нажми Esc.",
                "OK"
            );

        }


        // =====================================================
        // BUTTON
        // =====================================================

        private static Button CreateButton(
            string objectName,
            string text,
            Transform parent
        )
        {

            GameObject buttonObject =
                CreateRectObject(
                    objectName,
                    parent
                );


            RectTransform rect =
                buttonObject.GetComponent<
                    RectTransform
                >();


            rect.sizeDelta =
                new Vector2(
                    330f,
                    64f
                );


            Image image =
                buttonObject.AddComponent<
                    Image
                >();


            image.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.10f,
                    0.94f
                );


            Outline outline =
                buttonObject.AddComponent<
                    Outline
                >();


            outline.effectColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.24f
                );


            outline.effectDistance =
                new Vector2(
                    1f,
                    -1f
                );


            Button button =
                buttonObject.AddComponent<
                    Button
                >();


            button.targetGraphic =
                image;


            GameObject textObject =
                CreateRectObject(
                    "Text",
                    buttonObject.transform
                );


            RectTransform textRect =
                textObject.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                textRect
            );


            TMP_Text label =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();


            label.text =
                text;


            label.fontSize =
                20f;


            label.alignment =
                TextAlignmentOptions.Center;


            label.raycastTarget =
                false;


            MenuButtonVisual visual =
                buttonObject.AddComponent<
                    MenuButtonVisual
                >();


            SerializedObject serializedVisual =
                new SerializedObject(
                    visual
                );


            serializedVisual
                .FindProperty(
                    "image"
                )
                .objectReferenceValue =
                image;


            serializedVisual
                .ApplyModifiedPropertiesWithoutUndo();


            return
                button;

        }


        // =====================================================
        // CANVAS
        // =====================================================

        private static Canvas CreateCanvas()
        {

            GameObject canvasObject =
                new GameObject(
                    "GameCanvas"
                );


            Canvas canvas =
                canvasObject.AddComponent<
                    Canvas
                >();


            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;


            canvas.sortingOrder =
                2000;


            CanvasScaler scaler =
                canvasObject.AddComponent<
                    CanvasScaler
                >();


            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;


            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f
                );


            scaler.matchWidthOrHeight =
                0.5f;


            canvasObject.AddComponent<
                GraphicRaycaster
            >();


            return
                canvas;

        }


        // =====================================================
        // EVENT SYSTEM
        // =====================================================

        private static void EnsureEventSystem()
        {

            EventSystem existing =
                Object.FindFirstObjectByType<
                    EventSystem
                >();


            if (
                existing != null
            )
            {

                return;

            }


            GameObject eventSystem =
                new GameObject(
                    "EventSystem"
                );


            eventSystem.AddComponent<
                EventSystem
            >();


            eventSystem.AddComponent<
                StandaloneInputModule
            >();

        }


        // =====================================================
        // RECT HELPERS
        // =====================================================

        private static GameObject CreateRectObject(
            string name,
            Transform parent
        )
        {

            GameObject gameObject =
                new GameObject(
                    name,
                    typeof(RectTransform)
                );


            gameObject.transform.SetParent(
                parent,
                false
            );


            return
                gameObject;

        }


        private static void StretchFullScreen(
            RectTransform rect
        )
        {

            rect.anchorMin =
                Vector2.zero;


            rect.anchorMax =
                Vector2.one;


            rect.offsetMin =
                Vector2.zero;


            rect.offsetMax =
                Vector2.zero;

        }

    }

}

#endif
