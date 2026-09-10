#if UNITY_EDITOR

using TMPro;

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Game.UI;
using Game.UI.MainMenu;


namespace Game.EditorTools
{

    public static class MainMenuSceneSetupWizard
    {

        // =====================================================
        // MENU
        // =====================================================

        [MenuItem(
            "Tools/Game/Create Main Menu Scene"
        )]
        public static void CreateMainMenuScene()
        {

            // =================================================
            // SAVE CURRENT SCENE IF NEEDED
            // =================================================

            if (
                !EditorSceneManager
                    .SaveCurrentModifiedScenesIfUserWantsTo()
            )
            {

                return;

            }


            // =================================================
            // CHOOSE SCENE PATH
            // =================================================

            string scenePath =
                EditorUtility
                    .SaveFilePanelInProject(
                        "Create Main Menu Scene",
                        "MainMenu",
                        "unity",
                        "Выбери папку, куда сохранить сцену главного меню."
                    );


            if (
                string.IsNullOrWhiteSpace(
                    scenePath
                )
            )
            {

                return;

            }


            // =================================================
            // CREATE EMPTY SCENE
            // =================================================

            Scene scene =
                EditorSceneManager
                    .NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single
                    );


            // =================================================
            // CAMERA
            // =================================================

            GameObject cameraObject =
                new GameObject(
                    "MainMenuCamera"
                );


            Camera camera =
                cameraObject.AddComponent<
                    Camera
                >();


            cameraObject.transform.position =
                new Vector3(
                    0f,
                    0f,
                    -10f
                );


            cameraObject.transform.rotation =
                Quaternion.identity;


            camera.clearFlags =
                CameraClearFlags.SolidColor;


            camera.backgroundColor =
                Color.black;


            camera.orthographic =
                false;


            camera.fieldOfView =
                60f;


            camera.nearClipPlane =
                0.1f;


            camera.farClipPlane =
                100f;


            cameraObject.tag =
                "MainCamera";


            // =================================================
            // CANVAS
            // =================================================

            GameObject canvasObject =
                new GameObject(
                    "MainMenuCanvas"
                );


            Canvas canvas =
                canvasObject.AddComponent<
                    Canvas
                >();


            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;


            canvas.sortingOrder =
                1000;


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


            // =================================================
            // EVENT SYSTEM
            // =================================================

            GameObject eventSystemObject =
                new GameObject(
                    "EventSystem"
                );


            eventSystemObject.AddComponent<
                EventSystem
            >();


            eventSystemObject.AddComponent<
                StandaloneInputModule
            >();


            // =================================================
            // MAIN SYSTEM
            // =================================================

            GameObject systemObject =
                new GameObject(
                    "MainMenuSystem"
                );


            MainMenuController mainController =
                systemObject.AddComponent<
                    MainMenuController
                >();


            SaveSelectionController saveController =
                systemObject.AddComponent<
                    SaveSelectionController
                >();


            // =================================================
            // HOME PANEL
            // =================================================

            GameObject homePanel =
                CreateRectObject(
                    "HomePanel",
                    canvasObject.transform
                );


            StretchFullScreen(
                homePanel.GetComponent<
                    RectTransform
                >()
            );


            // Никакой непрозрачной панели:
            // фон остаётся чёрным космосом от Camera.


            GameObject leftButtons =
                CreateRectObject(
                    "LeftButtons",
                    homePanel.transform
                );


            RectTransform leftRect =
                leftButtons.GetComponent<
                    RectTransform
                >();


            leftRect.anchorMin =
                new Vector2(
                    0f,
                    0.5f
                );


            leftRect.anchorMax =
                new Vector2(
                    0f,
                    0.5f
                );


            leftRect.pivot =
                new Vector2(
                    0f,
                    0.5f
                );


            leftRect.anchoredPosition =
                new Vector2(
                    72f,
                    0f
                );


            leftRect.sizeDelta =
                new Vector2(
                    340f,
                    300f
                );


            VerticalLayoutGroup leftLayout =
                leftButtons.AddComponent<
                    VerticalLayoutGroup
                >();


            leftLayout.spacing =
                18f;


            leftLayout.childAlignment =
                TextAnchor.MiddleLeft;


            leftLayout.childControlWidth =
                false;


            leftLayout.childControlHeight =
                false;


