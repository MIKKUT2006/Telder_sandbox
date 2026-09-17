using UnityEngine;
using Game.Inventory;
using Game.Inventory.UI;
using Game.World;

namespace Game.Items.Durability
{
    // Runs before the current BlockInteraction Update and checks the result in LateUpdate.
    // Durability is spent only when a foreground/background cell actually changes to air.
    [DefaultExecutionOrder(-1000)]
    public class DurabilityMiningHook : MonoBehaviour
    {
        [SerializeField] private float interactionDistance = 6f;

        private PlayerInventory inventory;
        private Camera playerCamera;
        private WorldManager worldManager;
        private Game.World.World world;

        private bool captured;
        private int x;
        private int y;
        private ushort foregroundBefore;
        private ushort backgroundBefore;
        private int selectedSlotBefore;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        }

        private void Start()
        {
            playerCamera = Camera.main;
            worldManager = WorldManager.Instance;
            if (worldManager != null) world = worldManager.GetWorld();
        }

        private void Update()
        {
            captured = false;
            if (inventory == null || !Input.GetMouseButton(0)) return;
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

            if (playerCamera == null) playerCamera = Camera.main;
            if (worldManager == null) worldManager = WorldManager.Instance;
            if (world == null && worldManager != null) world = worldManager.GetWorld();
            if (playerCamera == null || world == null) return;

            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(playerCamera.transform.position.z);
            Vector3 mouse = playerCamera.ScreenToWorldPoint(screen);

            x = Mathf.FloorToInt(mouse.x);
            y = Mathf.FloorToInt(mouse.y);

            Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
            if (Vector2.Distance(transform.position, cellCenter) > interactionDistance) return;

            foregroundBefore = world.GetBlock(x, y);
            backgroundBefore = world.GetBackground(x, y);
            if (foregroundBefore == 0 && backgroundBefore == 0) return;

            selectedSlotBefore = inventory.SelectedHotbarIndex;
            captured = true;
        }

        private void LateUpdate()
        {
            if (!captured || world == null || inventory == null) return;

            ushort foregroundAfter = world.GetBlock(x, y);
            ushort backgroundAfter = world.GetBackground(x, y);

            bool foregroundBroken = foregroundBefore != 0 && foregroundAfter == 0;
            bool backgroundBroken = foregroundBefore == 0 && backgroundBefore != 0 && backgroundAfter == 0;
            if (!foregroundBroken && !backgroundBroken) return;

            ItemStack tool = inventory.GetSlot(selectedSlotBefore);
            if (tool == null || tool.IsEmpty || !DurabilitySystem.HasDurability(tool)) return;

            if (DurabilitySystem.Damage(tool, 1))
            {
                tool.Clear();
                Debug.Log("DURABILITY: Tool broke.");
            }

            inventory.NotifyExternalChange();
        }
    }
}
