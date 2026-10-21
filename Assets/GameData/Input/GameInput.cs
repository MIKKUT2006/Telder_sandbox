using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Game.InputSupport
{
    public enum GameAction
    {
        Jump, Attack, Use, Interact, Inventory, Pause,
        HotbarPrevious, HotbarNext, Drop, QuickMove, SplitStack,
        RotateBlock, SwitchLayer, MoveLeft, MoveRight,
        UiConfirm, UiCancel, UiUp, UiDown, UiLeft, UiRight,
        DimensionMenu, Toggle3DView,
        Hotbar1, Hotbar2, Hotbar3, Hotbar4, Hotbar5, Hotbar6, Hotbar7, Hotbar8, Hotbar9,
        // Appended at the end to preserve all existing saved binding indices.
        RopeShorten, RopeLengthen
    }

    [Serializable]
    public class BindingEntry
    {
        public string Action;
        public string Keyboard;
        public string Gamepad;
    }

    [Serializable]
    public class InputPreferences
    {
        public int Version = 2;
        public int FullscreenMode = 1;
        public int LightingQuality = 2;
        public float MasterVolume = 1f;
        public float StickDeadzone = 0.24f;
        public float AimSpeed = 850f; // Legacy V10 setting, kept for save compatibility.
        public float BuildAimSpeed = 12.5f; // World blocks / second: responsive tile selection.
        public float BuildAimRadius = 5.0f; // Never exceed normal interaction range.
        public float CombatAimRadius = 2.25f; // Reticle orbits the player, never the screen.
        public bool Rumble = true;
        public bool DisableGamepadAim = false;
        public bool ForceGamepadAim = false;
        public bool ShowGamepadAimDiagnostics = false;
        public List<BindingEntry> Bindings = new List<BindingEntry>();
    }

    // One input service: no InputAction asset / prefab dependencies. When the new
    // Input System is enabled, Steam Deck is read as a standard Gamepad. Legacy
    // joystick buttons are a fallback for projects using the old Input Manager.
    public enum GamepadAimMode { Building, Weapon }

    public static class GameInput
    {
        public static readonly GameAction[] Actions = (GameAction[])Enum.GetValues(typeof(GameAction));
        private static readonly string[] Keys = {
            "Space", "Mouse0", "Mouse1", "F", "E", "Escape",
            "Q", "R", "G", "LeftShift", "Mouse2", "Z", "X", "A", "D",
            "Return", "Escape", "UpArrow", "DownArrow", "LeftArrow", "RightArrow", "T", "F5",
            "Alpha1", "Alpha2", "Alpha3", "Alpha4", "Alpha5", "Alpha6", "Alpha7", "Alpha8", "Alpha9", "W", "S"
        };
        private static readonly string[] Pads = {
            "buttonSouth", "rightTrigger", "leftTrigger", "buttonWest", "buttonNorth", "startButton",
            "leftShoulder", "rightShoulder", "rightStickPress", "buttonWest", "leftStickPress", "dpad/up", "dpad/down",
            "", "", "buttonSouth", "buttonEast", "dpad/up", "dpad/down", "dpad/left", "dpad/right", "selectButton", "dpad/right", "", "", "", "", "", "", "", "", "", "", ""
        };
        private static readonly string[] LegacyPads = {
            "JoystickButton0", "", "", "JoystickButton2", "JoystickButton3", "JoystickButton7",
            "JoystickButton4", "JoystickButton5", "JoystickButton9", "JoystickButton2", "JoystickButton8", "JoystickButton0", "JoystickButton0",
            "", "", "JoystickButton0", "JoystickButton1", "", "", "", "", "JoystickButton6", "", "", "", "", "", "", "", "", "", "", "", ""
        };
        private static InputPreferences settings;
        private static readonly Dictionary<GameAction, BindingEntry> cache = new Dictionary<GameAction, BindingEntry>();
        private static Vector2 pointer;
        // Cursor for gamepad UI navigation. Separate from the world/building reticle.
        private static Vector2 uiPointer;
        private static bool uiPointerActive;
        private static int pointerFrame = -1;
        private static bool padAimActive;
        private static Vector2 buildOffset = new Vector2(2f, 0f);
        private static Vector2 combatDirection = Vector2.right;
        private static Vector2 worldAimPosition;
        private static GamepadAimMode aimMode;
        private static GamepadAimMode previousAimMode;
        private static bool worldAimReady;
        private static Game.Inventory.PlayerInventory activePlayer;
        private static Camera aimCamera;
        private static Game.World.Voxel3D.VoxelPlayerController3D voxelPlayer;
        private static float nextPlayerSearch;
        private static float nextVoxelSearch;
        private static Vector2 lastMousePosition;
        private static bool mousePositionInitialized;
        private static bool padOwnsAim;
        private static string aimStatus = "Not initialized";
        private static Vector2 lastRightStick;

        // Touchscreen input is supplied by MobileGameplayControls before the
        // other gameplay Update calls. PC bindings remain untouched.
        private static readonly bool[] mobileHeld = new bool[Actions.Length];
        private static readonly int[] mobilePressed = new int[Actions.Length];
        private static readonly int[] mobileReleased = new int[Actions.Length];
        private static readonly int[] mobilePulse = new int[Actions.Length];
        private static Vector2 mobileMovement;
        private static Vector2 mobileLook;
        private static Vector2 mobileScreenTarget;
        private static bool mobileScreenTargetActive;
        public static bool MobileEnabled => Application.isMobilePlatform ||
            (Application.isEditor && PlayerPrefs.GetInt("TELDER_TEST_MOBILE_UI", 0) == 1);

        public static void MobileSetMovement(Vector2 value) { mobileMovement = Vector2.ClampMagnitude(value, 1f); }
        public static void MobileSetLook(Vector2 value) { mobileLook = Vector2.ClampMagnitude(value, 1f); pointerFrame = -1; }
        public static void MobileSetWorldTouch(Vector2 position, bool active)
        {
            mobileScreenTarget = position;
            mobileScreenTargetActive = active;
        }
        public static void MobileSetHeld(GameAction action, bool value)
        {
            int id = (int)action;
            if (mobileHeld[id] == value) return;
            mobileHeld[id] = value;
            if (value) mobilePressed[id] = Time.frameCount;
            else mobileReleased[id] = Time.frameCount;
        }
        public static void MobilePulse(GameAction action) { mobilePulse[(int)action] = Time.frameCount; }
        public static void MobileReset()
        {
            Array.Clear(mobileHeld, 0, mobileHeld.Length);
            Array.Clear(mobilePressed, 0, mobilePressed.Length);
            Array.Clear(mobileReleased, 0, mobileReleased.Length);
            Array.Clear(mobilePulse, 0, mobilePulse.Length);
            mobileMovement = mobileLook = Vector2.zero;
            mobileScreenTargetActive = false;
            pointerFrame = -1;
        }

        private static bool isRebinding;
        private static int consumedCancelFrame = -1;
        public static void ConsumeUiCancel() { consumedCancelFrame = Time.frameCount; }
        public static bool IsRebinding { get { return isRebinding; } set { isRebinding = value; } }
        public static InputPreferences Settings { get { Ensure(); return settings; } }
        // Allows users whose Steam Input profile emulates keyboard/mouse to
        // explicitly request the game-world cursor in Settings > General.
        public static bool GamepadAimRequested
        {
            get
            {
                Ensure();
                return !settings.DisableGamepadAim && (GamepadConnected || settings.ForceGamepadAim);
            }
        }
        public static string AimStatus { get { UpdateWorldPointer(); return aimStatus; } }
        public static Vector2 DebugRightStick { get { UpdateWorldPointer(); return lastRightStick; } }
        public static bool NewInputSystemSeesGamepad
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null;
#else
                return false;
#endif
            }
        }
        public static void RefreshSceneBindings()
        {
            activePlayer = null;
            aimCamera = null;
            voxelPlayer = null;
            pointerFrame = -1;
            uiPointerActive = false;
            nextPlayerSearch = nextVoxelSearch = 0f;
        }
        public static bool GamepadConnected
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                string[] joysticks = Input.GetJoystickNames();
                for (int i = 0; i < joysticks.Length; i++)
                    if (!string.IsNullOrWhiteSpace(joysticks[i])) return true;