            leftLayout.childForceExpandWidth =
                false;


            leftLayout.childForceExpandHeight =
                false;


            Button playButton =
                CreateButton(
                    "PlayButton",
                    "ИГРАТЬ",
                    leftButtons.transform,
                    320f,
                    68f
                );


            Button settingsButton =
                CreateButton(
                    "SettingsButton",
                    "НАСТРОЙКИ",
                    leftButtons.transform,
                    320f,
                    68f
                );


            Button exitButton =
                CreateButton(
                    "ExitButton",
                    "ВЫЙТИ",
                    leftButtons.transform,
                    320f,
                    68f
                );


            UnityEventTools
                .AddPersistentListener(
                    playButton.onClick,
                    mainController.Play
                );


            UnityEventTools
                .AddPersistentListener(
                    settingsButton.onClick,
                    mainController.OpenSettings
                );


            UnityEventTools
                .AddPersistentListener(
                    exitButton.onClick,
                    mainController.QuitGame
                );


            // =================================================
            // SAVE SELECTION PANEL
            // =================================================

            GameObject savePanel =
                CreateRectObject(
                    "SaveSelectionPanel",
                    canvasObject.transform
                );


            StretchFullScreen(
                savePanel.GetComponent<
                    RectTransform
                >()
            );


            // Верхний заголовок.
            TMP_Text saveTitle =
                CreateText(
                    "Title",
                    "TELDER МИРЫ",
                    savePanel.transform,
                    38f
                );


            RectTransform saveTitleRect =
                saveTitle.rectTransform;


            saveTitleRect.anchorMin =
                new Vector2(
                    0.5f,
                    1f
                );


            saveTitleRect.anchorMax =
                new Vector2(
                    0.5f,
                    1f
                );


            saveTitleRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );


            saveTitleRect.anchoredPosition =
                new Vector2(
                    0f,
                    -34f
                );


            saveTitleRect.sizeDelta =
                new Vector2(
                    700f,
                    70f
                );


            // Нижняя зона создания нового мира.
            GameObject createArea =
                CreateRectObject(
                    "CreateWorldArea",
                    savePanel.transform
                );


            RectTransform createAreaRect =
                createArea.GetComponent<
                    RectTransform
                >();


