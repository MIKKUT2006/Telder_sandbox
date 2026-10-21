using System;
using System.Collections.Generic;
using Game.InputSupport;
using Game.Inventory;
using Game.Inventory.UI;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.InputSupport.Mobile
{
    /// <summary>
    /// Touchscreen-only 2D HUD. Feeds the existing GameInput actions instead of
    /// duplicating mining, combat, furniture, inventory, or pause gameplay code.
    /// Does not need to be put in a scene or on the player prefab.
    /// </summary>
    [DefaultExecutionOrder(-15000)]
    public sealed class MobileGameplayControls : MonoBehaviour
    {
        // Created before the first scene to survive menu -> world -> menu -> world.
        // The AfterSceneLoad call is a fallback for builds whose bootstrap order
        // differs; the existence check prevents a second HUD.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallEarly() { EnsureInstalled(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterScene() { EnsureInstalled(); }

        // WorldManager explicitly invokes this on every gameplay load. It also
        // prevents IL2CPP from stripping this HUD when no scene references it.
        public static void EnsureInstalled()
        {
            if (!GameInput.MobileEnabled) return;
            MobileGameplayControls existing = FindObjectOfType<MobileGameplayControls>();
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
                if (!existing.enabled) existing.enabled = true;
                return;
            }
            GameObject go = new GameObject("Telder Mobile Controls");
            DontDestroyOnLoad(go);
            go.AddComponent<MobileGameplayControls>();
            Debug.Log("TELDER MOBILE HUD V3: runtime component created (" + Application.platform + ").");
        }

        private Canvas canvas;
        private RectTransform safeArea;
        private MobileTouchStick leftStick;
        private MobileTouchStick rightStick;
        private readonly Dictionary<GameAction, MobileActionButton> buttons = new Dictionary<GameAction, MobileActionButton>();
        private readonly Dictionary<GameAction, Text> buttonLabels = new Dictionary<GameAction, Text>();
        private readonly List<RectTransform> controlRects = new List<RectTransform>();
        private readonly List<RaycastResult> raycastBuffer = new List<RaycastResult>(16);
        private PointerEventData touchRaycast;
        private PlayerInventory inventory;
        private PlayerController player2D;
        private Game.World.Voxel3D.VoxelPlayerController3D voxelPlayer;
        private float nextPlayerLookup;
        private Game.World.Furniture.FurniturePlacementModeController placementMode;
        private float nextLabelsAt;
        private int touchFinger = -1;
        private float touchStartedAt;
        private Vector2 touchPosition;
        private bool touchIsMining;
        private bool targetOnThisFrame;
        private Rect previousSafeRect;
        private bool worldScenePresent;
        private float nextWorldCheck;
        private float firstWorldDetectedAt = -1f;

        private static readonly Color Dark = new Color(0.075f, 0.10f, 0.15f, 0.78f);
        private static readonly Color Border = new Color(0.35f, 0.62f, 0.86f, 0.93f);
        private static readonly Color Thumb = new Color(0.62f, 0.85f, 1f, 0.85f);
        private static Sprite circleSprite;
        private static Font buttonFont;
        private const float TapToMineDelay = 0.24f;

        private void Awake()
        {
            if (!GameInput.MobileEnabled) { Destroy(gameObject); return; }
            // On a New-Input-only Android build accessing the old Input API
            // throws before BuildUi(), leaving the HUD completely absent.
#if ENABLE_LEGACY_INPUT_MANAGER
            try { Input.simulateMouseWithTouches = false; }
            catch (Exception e) { Debug.LogWarning("TELDER MOBILE: touch mouse emulation: " + e.Message); }
#endif
            BuildUi();
            // The menu does not show game-world controls.
            canvas.enabled = false;
            SceneManager.sceneLoaded += SceneChanged;
            ApplySafeArea();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneChanged;
            GameInput.MobileReset();
        }

        private void SceneChanged(Scene scene, LoadSceneMode mode)
        {
            inventory = null;
            player2D = null;
            placementMode = null;
            nextLabelsAt = 0f;
            voxelPlayer = null;
            nextPlayerLookup = 0f;
            touchFinger = -1;
            touchIsMining = false;
            GameInput.MobileReset();
            worldScenePresent = false;
            nextWorldCheck = 0f;
            firstWorldDetectedAt = -1f;
            GameInput.RefreshSceneBindings();
        }

        private void Update()
        {
            if (canvas == null) return;
            if (!Mathf.Approximately(previousSafeRect.width, Screen.safeArea.width) ||
                !Mathf.Approximately(previousSafeRect.height, Screen.safeArea.height) ||
                !Mathf.Approximately(previousSafeRect.x, Screen.safeArea.x) ||
                !Mathf.Approximately(previousSafeRect.y, Screen.safeArea.y))
                ApplySafeArea();

            // Allow the HUD to appear as soon as a world scene exists. Previously
            // it was hidden until a PlayerController became enabled, which can
            // happen late (or never after a spawn/loading fault) on Android.
            if (Time.unscaledTime >= nextPlayerLookup)
            {
                nextPlayerLookup = Time.unscaledTime + 0.5f;
                if (inventory == null) inventory = FindObjectOfType<PlayerInventory>();
                if (player2D == null)
                {
                    player2D = FindObjectOfType<PlayerController>();
                    if (player2D != null)
                        placementMode = player2D.GetComponent<Game.World.Furniture.FurniturePlacementModeController>();
                }
                if (voxelPlayer == null)
                    voxelPlayer = FindObjectOfType<Game.World.Voxel3D.VoxelPlayerController3D>();
            }
            if (Time.unscaledTime >= nextWorldCheck)
            {
                nextWorldCheck = Time.unscaledTime + 0.5f;
                worldScenePresent = Game.World.WorldManager.Instance != null ||
                    FindObjectOfType<Game.World.Voxel3D.VoxelWorld3DRuntime>() != null;
            }
            bool worldReady = worldScenePresent || player2D != null || voxelPlayer != null;
            if (!worldReady)
            {
                if (canvas.enabled) canvas.enabled = false;
                ResetTouch();
                GameInput.MobileReset();
                return;
            }
            if (!canvas.enabled)
            {
                canvas.enabled = true;
                Debug.Log("TELDER MOBILE HUD V3: controls visible. " +
                    "WorldManager=" + (Game.World.WorldManager.Instance != null) +
                    ", player2D=" + (player2D != null) +
                    ", voxel3D=" + (voxelPlayer != null));
            }
            if (firstWorldDetectedAt < 0f) firstWorldDetectedAt = Time.unscaledTime;
            EnsureEventSystem();
            if (Time.unscaledTime >= nextLabelsAt)
            {
                nextLabelsAt = Time.unscaledTime + .12f;
                UpdateModeLabels();
            }

            bool paused = Time.timeScale < 0.001f;
            bool inMenu = paused || (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                Game.UI.Settings.SettingsTabsController.IsOpen ||
                Game.GameplaySystems.Respawn.DeathScreenUI.IsVisible;
            foreach (var pair in buttons)
            {
                bool persistent = pair.Key == GameAction.Pause ||
                    (pair.Key == GameAction.Inventory && !paused);
                if (pair.Value.gameObject.activeSelf != (!inMenu || persistent))
                    pair.Value.gameObject.SetActive(!inMenu || persistent);
            }
            leftStick.gameObject.SetActive(!inMenu);
            rightStick.gameObject.SetActive(!inMenu);

            foreach (var pair in buttons)
            {
                if (pair.Value.ConsumePress()) GameInput.MobilePulse(pair.Key);
                // Jump is merged with upward stick movement below. Do not
                // toggle its held state twice per frame (would auto-jump).
                if (pair.Key != GameAction.Jump)
                    GameInput.MobileSetHeld(pair.Key, !inMenu && pair.Value.IsDown);
            }

            if (inMenu)
            {
                ResetTouch();
                GameInput.MobileSetMovement(Vector2.zero);
                GameInput.MobileSetLook(Vector2.zero);
                GameInput.MobileSetHeld(GameAction.Attack, false);
                GameInput.MobileSetHeld(GameAction.Use, false);
                GameInput.MobileSetHeld(GameAction.Jump, false);
                return;
            }

            Vector2 left = leftStick.Value;
            Vector2 right = rightStick.Value;
            GameInput.MobileSetMovement(left);
            GameInput.MobileSetLook(right);
            // Push the left stick up to jump/swim/climb; a dedicated JUMP button
            // is provided as well so jumping never requires releasing movement.
            bool in3D = voxelPlayer != null && voxelPlayer.isActiveAndEnabled;
            bool jump = (!in3D && left.y > .66f) || buttons[GameAction.Jump].IsDown;
            GameInput.MobileSetHeld(GameAction.Jump, jump);

            HandleWorldTouches();

            // Right stick attacks with a weapon, or mines with a selected tool.
            // With a placeable block it only positions the build reticle.
            bool rightActive = right.sqrMagnitude > 0.22f;
            bool rightAttack = rightActive && IsWeaponOrToolEquipped();
            GameInput.MobileSetHeld(GameAction.Attack, rightAttack || touchIsMining);
        }

        private bool IsWeaponOrToolEquipped()
        {
            if (inventory == null) return false;
            string id = inventory.GetSelectedItemId();
            if (string.IsNullOrEmpty(id)) return false;
            Game.Items.ItemDefinition item = Game.Items.ItemRegistry.Get(id);
            if (item != null && item.Type == Game.Items.ItemType.Block) return false;
            return (item != null && (item.Type == Game.Items.ItemType.Tool ||
                    item.Type == Game.Items.ItemType.Weapon || item.Type == Game.Items.ItemType.Bomb)) ||
                GameInput.CurrentAimMode == GamepadAimMode.Weapon;
        }

        private void HandleWorldTouches()
        {
            targetOnThisFrame = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            for (int n = 0; n < Input.touchCount; n++)
            {
                Touch touch = Input.GetTouch(n);
                if (touch.phase == TouchPhase.Began && touchFinger < 0 && !IsControlTouch(touch.position))
                {
                    touchFinger = touch.fingerId;
                    touchStartedAt = Time.unscaledTime;
                    touchIsMining = false;
                    touchPosition = touch.position;
                }
                if (touch.fingerId != touchFinger) continue;
                touchPosition = touch.position;
                targetOnThisFrame = true;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    bool shortTap = !touchIsMining && touch.phase == TouchPhase.Ended;
                    touchFinger = -1;
                    touchIsMining = false;
                    if (shortTap)
                    {
                        // Transform mode consumes Attack to rotate/mirror; a
                        // weapon consumes Attack. Otherwise tap places once.
                        bool attack = Game.BlockTransforms.BlockTransformGameplayController.Mode !=
                            Game.BlockTransforms.BlockTransformMode.None ||
                            GameInput.CurrentAimMode == GamepadAimMode.Weapon;
                        GameInput.MobilePulse(attack ? GameAction.Attack : GameAction.Use);
                    }
                    continue;
                }
                if (Time.unscaledTime - touchStartedAt >= TapToMineDelay)
                    touchIsMining = true;
            }
#elif ENABLE_INPUT_SYSTEM
            // New Input System only: read touchscreen without UnityEngine.Input.
            var screen = UnityEngine.InputSystem.Touchscreen.current;
            if (screen != null)
            {
                foreach (var contact in screen.touches)
                {
                    int fingerId = contact.touchId.ReadValue();
                    bool down = contact.press.isPressed;
                    Vector2 pos = contact.position.ReadValue();
                    if (contact.press.wasPressedThisFrame && touchFinger < 0 && !IsControlTouch(pos))
                    {
                        touchFinger = fingerId;
                        touchStartedAt = Time.unscaledTime;
                        touchIsMining = false;
                        touchPosition = pos;
                    }
                    if (fingerId != touchFinger) continue;
                    touchPosition = pos;
                    targetOnThisFrame = true;
                    if (!down || contact.press.wasReleasedThisFrame)
                    {
                        bool shortTap = !touchIsMining && contact.press.wasReleasedThisFrame;
                        touchFinger = -1;
                        touchIsMining = false;
                        if (shortTap)
                        {
                            bool attack = Game.BlockTransforms.BlockTransformGameplayController.Mode !=
                                Game.BlockTransforms.BlockTransformMode.None ||
                                GameInput.CurrentAimMode == GamepadAimMode.Weapon;
                            GameInput.MobilePulse(attack ? GameAction.Attack : GameAction.Use);
                        }
                    }
                    else if (Time.unscaledTime - touchStartedAt >= TapToMineDelay)
                        touchIsMining = true;
                }
            }
#endif
#if UNITY_EDITOR
            // In Editor preview, mouse simulates ONE world finger. The actual
            // touchscreen build always uses Input.touches instead.
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0) && touchFinger == -1 &&
                    !IsControlTouch(Input.mousePosition))
                {
                    touchFinger = -2;
                    touchStartedAt = Time.unscaledTime;
                    touchIsMining = false;
                    touchPosition = Input.mousePosition;
                }
                if (touchFinger == -2)
                {
                    touchPosition = Input.mousePosition;
                    targetOnThisFrame = true;
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (!touchIsMining)
                        {
                            bool attack = Game.BlockTransforms.BlockTransformGameplayController.Mode !=
                                Game.BlockTransforms.BlockTransformMode.None ||
                                GameInput.CurrentAimMode == GamepadAimMode.Weapon;
                            GameInput.MobilePulse(attack ? GameAction.Attack : GameAction.Use);
                        }
                        touchFinger = -1;
                        touchIsMining = false;
                    }
                    else if (Time.unscaledTime - touchStartedAt >= TapToMineDelay)
                        touchIsMining = true;
                }
            }