#endif
                return false;
            }
        }

        private static void Ensure()
        {
            if (settings != null) return;
            try { settings = JsonUtility.FromJson<InputPreferences>(PlayerPrefs.GetString("TELDER_CONTROLS_V1", "")); }
            catch { settings = null; }
            if (settings == null) settings = new InputPreferences();
            if (settings.Bindings == null) settings.Bindings = new List<BindingEntry>();
            // Settings written by V10 did not contain these fields; JsonUtility
            // may return zero rather than a field initializer for missing values.
            if (settings.BuildAimSpeed <= 0f) settings.BuildAimSpeed = 12.5f;
            // One-time migration from the much slower V10.2 default.
            // Keep any faster user setting intact.
            if (settings.Version < 2)
            {
                settings.BuildAimSpeed = Mathf.Max(12.5f, settings.BuildAimSpeed);
                settings.Version = 2;
            }
            if (settings.BuildAimRadius <= 0f) settings.BuildAimRadius = 5.0f;
            if (settings.CombatAimRadius <= 0f) settings.CombatAimRadius = 2.25f;
            foreach (GameAction action in Actions)
            {
                string id = action.ToString();
                BindingEntry b = settings.Bindings.Find(x => x != null && x.Action == id);
                if (b == null)
                {
                    int i = (int)action;
                    b = new BindingEntry { Action = id, Keyboard = Keys[i], Gamepad = Pads[i] };
                    settings.Bindings.Add(b);
                }
                cache[action] = b;
            }
            pointer = new Vector2(Screen.width / 2f, Screen.height / 2f);
            ApplyGraphics();
        }
        public static void Save()
        {
            Ensure();
            PlayerPrefs.SetString("TELDER_CONTROLS_V1", JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
            ApplyGraphics();
        }
        public static void RestoreDefaults(bool keyboard)
        {
            Ensure();
            foreach (GameAction action in Actions)
            {
                int i = (int)action;
                if (keyboard) cache[action].Keyboard = Keys[i];
                else cache[action].Gamepad = Pads[i];
            }
            Save();
        }
        public static string Binding(GameAction action, bool gamepad)
        {
            Ensure();
            return gamepad ? cache[action].Gamepad : cache[action].Keyboard;
        }
        public static void Rebind(GameAction action, bool gamepad, string newValue)
        {
            Ensure();
            if (gamepad) cache[action].Gamepad = newValue ?? "";
            else cache[action].Keyboard = newValue ?? "";
            Save();
        }
        public static bool TryCapture(bool gamepad, out string value)
        {
            value = null;
            if (gamepad)
            {
#if ENABLE_INPUT_SYSTEM
                Gamepad pad = Gamepad.current;
                if (pad != null)
                {
                    foreach (var control in pad.allControls)
                    {
                        var button = control as ButtonControl;
                        if (button != null && button.wasPressedThisFrame &&
                            control.name != "leftTriggerButton" && control.name != "rightTriggerButton")
                        {
                            // dpad controls are addressed relative to the pad.
                            value = control.parent != null && control.parent != pad
                                ? control.parent.name + "/" + control.name : control.name;
                            return true;
                        }
                    }
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                for (int i = 0; i < 20; i++)
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                    { value = "JoystickButton" + i; return true; }
#endif
            }
            else
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
                    if (code != KeyCode.None && Input.GetKeyDown(code) && !code.ToString().StartsWith("Joystick"))
                    { value = code.ToString(); return true; }
#endif
            }
            return false;
        }
        private static bool Key(string key, int mode)
        {
            if (string.IsNullOrEmpty(key)) return false;
            KeyCode code;
            if (!Enum.TryParse(key, true, out code)) return false;
#if ENABLE_LEGACY_INPUT_MANAGER
            return mode == 0 ? Input.GetKeyDown(code) : mode == 1 ? Input.GetKey(code) : Input.GetKeyUp(code);
#else
            return false;
#endif
        }
        private static bool Pad(string path, GameAction action, int mode)
        {
            if (string.IsNullOrEmpty(path)) return false;
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                InputControl control = null;
                try { control = pad[path]; } catch { }
                var button = control as ButtonControl;
                if (button != null) return mode == 0 ? button.wasPressedThisFrame : mode == 1 ? button.isPressed : button.wasReleasedThisFrame;
            }
#endif
            // Default controller on older projects, with optional explicit JoystickButton mapping.
            if (path.StartsWith("JoystickButton")) return Key(path, mode);
            if (path == Pads[(int)action]) return Key(LegacyPads[(int)action], mode);
            return false;
        }
        private static bool Read(GameAction action, int mode)
        {
            Ensure();
            if (isRebinding) return false;
            if (MobileEnabled)
            {
                int index = (int)action;
                bool touch = mode == 0 ? mobilePressed[index] == Time.frameCount || mobilePulse[index] == Time.frameCount :
                    mode == 1 ? mobileHeld[index] || mobilePulse[index] == Time.frameCount :
                    mobileReleased[index] == Time.frameCount;
                if (touch) return true;
                // On New-Input-only builds UnityEngine.Input.GetKey throws.
                // Touch button values (or a connected gamepad) are sufficient.
                BindingEntry mb = cache[action];
                return Pad(mb.Gamepad, action, mode);
            }
            BindingEntry b = cache[action];
            return Key(b.Keyboard, mode) || Pad(b.Gamepad, action, mode);
        }
        public static bool Pressed(GameAction action)
        {
            if (action == GameAction.UiCancel && consumedCancelFrame == Time.frameCount) return false;
            return Read(action, 0);
        }
        public static bool Held(GameAction action) { return Read(action, 1); }
        public static bool Released(GameAction action) { return Read(action, 2); }
        public static float Horizontal
        {
            get
            {
                if (IsRebinding) return 0f;
                if (MobileEnabled && Mathf.Abs(mobileMovement.x) > .02f) return mobileMovement.x;
                float k = (Held(GameAction.MoveRight) ? 1f : 0f) - (Held(GameAction.MoveLeft) ? 1f : 0f);
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null)
                {
                    float x = Gamepad.current.leftStick.ReadValue().x;
                    bool customMovement = !string.IsNullOrEmpty(cache[GameAction.MoveLeft].Gamepad) ||
                        !string.IsNullOrEmpty(cache[GameAction.MoveRight].Gamepad);
                    if (!customMovement && Mathf.Abs(x) > Settings.StickDeadzone) k = x;
                }
                else
#endif
                {
                    try { float x = Input.GetAxisRaw("Horizontal"); if (Mathf.Abs(x) > 0.1f) k = x; } catch { }
                }
                return Mathf.Clamp(k, -1f, 1f);
            }
        }
        public static Vector2 Navigation
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null)
                {
                    Vector2 stick = Gamepad.current.leftStick.ReadValue();
                    if (stick.magnitude > Settings.StickDeadzone) return stick;
                    // In menus both sticks can navigate slots; in the world
                    // the right stick exclusively aims.
                    Vector2 right = Gamepad.current.rightStick.ReadValue();
                    if (right.magnitude > Settings.StickDeadzone) return right;
                    bool customNav = cache[GameAction.UiUp].Gamepad != "dpad/up" ||
                        cache[GameAction.UiDown].Gamepad != "dpad/down" ||
                        cache[GameAction.UiLeft].Gamepad != "dpad/left" ||
                        cache[GameAction.UiRight].Gamepad != "dpad/right";
                    if (!customNav)
                    {
                        Vector2 pad = Gamepad.current.dpad.ReadValue();
                        if (pad.sqrMagnitude > 0.2f) return pad;
                    }
                }