            createAreaRect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );


            createAreaRect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );


            createAreaRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );


            createAreaRect.anchoredPosition =
                new Vector2(
                    0f,
                    38f
                );


            createAreaRect.sizeDelta =
                new Vector2(
                    920f,
                    76f
                );


            HorizontalLayoutGroup createLayout =
                createArea.AddComponent<
                    HorizontalLayoutGroup
                >();


            createLayout.spacing =
                14f;


            createLayout.childAlignment =
                TextAnchor.MiddleCenter;


            createLayout.childControlWidth =
                false;


            createLayout.childControlHeight =
                false;


            createLayout.childForceExpandWidth =
                false;


            createLayout.childForceExpandHeight =
                false;


            TMP_InputField nameInput =
                CreateInputField(
                    "WorldNameInput",
                    "Название Telder мира",
                    createArea.transform
                );


            Button createButton =
                CreateButton(
                    "CreateWorldButton",
                    "СОЗДАТЬ НОВЫЙ TELDER МИР",
                    createArea.transform,
                    430f,
                    62f
                );


            UnityEventTools
                .AddPersistentListener(
                    createButton.onClick,
                    saveController
                        .CreateNewTelderWorld
                );


            Button backFromSavesButton =
                CreateButton(
                    "BackFromSavesButton",
                    "НАЗАД",
                    savePanel.transform,
                    180f,
                    54f
                );


            RectTransform backFromSavesRect =
                backFromSavesButton
                    .GetComponent<
                        RectTransform
                    >();


            backFromSavesRect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );


            backFromSavesRect.anchorMax =
                new Vector2(
                    0f,
                    1f
                );


            backFromSavesRect.pivot =
                new Vector2(
                    0f,
                    1f
                );


            backFromSavesRect.anchoredPosition =
                new Vector2(
                    36f,
                    -30f
                );


            UnityEventTools
                .AddPersistentListener(
                    backFromSavesButton.onClick,
                    mainController.ShowHome
                );


            // =================================================
            // PLANET ROOT
            // =================================================
            //
            // Это 3D-объект в мире, НЕ UI.
            // SaveSelectionController будет создавать Sphere
            // как дочерние объекты.
            //

            GameObject planetRoot =
                new GameObject(
                    "PlanetRoot"
                );


            planetRoot.transform.position =
                Vector3.zero;


            // =================================================
            // SETTINGS PANEL
            // =================================================

            GameObject settingsPanel =
                CreateRectObject(
                    "SettingsPanel",
                    canvasObject.transform
                );


            StretchFullScreen(
                settingsPanel.GetComponent<
                    RectTransform
                >()
            );


            GameObject settingsCard =
                CreateRectObject(
                    "SettingsCard",
                    settingsPanel.transform
                );


            RectTransform settingsCardRect =
                settingsCard.GetComponent<
                    RectTransform
                >();


            settingsCardRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsCardRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsCardRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsCardRect.sizeDelta =
                new Vector2(
                    700f,
                    440f
                );


            Image settingsCardImage =
                settingsCard.AddComponent<
                    Image
                >();


            settingsCardImage.color =
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.96f
                );


            Outline settingsOutline =
                settingsCard.AddComponent<
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


            TMP_Text settingsTitle =
                CreateText(
                    "Title",
                    "НАСТРОЙКИ",
                    settingsCard.transform,
                    34f
                );


            RectTransform settingsTitleRect =
                settingsTitle.rectTransform;


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
                    -34f
                );


            settingsTitleRect.sizeDelta =
                new Vector2(
                    600f,
                    60f
                );


            TMP_Text settingsPlaceholder =
                CreateText(
                    "Placeholder",
                    "Здесь позже будут настройки игры",
                    settingsCard.transform,
                    21f
                );


            RectTransform settingsPlaceholderRect =
                settingsPlaceholder.rectTransform;


            settingsPlaceholderRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsPlaceholderRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsPlaceholderRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            settingsPlaceholderRect.sizeDelta =
                new Vector2(
                    580f,
                    120f
                );


            settingsPlaceholder.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.65f
                );


            Button backFromSettingsButton =
                CreateButton(
                    "BackButton",
                    "НАЗАД",
                    settingsCard.transform,
                    220f,
                    58f
                );


            RectTransform backSettingsRect =
                backFromSettingsButton
                    .GetComponent<
                        RectTransform
                    >();


            backSettingsRect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );


            backSettingsRect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );


            backSettingsRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );


            backSettingsRect.anchoredPosition =
                new Vector2(
                    0f,
                    28f
                );


            UnityEventTools
                .AddPersistentListener(
                    backFromSettingsButton.onClick,
                    mainController.ShowHome
                );


            // =================================================
            // WIRE MAIN CONTROLLER
            // =================================================

            SerializedObject mainSerialized =
                new SerializedObject(
                    mainController
                );


            mainSerialized
                .FindProperty(
                    "homePanel"
                )
                .objectReferenceValue =
                homePanel;


            mainSerialized
                .FindProperty(
                    "saveSelectionPanel"
                )
                .objectReferenceValue =
                savePanel;


            mainSerialized
                .FindProperty(
                    "settingsPanel"
                )
                .objectReferenceValue =
                settingsPanel;


            mainSerialized
                .FindProperty(
                    "saveSelection"
                )
                .objectReferenceValue =
                saveController;


            mainSerialized
                .ApplyModifiedPropertiesWithoutUndo();


            // =================================================
            // WIRE SAVE CONTROLLER
            // =================================================

            SerializedObject saveSerialized =
                new SerializedObject(
                    saveController
                );


            saveSerialized
                .FindProperty(
                    "gameSceneName"
                )
                .stringValue =
                "Game";


            saveSerialized
                .FindProperty(
                    "planetRoot"
                )
                .objectReferenceValue =
                planetRoot.transform;


            saveSerialized
                .FindProperty(
                    "worldNameInput"
                )
                .objectReferenceValue =
                nameInput;


            saveSerialized
                .FindProperty(
                    "defaultWorldName"
                )
                .stringValue =
                "Telder мир";


            saveSerialized
                .ApplyModifiedPropertiesWithoutUndo();


            // =================================================
            // INITIAL PANEL STATE
            // =================================================

            homePanel.SetActive(
                true
            );


            savePanel.SetActive(
                false
            );


            settingsPanel.SetActive(
                false
            );


            // =================================================
            // SAVE SCENE
            // =================================================

            EditorSceneManager.SaveScene(
                scene,
                scenePath
            );


            Selection.activeGameObject =
                systemObject;


            EditorUtility.DisplayDialog(
                "Main Menu Scene",
                "Готово.\n\nСоздана полноценная сцена главного меню:\n• чёрный фон\n• Играть\n• Настройки\n• Выйти\n• экран Telder-миров\n• поле имени\n• кнопка создания мира\n• PlanetRoot для 3D-планет\n• SettingsPanel\n\nСцена сохранена:\n" +
                scenePath +
                "\n\nПроверь Game Scene Name у SaveSelectionController.",
                "OK"
            );

        }


        // =====================================================
        // BUTTON
        // =====================================================

        private static Button CreateButton(
            string objectName,
            string text,
            Transform parent,
            float width,
            float height
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
                    width,
                    height
                );


            Image image =
                buttonObject.AddComponent<
                    Image
                >();


            image.color =
                new Color(
                    0.07f,
                    0.07f,
                    0.09f,
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
                    0.25f
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


            TMP_Text label =
                CreateText(
                    "Text",
                    text,
                    buttonObject.transform,
                    20f
                );


            StretchFullScreen(
                label.rectTransform
            );


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


            return button;

        }


        // =====================================================
        // INPUT FIELD
        // =====================================================

        private static TMP_InputField CreateInputField(
            string objectName,
            string placeholderText,
            Transform parent
        )
        {

            GameObject root =
                CreateRectObject(
                    objectName,
                    parent
                );


            RectTransform rootRect =
                root.GetComponent<
                    RectTransform
                >();


            rootRect.sizeDelta =
                new Vector2(
                    430f,
                    62f
                );


            Image background =
                root.AddComponent<
                    Image
                >();


            background.color =
                new Color(
                    0.06f,
                    0.06f,
                    0.075f,
                    0.96f
                );


            Outline outline =
                root.AddComponent<
                    Outline
                >();


            outline.effectColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.22f
                );


            outline.effectDistance =
                new Vector2(
                    1f,
                    -1f
                );


            TMP_InputField input =
                root.AddComponent<
                    TMP_InputField
                >();


            // Text Area
            GameObject textArea =
                CreateRectObject(
                    "Text Area",
                    root.transform
                );


            RectTransform textAreaRect =
                textArea.GetComponent<
                    RectTransform
                >();


            textAreaRect.anchorMin =
                Vector2.zero;


            textAreaRect.anchorMax =
                Vector2.one;


            textAreaRect.offsetMin =
                new Vector2(
                    16f,
                    8f
                );


            textAreaRect.offsetMax =
                new Vector2(
                    -16f,
                    -8f
                );


            RectMask2D mask =
                textArea.AddComponent<
                    RectMask2D
                >();


            // Placeholder
            TMP_Text placeholder =
                CreateText(
                    "Placeholder",
                    placeholderText,
                    textArea.transform,
                    18f
                );


            StretchFullScreen(
                placeholder.rectTransform
            );


            placeholder.alignment =
                TextAlignmentOptions.MidlineLeft;


            placeholder.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.42f
                );


            placeholder.fontStyle =
                FontStyles.Italic;


            // Text
            TMP_Text text =
                CreateText(
                    "Text",
                    "",
                    textArea.transform,
                    18f
                );


            StretchFullScreen(
                text.rectTransform
            );


            text.alignment =
                TextAlignmentOptions.MidlineLeft;


            input.textViewport =
                textAreaRect;


            input.textComponent =
                text;


            input.placeholder =
                placeholder;


            input.lineType =
                TMP_InputField.LineType.SingleLine;


            input.characterLimit =
                32;


            return input;

        }


        // =====================================================
        // TEXT
        // =====================================================

        private static TMP_Text CreateText(
            string objectName,
            string value,
            Transform parent,
            float fontSize
        )
        {

            GameObject textObject =
                CreateRectObject(
                    objectName,
                    parent
                );


            TMP_Text text =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();


            text.text =
                value;


            text.fontSize =
                fontSize;


            text.alignment =
                TextAlignmentOptions.Center;


            text.color =
                Color.white;


            text.raycastTarget =
                false;


            return text;

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


            return gameObject;

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
