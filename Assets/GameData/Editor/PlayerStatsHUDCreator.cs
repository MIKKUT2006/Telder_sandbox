#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayerStatsComponent = Game.PlayerStats.PlayerStats;
using PlayerStatsHUDComponent = Game.PlayerStats.PlayerStatsHUD;

namespace Game.EditorTools
{
    public static class PlayerStatsHUDCreator
    {
        private const string HudName = "PlayerStatsHUD";

        private const float DefaultTotalWidth = 448f;
        private const float MinTotalWidth = 320f;
        private const float MaxTotalWidth = 640f;

        private const float BarHeight = 18f;
        private const float Gap = 8f;
        private const float FrameThickness = 2f;
        private const float DistanceAboveHotbar = 10f;

        private static readonly Color HealthColor =
            new Color32(140, 14, 26, 255); // #8C0E1A

        private static readonly Color HungerColor =
            new Color32(226, 130, 91, 255); // #E2825B

        private static readonly Color FrameColor =
            new Color32(255, 255, 255, 255);

        [MenuItem(
            "Tools/Game/Player Stats/Create or Repair HUD",
            priority = 2200
        )]
        public static void CreateOrRepairHUD()
        {
            Canvas canvas = FindBestCanvas();

            if (canvas == null)
                canvas = CreateCanvas();

            RectTransform canvasRect =
                canvas.transform as RectTransform;

            if (canvasRect == null)
            {
                Debug.LogError(
                    "PLAYER STATS HUD: Canvas has no RectTransform."
                );

                return;
            }

            RectTransform hotbar =
                FindHotbarUnderCanvas(canvas);

            float totalWidth =
                CalculateHudWidth(hotbar);

            float barWidth =
                (totalWidth - Gap) * 0.5f;

            float hudY =
                CalculateHudY(hotbar);

            RectTransform hudRect =
                GetOrCreateRect(
                    canvas.transform,
                    HudName
                );

            Undo.RecordObject(
                hudRect,
                "Setup Player Stats HUD"
            );

            hudRect.anchorMin =
                new Vector2(0.5f, 0f);

            hudRect.anchorMax =
                new Vector2(0.5f, 0f);

            hudRect.pivot =
                new Vector2(0.5f, 0f);

            hudRect.sizeDelta =
                new Vector2(
                    totalWidth,
                    BarHeight
                );

            hudRect.anchoredPosition =
                new Vector2(
                    0f,
                    hudY
                );

            PlayerStatsHUDComponent hud =
                GetOrAddComponent<PlayerStatsHUDComponent>(
                    hudRect.gameObject
                );

            RectTransform healthBar =
                GetOrCreateRect(
                    hudRect,
                    "HealthBar"
                );

            RectTransform hungerBar =
                GetOrCreateRect(
                    hudRect,
                    "HungerBar"
                );

            SetupBarRect(
                healthBar,
                left: true,
                barWidth: barWidth
            );

            SetupBarRect(
                hungerBar,
                left: false,
                barWidth: barWidth
            );

            Sprite defaultSprite =
                AssetDatabase
                    .GetBuiltinExtraResource<Sprite>(
                        "UI/Skin/UISprite.psd"
                    );

            Image healthFrame =
                SetupFrame(
                    healthBar,
                    defaultSprite,
                    FrameColor
                );

            Image healthFill =
                SetupFill(
                    healthBar,
                    defaultSprite,
                    HealthColor
                );

            Image hungerFrame =
                SetupFrame(
                    hungerBar,
                    defaultSprite,
                    FrameColor
                );

            Image hungerFill =
                SetupFill(
                    hungerBar,
                    defaultSprite,
                    HungerColor
                );

            PlayerStatsComponent playerStats =
                FindScenePlayerStats();

            BindHud(
                hud,
                playerStats,
                healthFill,
                healthFrame,
                hungerFill,
                hungerFrame,
                defaultSprite
            );

            EnsureCanvasSettings(canvas);

            Selection.activeGameObject =
                hudRect.gameObject;

            EditorGUIUtility.PingObject(
                hudRect.gameObject
            );

            EditorUtility.SetDirty(
                hudRect.gameObject
            );

            if (canvas.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement
                    .EditorSceneManager
                    .MarkSceneDirty(
                        canvas.gameObject.scene
                    );
            }

            if (playerStats == null)
            {
                Debug.LogWarning(
                    "PLAYER STATS HUD: HUD created, but no PlayerStats " +
                    "component was found in the current scene. " +
                    "Assign Player Stats in the PlayerStatsHUD Inspector."
                );
            }
            else
            {
                Debug.Log(
                    "PLAYER STATS HUD: HUD created and connected successfully."
                );
            }
        }


        // =====================================================
        // CANVAS
        // =====================================================

        private static Canvas FindBestCanvas()
        {
            Canvas[] canvases =
                global::UnityEngine.Resources.FindObjectsOfTypeAll<Canvas>();

            Canvas best =
                canvases
                    .Where(IsSceneObject)
                    .Where(c => c.renderMode != RenderMode.WorldSpace)
                    .OrderByDescending(c => c.isActiveAndEnabled)
                    .FirstOrDefault();

            return best;
        }

