using System.Collections.Generic;
using Game.GameplaySystems.Furnace;
using Game.Inventory.UI;
using Game.UI.MainMenu;
using Game.UI.Pause;
using Game.UI.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.InputSupport
{
    // Scene-independent d-pad/left-stick focus for both selectable buttons and
    // custom IPointerClickHandler slots (inventory, furnace, chests, bench).
    public sealed class GamepadUIFocusDriver : MonoBehaviour
    {
        private static GamepadUIFocusDriver instance;
        private RectTransform focusRect;
        private RectTransform crosshair;
        private Image crosshairImage;
        private Sprite buildCursorSprite;
        private Sprite combatCursorSprite;

        [Header("World gamepad cursors")]
        [SerializeField] private Color buildingCursorColor = new Color(.45f, .94f, 1f, 1f);
        [SerializeField] private Color combatCursorColor = new Color(1f, .72f, .32f, 1f);
        private bool lastUiActive;
        private bool drawWorldReticle;
        private RectTransform focused;
        private float nextMove;
        private bool navHeld;
        private float nextCandidatesRefresh;
        private EventSystem events;
        private readonly List<RectTransform> candidates = new List<RectTransform>(128);
        private GameObject hoverObject;
        private bool hadInterface;
        private PauseMenuController cachedPause;
        private MainMenuController cachedMainMenu;
        private SaveSelectionController cachedSaveSelection;
        private Game.World.Voxel3D.VoxelPlayerController3D cachedVoxelPlayer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;

            // ”же подключЄн Ч создаЄм сразу.
            if (GameInput.GamepadConnected || GameInput.Settings.ForceGamepadAim)
            {
                CreateInstance();
                return;
            }

            // »наче Ч только Ђнаблюдательї, который дождЄтс€ подключени€.
            var watcher = new GameObject("TelderGamepadWatcher", typeof(GamepadWatcher));
            DontDestroyOnLoad(watcher);
        }

        internal static void CreateInstance()
        {
            if (instance != null) return;
            var go = new GameObject("TelderGamepadUI", typeof(GamepadUIFocusDriver));
            DontDestroyOnLoad(go);
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            cachedPause = null;
            cachedMainMenu = null;
            cachedSaveSelection = null;
            cachedVoxelPlayer = null;
            hadInterface = false;
            navHeld = false;
            nextCandidatesRefresh = 0f;
            GameInput.ClearUiFocusPointer();
            lastUiActive = false;
            drawWorldReticle = false;
            GameInput.RefreshSceneBindings();
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            GameInput.ApplyGraphics();
            if (EventSystem.current == null)
            {
                GameObject e = new GameObject("TelderGamepadEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(e);
            }
            var root = new GameObject("GamepadFocusOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            Canvas c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 3000;
            RectTransform canvas = root.GetComponent<RectTransform>();
            var ring = new GameObject("SelectedSlotFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
            ring.transform.SetParent(canvas, false);
            focusRect = ring.GetComponent<RectTransform>();
            Image image = ring.GetComponent<Image>();
            image.color = new Color(.8f, .95f, 1f, .02f);
            image.raycastTarget = false;
            Outline outline = ring.GetComponent<Outline>();
            outline.effectColor = new Color(.65f, .86f, 1f, .95f);
            outline.effectDistance = new Vector2(3, 3);
            outline.useGraphicAlpha = false;
            focusRect.gameObject.SetActive(false);
            GameObject aim = new GameObject("GamepadWorldReticle", typeof(RectTransform), typeof(Image));
            aim.transform.SetParent(canvas, false);
            crosshair = aim.GetComponent<RectTransform>();
            crosshair.sizeDelta = new Vector2(22, 22);
            crosshairImage = aim.GetComponent<Image>();
            // These two point-filtered procedural sprites require no imported
            // textures or scene setup. Settings and other overlay panels cannot
            // intercept gameplay clicks because raycastTarget is false.
            combatCursorSprite = MakeCursorSprite(false);
            buildCursorSprite = MakeCursorSprite(true);
            crosshairImage.sprite = combatCursorSprite;
            crosshairImage.color = combatCursorColor;
            crosshairImage.raycastTarget = false;
            crosshair.gameObject.SetActive(false);
        }
        private void Update()
        {
            // UI selection must also work if the world reticle is disabled.
            if ((!GameInput.GamepadConnected && !GameInput.Settings.ForceGamepadAim) || GameInput.IsRebinding)
            { Hide(); drawWorldReticle = false; if (crosshair != null) crosshair.gameObject.SetActive(false); return; }
            if (cachedPause == null) cachedPause = FindObjectOfType<PauseMenuController>();
            if (cachedMainMenu == null && SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("menu"))
                cachedMainMenu = FindObjectOfType<MainMenuController>();
            if (cachedSaveSelection == null && cachedMainMenu != null)
                cachedSaveSelection = FindObjectOfType<SaveSelectionController>();
            if (cachedSaveSelection != null && cachedSaveSelection.GamepadSelectionActive)
            { Hide(); drawWorldReticle = false; if (crosshair != null) crosshair.gameObject.SetActive(false); return; }
            bool activeUI = SettingsTabsController.IsOpen ||
                (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                // Presence of a cached MainMenuController must not block the
                // world reticle in an additive gameplay scene.
                (cachedMainMenu != null && SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("menu")) ||
                (cachedPause != null && cachedPause.IsPaused) ||
                Game.GameplaySystems.Respawn.DeathScreenUI.IsVisible;
            if (!activeUI)
            {
                if (lastUiActive) GameInput.ResetPointer();
                lastUiActive = false;
                GameInput.ClearUiFocusPointer();
                navHeld = false;
                Hide(); hadInterface = false;
                UpdateWorldReticle();
                return;
            }
            lastUiActive = true;
            drawWorldReticle = false;
            if (crosshair != null) crosshair.gameObject.SetActive(false);
            if (!hadInterface) { focused = null; navHeld = false; nextCandidatesRefresh = 0f; hadInterface = true; }
            if (focused == null || !focused.gameObject.activeInHierarchy) { RefreshCandidates(); FocusFirst(); }
            Vector2 nav = GameInput.Navigation;
            bool navigating = nav.sqrMagnitude > .20f;
            if (!navigating)
            {
                navHeld = false;
                nextMove = 0f;
            }
            else if (!navHeld || Time.unscaledTime >= nextMove)
            {
                // Cache UI candidates for a short period instead of traversing
                // every Canvas at each gamepad repeat tick.
                if (Time.unscaledTime >= nextCandidatesRefresh)
                {
                    RefreshCandidates();
                    nextCandidatesRefresh = Time.unscaledTime + .25f;
                }
                Move(nav);
                // First repeat is slower to avoid overshooting a slot; while
                // held, traverse at ~12 slots/sec, including diagonally.
                nextMove = Time.unscaledTime + (navHeld ? .085f : .20f);
                navHeld = true;
            }
            if (GameInput.Pressed(GameAction.UiConfirm)) Click(PointerEventData.InputButton.Left);
            else if (GameInput.Pressed(GameAction.SplitStack)) Click(PointerEventData.InputButton.Right);
            else if (GameInput.Pressed(GameAction.QuickMove)) QuickTransfer();
            if (GameInput.Pressed(GameAction.UiCancel) && SettingsTabsController.IsOpen)
            {
                GameInput.ConsumeUiCancel();
                SettingsTabsController.Close(true);
                focused = null;
                GameInput.ClearUiFocusPointer();
                return;
            }
            if (GameInput.Pressed(GameAction.UiCancel) &&
                SaveContextMenuController.Instance != null && SaveContextMenuController.Instance.IsOpen)
            {
                GameInput.ConsumeUiCancel();
                SaveContextMenuController.Instance.Close();
                focused = null;
                GameInput.ClearUiFocusPointer();
                return;
            }
            if (GameInput.Pressed(GameAction.UiCancel) && !SettingsTabsController.IsOpen)
            {
                if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen &&
                    (cachedPause == null || !cachedPause.IsPaused))
                    InventoryUI.Instance.CloseInventory();
            }
            if (focused != null)
            {
                // UI layout can move (crafting panel, furnace, resolution).
                // Refresh screen-space pointer even without another stick tick.
                Vector2 pos = RectTransformUtility.WorldToScreenPoint(null,
                    focused.TransformPoint(focused.rect.center));
                GameInput.SetUiFocusPointer(pos);
                UpdateFocusVisual();
            }
        }
        private static Sprite MakeCursorSprite(bool building)
        {
            // 16x16 pixel frames with a completely transparent interior.
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color clear = Color.clear;
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool ink;
                if (building)
                {
                    // Corner-square for mining and placement. At screen size
                    // it matches a single game-world tile, not mouse distance.
                    bool side = x <= 1 || x >= 14;
                    bool top = y <= 1 || y >= 14;
                    ink = (side && top) ||
                        (side && (y < 5 || y >= 11)) ||
                        (top && (x < 5 || x >= 11));
                }
                else
                {
                    // Small combat crosshair/ring; never covers a whole tile.
                    int dx = x - 8;
                    int dy = y - 8;
                    int r2 = dx * dx + dy * dy;
                    ink = (r2 >= 22 && r2 <= 40) ||
                        (dx == 0 && (Mathf.Abs(dy) <= 2 || Mathf.Abs(dy) >= 6)) ||
                        (dy == 0 && (Mathf.Abs(dx) <= 2 || Mathf.Abs(dx) >= 6));
                }
                texture.SetPixel(x, y, ink ? Color.white : clear);
            }
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16),
                new Vector2(.5f, .5f), 16f, 0, SpriteMeshType.FullRect);
            sprite.name = building ? "GamepadBuildCursor" : "GamepadCombatCursor";
            return sprite;
        }

        // IMGUI renders independently of the EventSystem / UGUI Canvas and
        // survives dynamic scene UI construction. This fixes V10.1's missing
        // reticle even when all other controller actions are working.
        private void UpdateWorldReticle()
        {
            drawWorldReticle = GameInput.UsingGamepadAim;
            if (crosshair != null && crosshair.gameObject.activeSelf)
                crosshair.gameObject.SetActive(false);
        }
        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (GameInput.Settings.ShowGamepadAimDiagnostics)
            {
                GUI.depth = -10000;
                GUI.color = new Color(.06f, .09f, .14f, .93f);
                GUI.DrawTexture(new Rect(8f, 8f, 460f, 72f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(18f, 11f, 440f, 22f),
                    "AIM: " + GameInput.AimStatus);
                GUI.Label(new Rect(18f, 34f, 440f, 22f),
                    "Device: " + (GameInput.NewInputSystemSeesGamepad ? "Input System" :
                    (GameInput.GamepadConnected ? "Legacy joystick" : "not detected")) +
                    " | Right stick: " + GameInput.DebugRightStick.ToString("F2"));
                GUI.Label(new Rect(18f, 54f, 440f, 22f),
                    "Force cursor: " + GameInput.Settings.ForceGamepadAim);
            }
            if (!drawWorldReticle || !GameInput.UsingGamepadAim) return;
            Vector2 screen = GameInput.PointerPosition;
            if (screen.x < -60 || screen.x > Screen.width + 60 ||
                screen.y < -60 || screen.y > Screen.height + 60) return;
            bool building = GameInput.CurrentAimMode == GamepadAimMode.Building;
            float side = 24f;
            if (building)
            {
                Camera cam = GameInput.AimCamera;
                if (cam != null)
                {
                    Vector2 world = GameInput.WorldAimPosition;
                    Vector3 a = cam.WorldToScreenPoint(world);
                    Vector3 b = cam.WorldToScreenPoint(world + Vector2.right);
                    side = Mathf.Clamp(Mathf.Abs(b.x - a.x), 14f, 160f);
                }
            }
            GUI.depth = -10001;
            Rect location = new Rect(screen.x - side * .5f,
                Screen.height - screen.y - side * .5f, side, side);
            GUI.color = new Color(0f, 0f, 0f, .85f);
            GUI.DrawTexture(new Rect(location.x + 2f, location.y + 2f,
                side, side), (building ? buildCursorSprite : combatCursorSprite).texture);
            GUI.color = building ? buildingCursorColor : combatCursorColor;
            GUI.DrawTexture(location,
                (building ? buildCursorSprite : combatCursorSprite).texture);
            GUI.color = Color.white;
        }
        private void OnDestroy()
        {
            GameInput.ClearUiFocusPointer();
            if (instance == this) instance = null;
            if (buildCursorSprite != null)
            {
                Texture2D texture = buildCursorSprite.texture;
                Destroy(buildCursorSprite);
                if (texture != null) Destroy(texture);
            }
            if (combatCursorSprite != null)
            {
                Texture2D texture = combatCursorSprite.texture;
                Destroy(combatCursorSprite);
                if (texture != null) Destroy(texture);
            }
        }

        private void Hide()
        {
            GameInput.ClearUiFocusPointer();
            if (focusRect != null) focusRect.gameObject.SetActive(false);
            if (hoverObject != null && events != null)
                ExecuteEvents.Execute(hoverObject, new PointerEventData(events), ExecuteEvents.pointerExitHandler);
            hoverObject = null;
            focused = null;
        }
        private void RefreshCandidates()
        {
            candidates.Clear();
            Transform settingsRoot = SettingsTabsController.IsOpen ? SettingsTabsController.Root : null;
            foreach (Canvas canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.isActiveAndEnabled) continue;
                foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(false))
                {
                    if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                    if (settingsRoot != null && !rect.IsChildOf(settingsRoot)) continue;
                    if (rect.GetComponent<Selectable>() == null &&
                        rect.GetComponent<InventorySlotUI>() == null &&
                        rect.GetComponent<FurnaceSlotUI>() == null &&
                        rect.GetComponent<Game.Chests.UI.ChestSlotUI>() == null &&
                        rect.GetComponent<Game.ToolBench.ToolBenchSlotUI>() == null) continue;
                    if (rect.GetComponent<Selectable>() != null && !rect.GetComponent<Selectable>().IsInteractable()) continue;
                    if (rect.rect.width < 4 || rect.rect.height < 4) continue;
                    candidates.Add(rect);
                }
            }
        }
        private void FocusFirst()
        {
            if (candidates.Count == 0) { GameInput.ClearUiFocusPointer(); return; }
            // When inventory is open prefer a storage slot: this lets a
            // controller begin moving items immediately after opening E/Y.
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            {
                foreach (RectTransform r in candidates)
                {
                    InventorySlotUI slot = r.GetComponent<InventorySlotUI>();
                    if (slot != null && slot.SlotIndex == 9)
                    { Focus(r); return; }
                }
                foreach (RectTransform r in candidates)
                    if (r.GetComponent<InventorySlotUI>() != null)
                    { Focus(r); return; }
            }
            RectTransform best = null;
            float bestScore = float.NegativeInfinity;
            foreach (RectTransform r in candidates)
            {
                Vector3 p = r.TransformPoint(r.rect.center);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, p);
                if (screen.x < 0 || screen.y < 0 || screen.x > Screen.width || screen.y > Screen.height) continue;
                float score = screen.y - .07f * Mathf.Abs(screen.x - Screen.width * .5f);
                if (score > bestScore) { bestScore = score; best = r; }
            }
            if (best != null) Focus(best);
        }
        private void Move(Vector2 direction)
        {
            if (focused == null) { FocusFirst(); return; }
            Vector2 dir = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ?
                new Vector2(Mathf.Sign(direction.x), 0) : new Vector2(0, Mathf.Sign(direction.y));
            Vector2 from = RectTransformUtility.WorldToScreenPoint(null, focused.TransformPoint(focused.rect.center));
            RectTransform best = null;
            float cost = float.MaxValue;
            foreach (RectTransform candidate in candidates)
            {
                if (candidate == focused || candidate == null ||
                    !candidate.gameObject.activeInHierarchy) continue;
                Vector2 target = RectTransformUtility.WorldToScreenPoint(null, candidate.TransformPoint(candidate.rect.center));
                Vector2 delta = target - from;
                float along = Vector2.Dot(delta, dir);
                if (along < 5f) continue;
                float across = Mathf.Abs(dir.x != 0 ? delta.y : delta.x);
                float test = along + across * 3.5f;
                if (test < cost) { cost = test; best = candidate; }
            }
            if (best != null) Focus(best);
        }
        private void Focus(RectTransform target)
        {
            if (target == focused) return;
            if (events == null) events = EventSystem.current;
            if (hoverObject != null && events != null)
                ExecuteEvents.Execute(hoverObject, new PointerEventData(events), ExecuteEvents.pointerExitHandler);
            focused = target;
            hoverObject = target.gameObject;
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, focused.TransformPoint(focused.rect.center));
            GameInput.SetUiFocusPointer(p);
            if (events != null)
            {
                var data = new PointerEventData(events) { position = p };
                ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerEnterHandler);
                var selectable = target.GetComponent<Selectable>();
                // Do not select in EventSystem: StandaloneInputModule would submit twice.
            }
        }
        private void Click(PointerEventData.InputButton button)
        {
            if (focused == null) return;
            Button clickable = focused.GetComponent<Button>();
            if (clickable != null && button == PointerEventData.InputButton.Left)
            { clickable.onClick.Invoke(); focused = null; nextCandidatesRefresh = 0f; return; }
            if (events == null) events = EventSystem.current;
            if (events == null) return;
            var data = new PointerEventData(events) { button = button, position = GameInput.PointerPosition };
            if (button == PointerEventData.InputButton.Right)
                ExecuteEvents.Execute(focused.gameObject, data, ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(focused.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        private void QuickTransfer()
        {
            if (focused == null) return;
            InventorySlotUI slot = focused.GetComponent<InventorySlotUI>();
            if (slot != null && InventoryUI.Instance != null)
            { InventoryUI.Instance.GamepadQuickMoveSlot(slot.SlotIndex); return; }
            Game.Chests.UI.ChestSlotUI chest = focused.GetComponent<Game.Chests.UI.ChestSlotUI>();
            if (chest != null)
            {
                var ui = Game.Chests.UI.ChestUIController.Instance;
                if (ui != null) ui.GamepadQuickMoveFromChest(chest.SlotIndex);
                return;
            }
            FurnaceSlotUI furnace = focused.GetComponent<FurnaceSlotUI>();
            if (furnace != null)
            {
                var ui = FindObjectOfType<FurnaceUI>();
                if (ui != null) ui.GamepadQuickMove(furnace.Kind);
            }
            else Click(PointerEventData.InputButton.Left);
        }
        private void UpdateFocusVisual()
        {
            if (focused == null || !focused.gameObject.activeInHierarchy) { Hide(); return; }
            Vector3[] corners = new Vector3[4];
            focused.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            RectTransform canvas = focusRect.parent as RectTransform;
            focusRect.anchorMin = focusRect.anchorMax = new Vector2(.5f, .5f);
            focusRect.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x) + 5, Mathf.Abs(b.y - a.y) + 5);
            focusRect.position = new Vector3((a.x + b.x) * .5f, (a.y + b.y) * .5f, 0);
            focusRect.gameObject.SetActive(true);
        }
    }
    public sealed class GamepadWatcher : MonoBehaviour
    {
        private bool wasConnected;

        private void Update()
        {
            bool connected = GameInput.GamepadConnected || GameInput.Settings.ForceGamepadAim;
            if (connected && !wasConnected)
                GamepadUIFocusDriver.CreateInstance();
            wasConnected = connected;
        }
    }
}