#endif
                return new Vector2((Held(GameAction.UiRight) ? 1 : 0) - (Held(GameAction.UiLeft) ? 1 : 0),
                    (Held(GameAction.UiUp) ? 1 : 0) - (Held(GameAction.UiDown) ? 1 : 0));
            }
        }
        public static float Vertical
        {
            get
            {
                if (MobileEnabled && Mathf.Abs(mobileMovement.y) > .02f) return mobileMovement.y;
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null)
                {
                    float y = Gamepad.current.leftStick.ReadValue().y;
                    if (Mathf.Abs(y) > Settings.StickDeadzone) return y;
                }
#endif
                try { return Input.GetAxisRaw("Vertical"); } catch { return 0f; }
            }
        }
        public static Vector2 Look
        {
            get
            {
                if (MobileEnabled) return mobileLook;
#if ENABLE_INPUT_SYSTEM
                if (Gamepad.current != null)
                {
                    Vector2 stick = Gamepad.current.rightStick.ReadValue();
                    return stick.magnitude > Settings.StickDeadzone ? stick * 2.5f : Vector2.zero;
                }
#endif
                return Vector2.zero;
            }
        }
        // Cursor mode is chosen from the selected item, without introducing an
        // extra "aim mode" button: weapon => combat reticle, everything else =>
        // world/build cursor. All existing systems use PointerPosition, so the
        // same world target works for mining, furniture, furnace, and weapons.
        public static GamepadAimMode CurrentAimMode
        {
            get { UpdateWorldPointer(); return aimMode; }
        }

        public static bool UsingGamepadAim
        {
            get { UpdateWorldPointer(); return padAimActive && worldAimReady; }
        }

        public static Vector2 WorldAimPosition
        {
            get { UpdateWorldPointer(); return worldAimPosition; }
        }

        public static Camera AimCamera
        {
            get { UpdateWorldPointer(); return aimCamera; }
        }

        public static Vector2 PointerPosition
        {
            get
            {
                if (uiPointerActive) return uiPointer;
                if (MobileEnabled && mobileScreenTargetActive) return mobileScreenTarget;
                UpdateWorldPointer();
                if (MobileEnabled)
                    return padAimActive && worldAimReady ? pointer :
                        new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return padAimActive && worldAimReady ? pointer : (Vector2)Input.mousePosition;
            }
        }

        // The inventory cursor item and its tooltip must follow the focused slot,
        // not the OS mouse. This is only active while navigating UI on gamepad.
        public static void SetUiFocusPointer(Vector2 screenPosition)
        {
            Ensure();
            uiPointer = screenPosition;
            uiPointerActive = true;
        }
        public static void ClearUiFocusPointer()
        {
            uiPointerActive = false;
        }

        // Manual pointer calls are retained for compatibility with UI scripts.
        public static void SetPointer(Vector2 value)
        {
            Ensure();
            pointer = value;
            padOwnsAim = true;
            pointerFrame = -1;
        }

        public static void ResetPointer()
        {
            Ensure();
            buildOffset = new Vector2(2f, 0f);
            combatDirection = Vector2.right;
            pointerFrame = -1;
            // Do not reset to the centre of the screen. The player may be
            // moving and the camera might not centre on their character.
        }

        private static void UpdateWorldPointer()
        {
            Ensure();
            if (pointerFrame == Time.frameCount) return;
            pointerFrame = Time.frameCount;
            padAimActive = false;
            worldAimReady = false;
            aimStatus = "No active gamepad (or cursor disabled)";

            // The aim must remain visible even when the stick is stationary.
            // Steam Input can emulate mouse motion; this is NOT a reason to
            // revoke gamepad ownership each frame as V10.1 used to do.
            Vector2 actualMouse = MobileEnabled ?
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) :
                (Vector2)Input.mousePosition;
            Vector2 mouseDelta = Vector2.zero;
            if (mousePositionInitialized)
                mouseDelta = actualMouse - lastMousePosition;
            else
                mousePositionInitialized = true;
            lastMousePosition = actualMouse;

            if ((!GamepadAimRequested && !MobileEnabled) || isRebinding) return;
            padOwnsAim = true;

            Vector2 stick = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
                stick = Gamepad.current.rightStick.ReadValue();