        private static Canvas CreateCanvas()
        {
            GameObject go =
                new GameObject(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

            Undo.RegisterCreatedObjectUndo(
                go,
                "Create UI Canvas"
            );

            Canvas canvas =
                go.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                go.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f
                );

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode
                    .MatchWidthOrHeight;

            scaler.matchWidthOrHeight =
                0.5f;

            EnsureEventSystem();

            return canvas;
        }

        private static void EnsureCanvasSettings(
            Canvas canvas)
        {
            CanvasScaler scaler =
                canvas.GetComponent<CanvasScaler>();

            if (scaler == null)
                scaler =
                    Undo.AddComponent<CanvasScaler>(
                        canvas.gameObject
                    );

            if (canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                scaler.uiScaleMode =
                    CanvasScaler.ScaleMode
                        .ScaleWithScreenSize;

                if (scaler.referenceResolution.x <= 0f ||
                    scaler.referenceResolution.y <= 0f)
                {
                    scaler.referenceResolution =
                        new Vector2(
                            1920f,
                            1080f
                        );
                }
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(
                    canvas.gameObject
                );
            }

            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            EventSystem[] systems =
                global::UnityEngine.Resources
                    .FindObjectsOfTypeAll<EventSystem>();

            bool exists =
                systems.Any(IsSceneObject);

            if (exists)
                return;

            GameObject eventSystem =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );

            Undo.RegisterCreatedObjectUndo(
                eventSystem,
                "Create EventSystem"
            );
        }


        // =====================================================
        // HOTBAR SEARCH
        // =====================================================

        private static RectTransform FindHotbarUnderCanvas(
            Canvas canvas)
        {
            RectTransform[] rects =
                canvas
                    .GetComponentsInChildren<RectTransform>(
                        true
                    );

            RectTransform exact =
                rects
                    .Where(r => r != null)
                    .Where(r => r != canvas.transform)
                    .FirstOrDefault(
                        r =>
                        {
                            string n =
                                NormalizeName(
                                    r.name
                                );

                            return
                                n == "hotbar" ||
                                n == "hotbarui" ||
                                n == "hotbarcontainer" ||
                                n == "hotbarpanel";
                        }
                    );

            if (exact != null)
                return exact;

            RectTransform candidate =
                rects
                    .Where(r => r != null)
                    .Where(r => r != canvas.transform)
                    .Where(
                        r =>
                            NormalizeName(
                                r.name
                            )
                            .Contains("hotbar")
                    )
                    .OrderByDescending(
                        r =>
                            Mathf.Abs(
                                r.rect.width
                            )
                    )
                    .FirstOrDefault();

            return candidate;
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
        }

        private static float CalculateHudWidth(
            RectTransform hotbar)
        {
            if (hotbar == null)
                return DefaultTotalWidth;

            Canvas.ForceUpdateCanvases();

            float hotbarWidth =
                Mathf.Abs(
                    hotbar.rect.width
                );

            if (hotbarWidth < 50f)
                return DefaultTotalWidth;

            return Mathf.Clamp(
                hotbarWidth,
                MinTotalWidth,
                MaxTotalWidth
            );
        }

        private static float CalculateHudY(
            RectTransform hotbar)
        {
            if (hotbar == null)
                return 92f;

            Canvas.ForceUpdateCanvases();

            // Best result for the usual bottom-anchored hotbar.
            if (hotbar.anchorMin.y < 0.25f &&
                hotbar.anchorMax.y < 0.25f)
            {
                float top =
                    hotbar.anchoredPosition.y +
                    hotbar.rect.height *
                    (1f - hotbar.pivot.y);

                return
                    top +
                    DistanceAboveHotbar;
            }

            // Safe fallback if the existing hotbar uses unusual anchors.
            return 92f;
        }


        // =====================================================
        // HUD CONSTRUCTION
        // =====================================================

        private static void SetupBarRect(
            RectTransform rect,
            bool left,
            float barWidth)
        {
            Undo.RecordObject(
                rect,
                "Setup Player Stat Bar"
            );

            rect.anchorMin =
                left
                    ? new Vector2(0f, 0f)
                    : new Vector2(1f, 0f);

            rect.anchorMax =
                rect.anchorMin;

            rect.pivot =
                left
                    ? new Vector2(0f, 0f)
                    : new Vector2(1f, 0f);

            rect.sizeDelta =
                new Vector2(
                    barWidth,
                    BarHeight
                );

            rect.anchoredPosition =
                Vector2.zero;
        }

        private static Image SetupFrame(
            RectTransform bar,
            Sprite sprite,
            Color color)
        {
            RectTransform rect =
                GetOrCreateRect(
                    bar,
                    "Frame"
                );

            StretchRect(
                rect,
                0f
            );

            Image image =
                GetOrAddComponent<Image>(
                    rect.gameObject
                );

            Undo.RecordObject(
                image,
                "Setup Player Stat Frame"
            );

            image.sprite =
                sprite;

            image.color =
                color;

            image.type =
                sprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;

            image.raycastTarget =
                false;

            // Frame must be behind the fill.
            rect.SetSiblingIndex(0);

            return image;
        }

