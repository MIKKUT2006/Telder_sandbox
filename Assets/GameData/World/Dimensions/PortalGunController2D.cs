using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Combat;
using Game.InputSupport;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items;
using Game.Items.Durability;

namespace Game.World.Dimensions
{
    /// <summary>
    /// Runtime behavior for the data-driven Portalgun item.
    /// Right-click (Use) or the DimensionMenu binding opens the dimension menu;
    /// choosing a destination consumes the configured crystal cost, sends a
    /// screen-space distortion beam from the hand, and opens the portal at its tip.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(61000)]
    public sealed class PortalGunController2D : MonoBehaviour
    {
        [Header("Auto-resolved references")]
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerWeaponRigController weaponRig;
        [SerializeField] private PlayerAnimationController playerAnimation;
        [SerializeField] private DimensionPortalManager portalManager;
        [SerializeField] private DimensionTravelMenu travelMenu;

        private PortalGunConfig menuSettings;
        private PortalGunConfig activeFiringSettings;
        private string menuItemId;
        private bool isFiring;
        private Vector2 activeAimTarget;
        private Coroutine fireRoutine;

        public string LastStatusMessage { get; private set; } = string.Empty;
        public bool IsFiring => isFiring;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }
            if (portalManager != null)
                portalManager.ClearPortalGunBeam();
            isFiring = false;
            activeFiringSettings = null;
            ClearFacingOverride();
        }

        private void OnDestroy()
        {
            if (portalManager != null)
                portalManager.ClearPortalGunBeam();
            ClearFacingOverride();
        }

        public void BindMenu(DimensionTravelMenu menu)
        {
            travelMenu = menu;
        }

        private void ResolveReferences()
        {
            if (inventory == null)
                inventory = GetComponent<PlayerInventory>();
            if (weaponRig == null)
                weaponRig = GetComponent<PlayerWeaponRigController>();
            if (playerAnimation == null)
                playerAnimation = GetComponent<PlayerAnimationController>();
            if (portalManager == null)
                portalManager = FindFirstObjectByType<DimensionPortalManager>();
            if (travelMenu == null)
                travelMenu = FindFirstObjectByType<DimensionTravelMenu>();
        }

        private void Update()
        {
            ResolveReferences();

            if (isFiring)
            {
                UpdateFacingOverride(activeAimTarget);
                return;
            }

            if (travelMenu != null && travelMenu.IsOpen)
            {
                if (!string.IsNullOrEmpty(menuItemId) &&
                    inventory != null &&
                    inventory.GetSelectedItemId() != menuItemId)
                {
                    travelMenu.CloseMenu();
                    NotifyMenuClosed();
                }
                return;
            }

            if (Time.timeScale < 0.001f || inventory == null ||
                (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) ||
                Game.UI.Settings.SettingsTabsController.IsOpen)
                return;

            if (!GameInput.Pressed(GameAction.Use) &&
                !GameInput.Pressed(GameAction.DimensionMenu))
                return;

            if (!GameInput.UsingGamepadAim && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            string selectedId = inventory.GetSelectedItemId();
            if (ItemRegistry.Get(selectedId) == null ||
                !PortalGunConfigRegistry.TryGet(selectedId, out PortalGunConfig itemConfig))
                return;

            if (travelMenu == null)
                travelMenu = FindFirstObjectByType<DimensionTravelMenu>();
            if (travelMenu == null)
            {
                LastStatusMessage = "Меню измерений не найдено.";
                Debug.LogWarning("PORTALGUN: DimensionTravelMenu is missing from the scene.");
                return;
            }

            menuSettings = CloneSettings(itemConfig);
            menuItemId = selectedId;
            LastStatusMessage = string.Empty;

            if (!travelMenu.OpenForPortalGun(this))
            {
                menuSettings = null;
                menuItemId = null;
            }
        }

        /// <summary>True only while the selected item is configured as ToolType=portal_gun.</summary>
        public bool IsHoldingPortalGun()
        {
            ResolveReferences();
            if (inventory == null)
                return false;

            string selectedId = inventory.GetSelectedItemId();
            return PortalGunConfigRegistry.TryGet(selectedId, out PortalGunConfig ignored);
        }

        public bool CanAffordPortal(out string message)
        {
            ResolveReferences();

            if (inventory == null || menuSettings == null || string.IsNullOrWhiteSpace(menuItemId))
            {
                message = "Возьми портальную пушку в руку.";
                LastStatusMessage = message;
                return false;
            }

            if (inventory.GetSelectedItemId() != menuItemId)
            {
                message = "Для открытия портала портальная пушка должна оставаться в руке.";
                LastStatusMessage = message;
                return false;
            }

            if (portalManager == null && Game.World.Voxel3D.VoxelWorld3DRuntime.Instance==null)
            {
                message = "Система порталов не найдена.";
                LastStatusMessage = message;
                return false;
            }

            int cost = Mathf.Max(0, menuSettings.CrystalCost);
            string crystalId = GetCrystalItemId(menuSettings);
            int available = cost == 0 ? cost : inventory.CountItem(crystalId);
            if (available < cost)
            {
                message = "Не хватает портальных кристаллов: нужно " + cost;
                LastStatusMessage = message;
                return false;
            }

            message = string.Empty;
            LastStatusMessage = string.Empty;
            return true;
        }

        public bool TryFireToDimension(Vector3 target, string targetDimension)
        {
            ResolveReferences();

            if (isFiring)
            {
                LastStatusMessage = "Портальная пушка уже стреляет.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetDimension))
            {
                LastStatusMessage = "Не выбрано целевое измерение.";
                return false;
            }

            // Ищем уже СОЗДАННОЕ измерение по имени.
            // Сид берётся из базы, поле сида тут ни при чём.
            DimensionDefinition targetDefinition =
                DimensionDatabase.LookupByName(targetDimension);

            if (targetDefinition == null)
            {
                LastStatusMessage = "Не удалось определить измерение.";
                return false;
            }

            int requiredTier = targetDefinition.Type != null
                ? targetDefinition.Type.Tier
                : 1;

            if (!CanAccessDimensionTier(requiredTier, out string tierMessage))
                return false;
            if (!CanAffordPortal(out string reason))
            {
                LastStatusMessage = reason;
                return false;
            }

            PortalGunConfig firingSettings = CloneSettings(menuSettings);
            string crystalId = GetCrystalItemId(firingSettings);
            int cost = Mathf.Max(0, firingSettings.CrystalCost);

            if (cost > 0 && !inventory.TryConsumeItem(crystalId, cost))
            {
                int available = inventory.CountItem(crystalId);
                LastStatusMessage = "Не хватает портальных кристаллов: нужно " + cost;
                return false;
            }

            var voxel=Game.World.Voxel3D.VoxelWorld3DRuntime.Instance;
            if(voxel!=null)
            {
                Game.World.Voxel3D.VoxelPortal3D.Open(voxel,targetDimension.Trim());LastStatusMessage=string.Empty;return true;
            }
            activeFiringSettings = CloneSettings(firingSettings);
            target.z = portalManager.PortalWorldZ;
            Color beamColor = ParseColor(firingSettings.BeamColor, new Color(0.38f, 0.91f, 1f, 1f));
            isFiring = true;
            activeAimTarget = target;
            fireRoutine = StartCoroutine(FireBeam(target, targetDimension.Trim(), firingSettings, beamColor));
            LastStatusMessage = string.Empty;
            return true;
        }

        public void NotifyMenuClosed()
        {
            menuSettings = null;
            menuItemId = null;
        }

        private IEnumerator FireBeam(
            Vector3 target,
            string targetDimension,
            PortalGunConfig settings,
            Color beamColor)
        {
            if (portalManager == null)
                ResolveReferences();

            if (portalManager == null)
            {
                isFiring = false;
                ClearFacingOverride();
                fireRoutine = null;
                LastStatusMessage = "Система порталов не найдена.";
                yield break;
            }

            Vector2 launchPoint = GetHandWorld();
            Vector2 destination = new Vector2(target.x, target.y);
            float distance = Vector2.Distance(launchPoint, destination);
            float speed = Mathf.Max(1f, settings.BeamSpeed);
            float duration = Mathf.Max(0.08f, distance / speed);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                activeAimTarget = Vector2.Lerp(launchPoint, destination, t);

                Vector2 hand = GetHandWorld();
                Vector3 beamStart = new Vector3(hand.x, hand.y, portalManager.PortalWorldZ);
                Vector3 beamEnd = new Vector3(activeAimTarget.x, activeAimTarget.y, portalManager.PortalWorldZ);
                portalManager.SetPortalGunBeam(
                    beamStart,
                    beamEnd,
                    beamColor,
                    settings.BeamWidth,
                    settings.BeamDistortion,
                    settings.BeamStrength);

                UpdateFacingOverride(activeAimTarget);
                yield return null;
            }

            DimensionPortal previousPortal = portalManager.CurrentPortal;
            portalManager.OpenPortal(target, targetDimension);
            while (portalManager.CurrentPortal == previousPortal)
            {
                activeAimTarget = destination;
                UpdateFacingOverride(activeAimTarget);
                yield return null;
            }

            portalManager.ClearPortalGunBeam();
            isFiring = false;
            fireRoutine = null;
            activeFiringSettings = null;
            ClearFacingOverride();
        }

        private Vector2 GetHandWorld()
        {
            if (weaponRig != null)
            {
                Transform hand = weaponRig.GetRuntimeHandPoint();
                if (hand != null)
                    return hand.position;
            }

            return (Vector2)transform.position + Vector2.up * 0.35f;
        }

        private void LateUpdate()
        {
            if (!isFiring)
                return;

            ResolveReferences();
            if (weaponRig != null)
                weaponRig.AimHarpoonRope(activeAimTarget);

            if (portalManager != null)
            {
                Vector2 hand = GetHandWorld();
                portalManager.SetPortalGunBeam(
                    new Vector3(hand.x, hand.y, portalManager.PortalWorldZ),
                    new Vector3(activeAimTarget.x, activeAimTarget.y, portalManager.PortalWorldZ),
                    ParseColor(activeFiringSettings != null ? activeFiringSettings.BeamColor : "#62E9FF", new Color(0.38f, 0.91f, 1f, 1f)),
                    activeFiringSettings != null ? activeFiringSettings.BeamWidth : 5f,
                    activeFiringSettings != null ? activeFiringSettings.BeamDistortion : 6f,
                    activeFiringSettings != null ? activeFiringSettings.BeamStrength : 1f);
            }

            UpdateFacingOverride(activeAimTarget);
        }

        private void UpdateFacingOverride(Vector2 target)
        {
            if (playerAnimation == null)
                return;

            float deltaX = target.x - transform.position.x;
            bool right = Mathf.Abs(deltaX) > 0.08f ? deltaX > 0f : playerAnimation.IsFacingRight();
            playerAnimation.SetExternalFacingOverride(true, right);
        }

        private void ClearFacingOverride()
        {
            if (playerAnimation != null)
                playerAnimation.ClearExternalFacingOverride();
        }

        private static string GetCrystalItemId(PortalGunConfig settings)
        {
            return settings == null || string.IsNullOrWhiteSpace(settings.CrystalItemId)
                ? "game:crystal"
                : settings.CrystalItemId.Trim();
        }
        public int GetCurrentPortalGunLevel()
        {
            if (inventory == null)
                return 0;

            ItemStack selectedStack = inventory.GetSelectedStack();
            if (selectedStack == null || selectedStack.IsEmpty)
                return 0;

            return DurabilitySystem.GetToolLevel(selectedStack);
        }

        public bool CanAccessDimensionTier(int requiredTier, out string message)
        {
            requiredTier = Mathf.Max(1, requiredTier);
            int currentLevel = GetCurrentPortalGunLevel();

            if (currentLevel < requiredTier)
            {
                message = "Низкий уровень портальной пушки";
                LastStatusMessage = message;
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static PortalGunConfig CloneSettings(PortalGunConfig source)
        {
            if (source == null)
                return null;

            return new PortalGunConfig
            {
                CrystalItemId = source.CrystalItemId,
                CrystalCost = source.CrystalCost,
                BeamSpeed = source.BeamSpeed,
                BeamWidth = source.BeamWidth,
                BeamDistortion = source.BeamDistortion,
                BeamStrength = source.BeamStrength,
                BeamColor = source.BeamColor
            };
        }

        private static Color ParseColor(string value, Color fallback)
        {
            if (!string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out Color color))
                return color;
            return fallback;
        }
    }
}