#endif
#endif
            // On tap release the final target remains available until this
            // frame's gameplay Update runs, then falls back to stick targeting.
            GameInput.MobileSetWorldTouch(touchPosition, targetOnThisFrame);
            if (touchFinger < 0 && !targetOnThisFrame) touchIsMining = false;
        }

        private void ResetTouch()
        {
            touchFinger = -1;
            touchIsMining = false;
            targetOnThisFrame = false;
            GameInput.MobileSetWorldTouch(Vector2.zero, false);
        }

        private bool IsControlTouch(Vector2 screenPosition)
        {
            foreach (RectTransform rect in controlRects)
            {
                if (rect != null && rect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, null))
                    return true;
            }
            // The existing hotbar slots and any other gameplay UI must remain
            // clickable without triggering world attacks behind the widgets.
            if (EventSystem.current == null) return false;
            if (touchRaycast == null) touchRaycast = new PointerEventData(EventSystem.current);
            touchRaycast.Reset();
            touchRaycast.position = screenPosition;
            raycastBuffer.Clear();
            EventSystem.current.RaycastAll(touchRaycast, raycastBuffer);
            for (int i = 0; i < raycastBuffer.Count; i++)
            {
                GameObject go = raycastBuffer[i].gameObject;
                if (go.GetComponentInParent<InventorySlotUI>() != null ||
                    go.GetComponentInParent<Selectable>() != null ||
                    go.GetComponentInParent<ScrollRect>() != null)
                    return true;
            }
            return false;
        }

        private static void EnsureEventSystem()
        {
            EventSystem current = EventSystem.current;
            if (current == null)
            {
                GameObject system = new GameObject("Telder Mobile EventSystem");
                current = system.AddComponent<EventSystem>();
            }
            // If a scene has an EventSystem but no input module, buttons may
            // be drawn yet not receive a single touch. Only supply a legacy
            // module when there is no existing input module (avoid conflicts
            // with InputSystemUIInputModule in projects using Both).
            var modules = current.GetComponents<BaseInputModule>();
            bool hasActiveModule = false;
            for (int i = 0; i < modules.Length; i++)
                if (modules[i] != null && modules[i].enabled) { hasActiveModule = true; break; }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // A legacy StandaloneInputModule can be enabled but unusable when
            // Player Settings selects only the new Input System.
            var newModule = current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null)
                newModule = current.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (!newModule.enabled) newModule.enabled = true;
            foreach (var module in modules)
                if (module != null && module != newModule) module.enabled = false;