        private static Image SetupFill(
            RectTransform bar,
            Sprite sprite,
            Color color)
        {
            RectTransform rect =
                GetOrCreateRect(
                    bar,
                    "Fill"
                );

            StretchRect(
                rect,
                FrameThickness
            );

            Image image =
                GetOrAddComponent<Image>(
                    rect.gameObject
                );

            Undo.RecordObject(
                image,
                "Setup Player Stat Fill"
            );

            image.sprite =
                sprite;

            image.color =
                color;

            image.type =
                Image.Type.Filled;

            image.fillMethod =
                Image.FillMethod.Horizontal;

            image.fillOrigin =
                (int)Image.OriginHorizontal.Left;

            image.fillClockwise =
                true;

            image.fillAmount =
                1f;

            image.raycastTarget =
                false;

            // Fill is drawn above the solid frame background,
            // leaving FrameThickness visible around the edges.
            rect.SetSiblingIndex(1);

            return image;
        }

        private static void StretchRect(
            RectTransform rect,
            float inset)
        {
            Undo.RecordObject(
                rect,
                "Stretch UI Rect"
            );

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.offsetMin =
                new Vector2(
                    inset,
                    inset
                );

            rect.offsetMax =
                new Vector2(
                    -inset,
                    -inset
                );
        }


        // =====================================================
        // PLAYER STATS HUD BINDING
        // =====================================================

        private static void BindHud(
            PlayerStatsHUDComponent hud,
            PlayerStatsComponent playerStats,
            Image healthFill,
            Image healthFrame,
            Image hungerFill,
            Image hungerFrame,
            Sprite defaultSprite)
        {
            SerializedObject so =
                new SerializedObject(hud);

            SetObjectReference(
                so,
                "playerStats",
                playerStats
            );

            SetObjectReference(
                so,
                "healthFill",
                healthFill
            );

            SetObjectReference(
                so,
                "healthFrame",
                healthFrame
            );

            SetObjectReference(
                so,
                "healthFillTexture",
                defaultSprite
            );

            SetObjectReference(
                so,
                "healthFrameTexture",
                defaultSprite
            );

            SerializedProperty healthColor =
                so.FindProperty(
                    "healthColor"
                );

            if (healthColor != null)
                healthColor.colorValue =
                    HealthColor;

            SerializedProperty healthFrameColor =
                so.FindProperty(
                    "healthFrameColor"
                );

            if (healthFrameColor != null)
                healthFrameColor.colorValue =
                    FrameColor;

            SetObjectReference(
                so,
                "hungerFill",
                hungerFill
            );

            SetObjectReference(
                so,
                "hungerFrame",
                hungerFrame
            );

            SetObjectReference(
                so,
                "hungerFillTexture",
                defaultSprite
            );

            SetObjectReference(
                so,
                "hungerFrameTexture",
                defaultSprite
            );

            SerializedProperty hungerColor =
                so.FindProperty(
                    "hungerColor"
                );

            if (hungerColor != null)
                hungerColor.colorValue =
                    HungerColor;

            SerializedProperty hungerFrameColor =
                so.FindProperty(
                    "hungerFrameColor"
                );

            if (hungerFrameColor != null)
                hungerFrameColor.colorValue =
                    FrameColor;

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(hud);
        }

        private static void SetObjectReference(
            SerializedObject so,
            string propertyName,
            Object value)
        {
            SerializedProperty property =
                so.FindProperty(
                    propertyName
                );

            if (property != null)
                property.objectReferenceValue =
                    value;
        }


        // =====================================================
        // HELPERS
        // =====================================================

        private static PlayerStatsComponent FindScenePlayerStats()
        {
            PlayerStatsComponent[] all =
                global::UnityEngine.Resources
                    .FindObjectsOfTypeAll<PlayerStatsComponent>();

            return all
                .FirstOrDefault(IsSceneObject);
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string objectName)
        {
            Transform existing =
                parent.Find(
                    objectName
                );

            if (existing != null)
            {
                RectTransform existingRect =
                    existing
                        .GetComponent<RectTransform>();

                if (existingRect != null)
                    return existingRect;
            }

            GameObject go =
                new GameObject(
                    objectName,
                    typeof(RectTransform)
                );

            Undo.RegisterCreatedObjectUndo(
                go,
                "Create " + objectName
            );

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.SetParent(
                parent,
                false
            );

            return rect;
        }

        private static T GetOrAddComponent<T>(
            GameObject go)
            where T : Component
        {
            T component =
                go.GetComponent<T>();

            if (component != null)
                return component;

            return Undo.AddComponent<T>(go);
        }

        private static bool IsSceneObject(
            Component component)
        {
            if (component == null)
                return false;

            GameObject go =
                component.gameObject;

            if (go == null)
                return false;

            Scene scene =
                go.scene;

            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                return false;
            }

            return
                (go.hideFlags &
                 HideFlags.HideAndDontSave) == 0;
        }
    }
}

#endif