#endif
            if (MobileEnabled) stick = mobileLook;
            lastRightStick = stick;
            float deadzone = Mathf.Clamp(settings.StickDeadzone, .05f, .75f);

            // In Steam Input's keyboard/mouse mode, right-stick motion may be
            // presented as mouse delta, not as the Input System rightStick.
            // This fallback lets the world cursor move in that configuration.
            // For native Gamepad input the analog stick remains authoritative.
            bool useMouseEmulation = !MobileEnabled && !NewInputSystemSeesGamepad &&
                mouseDelta.sqrMagnitude > 1.5f;
            if (useMouseEmulation)
            {
                mouseDelta = Vector2.ClampMagnitude(mouseDelta, 35f);
                stick = Vector2.ClampMagnitude(mouseDelta / 12f, 1f);
            }

            // First-person voxel motor owns the right stick in 3D.
            if (voxelPlayer == null && Time.unscaledTime >= nextVoxelSearch)
            {
                nextVoxelSearch = Time.unscaledTime + .7f;
                voxelPlayer = UnityEngine.Object.FindObjectOfType<Game.World.Voxel3D.VoxelPlayerController3D>();
            }
            if (voxelPlayer != null && voxelPlayer.isActiveAndEnabled)
            { aimStatus = "3D mode: use centre crosshair"; return; }

            if (Game.UI.Settings.SettingsTabsController.IsOpen ||
                Game.GameplaySystems.Respawn.DeathScreenUI.IsVisible ||
                (Game.Inventory.UI.InventoryUI.Instance != null && Game.Inventory.UI.InventoryUI.Instance.IsOpen) ||
                Time.timeScale < .0001f)
            { aimStatus = "UI open or game paused"; return; }

            if (activePlayer == null && Time.unscaledTime >= nextPlayerSearch)
            {
                nextPlayerSearch = Time.unscaledTime + .5f;
                activePlayer = UnityEngine.Object.FindObjectOfType<Game.Inventory.PlayerInventory>();
            }
            if (activePlayer == null)
            { aimStatus = "PlayerInventory not found"; return; }

            if (aimCamera == null || !aimCamera.isActiveAndEnabled || !aimCamera.orthographic)
            {
                aimCamera = Camera.main;
                if (aimCamera == null || !aimCamera.isActiveAndEnabled || !aimCamera.orthographic)
                {
                    // Not every 2D project tags its camera MainCamera.
                    foreach (Camera candidate in Camera.allCameras)
                    {
                        if (candidate != null && candidate.isActiveAndEnabled && candidate.orthographic)
                        { aimCamera = candidate; break; }
                    }
                }
            }
            if (aimCamera == null || !aimCamera.isActiveAndEnabled || !aimCamera.orthographic)
            { aimStatus = "Active orthographic camera not found"; return; }

            string itemId = activePlayer.GetSelectedItemId();
            Game.Items.ItemDefinition itemDefinition = string.IsNullOrWhiteSpace(itemId)
                ? null : Game.Items.ItemRegistry.Get(itemId);
            // An Item JSON can have Weapon: { Kind: "None" } solely because
            // it was edited in Weapon Studio (e.g. iron_pickaxe.json).
            // Such tools are NEVER combat weapons and keep the tile cursor.
            bool isWeapon = false;
            if (itemDefinition != null &&
                (itemDefinition.Type == Game.Items.ItemType.Block ||
                 itemDefinition.Type == Game.Items.ItemType.Tool))
            {
                isWeapon = false;
            }
            else if (!string.IsNullOrWhiteSpace(itemId))
            {
                Game.Combat.WeaponMetadata metadata;
                isWeapon = Game.Combat.WeaponMetadataRegistry.TryGet(itemId, out metadata) &&
                    metadata != null && metadata.GetKind() != Game.Combat.WeaponKind.None;
                if (!isWeapon && itemDefinition != null)
                    isWeapon = itemDefinition.Type == Game.Items.ItemType.Weapon ||
                        itemDefinition.Type == Game.Items.ItemType.Bomb;
            }
            aimMode = isWeapon ? GamepadAimMode.Weapon : GamepadAimMode.Building;

            if (previousAimMode != aimMode)
            {
                if (aimMode == GamepadAimMode.Building)
                    buildOffset = combatDirection * 2f;
                previousAimMode = aimMode;
            }

            Vector2 playerCentre = (Vector2)activePlayer.transform.position + Vector2.up * .3f;
            Vector2 directionInput = stick.magnitude > deadzone ? stick : Vector2.zero;
            if (directionInput != Vector2.zero && !useMouseEmulation)
            {
                float strength = Mathf.Clamp01((directionInput.magnitude - deadzone) / (1f - deadzone));
                directionInput = directionInput.normalized * strength;
            }

            if (aimMode == GamepadAimMode.Weapon)
            {
                if (directionInput.sqrMagnitude > .0001f)
                    combatDirection = directionInput.normalized;
                worldAimPosition = playerCentre + combatDirection *
                    Mathf.Clamp(settings.CombatAimRadius, .65f, 5f);
            }
            else
            {
                // For a mobile pickaxe/tool the right stick indicates a nearby
                // direction to mine, not a drifting five-block cursor. A block
                // in the selected tile remains targeted while the stick is held.
                if (MobileEnabled && itemDefinition != null &&
                    itemDefinition.Type == Game.Items.ItemType.Tool &&
                    directionInput.sqrMagnitude > .001f)
                    buildOffset = directionInput.normalized * 2.0f;
                else if (directionInput.sqrMagnitude > .001f)
                    buildOffset += directionInput *
                        Mathf.Clamp(settings.BuildAimSpeed, 1f, 32f) *
                        Mathf.Min(Time.unscaledDeltaTime, .05f);
                buildOffset = Vector2.ClampMagnitude(buildOffset,
                    Mathf.Clamp(settings.BuildAimRadius, 1f, 5.25f));
                Vector2 raw = playerCentre + buildOffset;
                worldAimPosition = new Vector2(Mathf.Floor(raw.x) + .5f,
                    Mathf.Floor(raw.y) + .5f);
            }

            Vector3 screen = aimCamera.WorldToScreenPoint(worldAimPosition);
            if (screen.z < 0f)
            { aimStatus = "Aim target is behind camera"; return; }
            pointer = new Vector2(screen.x, screen.y);
            padAimActive = true;
            worldAimReady = true;
            aimStatus = useMouseEmulation ? "ACTIVE (Steam mouse emulation)" :
                (NewInputSystemSeesGamepad ? "ACTIVE (Input System gamepad)" :
                "ACTIVE (legacy gamepad; check right stick)");
        }
        public static void ApplyGraphics()
        {
            if (settings == null) return;
            FullScreenMode mode = settings.FullscreenMode == 0 ? FullScreenMode.Windowed :
                settings.FullscreenMode == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
            if (Screen.fullScreenMode != mode) Screen.fullScreenMode = mode;
            // Lighting detail is consumed by the lighting bridge if present;
            // avoid accidentally changing the game's authored pixel-light algorithm.
            // Map to project's actual chunk-light update budget / sampling.
            var manager = Game.World.WorldManager.Instance;
            if (manager != null && manager.GetChunkRenderer() != null)
                manager.GetChunkRenderer().SetLightingDetail(settings.LightingQuality);
            AudioListener.volume = Mathf.Clamp01(settings.MasterVolume);
        }
    }
}