#else
            if (!hasActiveModule) current.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void OnGUI()
        {
            // If there are still no controls on the next build, this marker
            // helps distinguish a missing C# assembly from a hidden Canvas.
            if (firstWorldDetectedAt < 0f ||
                Time.unscaledTime - firstWorldDetectedAt > 20f ||
                PlayerPrefs.GetInt("TELDER_TOUCH_DIAGNOSTICS", 1) == 0) return;
            GUI.depth = -200000;
            GUIStyle marker = new GUIStyle(GUI.skin.label);
            marker.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * .023f), 13, 25);
            marker.normal.textColor = Color.cyan;
            GUI.Label(new Rect(12f, 9f, Screen.width * .8f, 40f),
                "TELDER TOUCH HUD V3  |  Canvas=" + (canvas != null && canvas.enabled) +
                "  |  Player=" + (player2D != null || voxelPlayer != null), marker);
        }

        private void BuildUi()
        {
            GameObject ui = new GameObject("MobileHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.transform.SetParent(transform, false);
            canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            CanvasScaler scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            safeArea = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(ui.transform, false);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            leftStick = CreateStick("Movement Joystick", new Vector2(164, 170), false);
            rightStick = CreateStick("Aim / Tool Joystick", new Vector2(-164, 170), true);

            // Main requested actions: pause at upper right, the three block /
            // inventory controls immediately above the centre hotbar.
            AddButton(GameAction.Pause, "II", new Vector2(-85, -82), new Vector2(110, 90), 1, 1);
            AddButton(GameAction.Inventory, "INV", new Vector2(-154, 230), new Vector2(126, 91), .5f, 0f);
            AddButton(GameAction.SwitchLayer, "LAYER", new Vector2(0, 230), new Vector2(136, 91), .5f, 0f);
            AddButton(GameAction.RotateBlock, "ROT", new Vector2(154, 230), new Vector2(126, 91), .5f, 0f);
            AddButton(GameAction.Jump, "JUMP", new Vector2(398, 188), new Vector2(120, 108), 0f, 0f);
            AddButton(GameAction.Interact, "USE", new Vector2(-393, 198), new Vector2(105, 94), 1f, 0f);
            AddButton(GameAction.DimensionMenu, "PORTAL", new Vector2(-396, 315), new Vector2(110, 75), 1f, 0f);
            AddButton(GameAction.Drop, "DROP", new Vector2(396, 315), new Vector2(110, 75), 0f, 0f);
            AddButton(GameAction.Toggle3DView, "CAM", new Vector2(-204, -78), new Vector2(105, 84), 1f, 1f);
            AddButton(GameAction.RopeShorten, "ROPE+", new Vector2(85, -78), new Vector2(112, 76), 0f, 1f);
            AddButton(GameAction.RopeLengthen, "ROPE-", new Vector2(211, -78), new Vector2(112, 76), 0f, 1f);
        }

        private MobileTouchStick CreateStick(string name, Vector2 pos, bool right)
        {
            Image pad = ImageRect(name, safeArea, Vector2.one * 235f, pos, right ? 1f : 0f,
                0f, Dark, GetCircleSprite());
            controlRects.Add(pad.rectTransform);
            MobileTouchStick stick = pad.gameObject.AddComponent<MobileTouchStick>();
            Image knob = ImageRect("Thumb", pad.rectTransform, Vector2.one * 100f,
                Vector2.zero, .5f, .5f, Thumb, GetCircleSprite());
            knob.raycastTarget = false;
            stick.Initialize(pad.rectTransform, knob.rectTransform, 72f);
            return stick;
        }

        private void AddButton(GameAction action, string label, Vector2 pos,
            Vector2 size, float ax, float ay)
        {
            Image image = ImageRect(action.ToString(), safeArea, size, pos, ax, ay, Dark, null);
            image.gameObject.AddComponent<Outline>().effectColor = Border;
            Text text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(image.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.text = label;
            text.font = GetFont();
            text.fontSize = 29;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            buttons.Add(action, image.gameObject.AddComponent<MobileActionButton>());
            buttonLabels.Add(action, text);
            controlRects.Add(image.rectTransform);
        }

        private void UpdateModeLabels()
        {
            Text text;
            if (buttonLabels.TryGetValue(GameAction.RotateBlock, out text) && text != null)
            {
                var mode = Game.BlockTransforms.BlockTransformGameplayController.Mode;
                string caption = mode == Game.BlockTransforms.BlockTransformMode.Rotate ? "ROT: ON" :
                    mode == Game.BlockTransforms.BlockTransformMode.Mirror ? "MIRROR" : "ROT";
                if (text.text != caption) text.text = caption;
            }
            if (buttonLabels.TryGetValue(GameAction.SwitchLayer, out text) && text != null)
            {
                string caption = placementMode == null ? "LAYER" : placementMode.FurniturePlacementActive ? "FURN" :
                    placementMode.BackgroundPlacementActive ? "BACK" : "FRONT";
                if (text.text != caption) text.text = caption;
            }
        }

        private static Image ImageRect(string name, Transform parent, Vector2 size,
            Vector2 pos, float anchorX, float anchorY, Color color, Sprite sprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            return image;
        }

        private static Font GetFont()
        {
            if (buttonFont != null) return buttonFont;
            try { buttonFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
            if (buttonFont == null)
            {
                try { buttonFont = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { }
            }
            if (buttonFont == null) buttonFont = Font.CreateDynamicFontFromOSFont("Arial", 22);
            return buttonFont;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int side = 64;
            Texture2D tex = new Texture2D(side, side, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color32[] pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * side + x] = dist > 31 ? new Color32(0, 0, 0, 0) :
                        dist > 28 ? new Color32(148, 199, 240, 255) :
                        new Color32(255, 255, 255, 255);
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            circleSprite = Sprite.Create(tex, new Rect(0, 0, side, side), new Vector2(.5f, .5f), side);
            return circleSprite;
        }

        private void ApplySafeArea()
        {
            previousSafeRect = Screen.safeArea;
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect r = Screen.safeArea;
            safeArea.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Real UI pointer capture; each stick tracks its own finger.</summary>
    public sealed class MobileTouchStick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value { get; private set; }
        private RectTransform pad;
        private RectTransform thumb;
        private float travel;
        private int pointerId = int.MinValue;

        public void Initialize(RectTransform padRect, RectTransform thumbRect, float radius)
        { pad = padRect; thumb = thumbRect; travel = radius; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != int.MinValue) return;
            pointerId = eventData.pointerId;
            UpdatePosition(eventData);
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (pointerId == eventData.pointerId) UpdatePosition(eventData);
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerId != eventData.pointerId) return;
            Clear();
        }
        private void OnDisable() { Clear(); }
        private void Clear()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            if (thumb != null) thumb.anchoredPosition = Vector2.zero;
        }
        private void UpdatePosition(PointerEventData data)
        {
            if (pad == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    pad, data.position, data.pressEventCamera, out local)) return;
            Vector2 offset = Vector2.ClampMagnitude(local, travel);
            Value = offset / Mathf.Max(1f, travel);
            if (thumb != null) thumb.anchoredPosition = offset;
        }
    }

    public sealed class MobileActionButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        public bool IsDown { get; private set; }
        private bool pendingPress;
        private int pointerId = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            pendingPress = true;
            IsDown = true;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointerId != e.pointerId) return;
            pointerId = int.MinValue;
            IsDown = false;
        }
        public bool ConsumePress()
        {
            bool value = pendingPress;
            pendingPress = false;
            return value;
        }
        private void OnDisable()
        {
            pointerId = int.MinValue;
            IsDown = false;
            pendingPress = false;
        }
    }
}
