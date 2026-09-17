using UnityEngine;
using UnityEngine.UI;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items.Durability;

namespace Game.ToolBench
{
    public class ToolBenchUI : MonoBehaviour
    {
        public static ToolBenchUI Instance { get; private set; }

        private GameObject panel;
        private ToolBenchSlotUI toolSlot;
        private ToolBenchSlotUI artifactSlot;
        private Button repairButton;
        private Button upgradeButton;
        private Text infoText;
        private Text repairText;
        private PlayerInventory inventory;
        private bool isOpen;
        private static Font runtimeFont;

        public static ToolBenchUI EnsureCreated()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("ToolBenchUIRuntime");
            DontDestroyOnLoad(go);
            return go.AddComponent<ToolBenchUI>();
        }

        public static Font GetRuntimeFont()
        {
            if (runtimeFont != null) return runtimeFont;
            try { runtimeFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (runtimeFont == null)
            {
                try { runtimeFont = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return runtimeFont;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(PlayerInventory playerInventory)
        {
            if (playerInventory == null || InventoryUI.Instance == null)
            {
                Debug.LogWarning("TOOL BENCH: PlayerInventory or InventoryUI is missing.");
                return;
            }

            inventory = playerInventory;
            BuildUI();
            InventoryUI.Instance.OpenInventory();
            panel.SetActive(true);
            isOpen = true;
            Refresh();
        }

        private void Update()
        {
            if (!isOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TryClose(true);
                return;
            }

            // Existing inventory controller may close itself with E.
            if (InventoryUI.Instance != null && !InventoryUI.Instance.IsOpen)
            {
                TryClose(false);
                return;
            }

            Refresh();
        }

        public bool TryClose(bool closeInventory = false)
        {
            if (!isOpen) return true;

            if (inventory != null && !ReturnBenchItems())
            {
                infoText.text = "Освободи место в инвентаре, чтобы закрыть Tool Bench.";
                if (InventoryUI.Instance != null) InventoryUI.Instance.OpenInventory();
                return false;
            }

            isOpen = false;
            if (panel != null) panel.SetActive(false);

            if (closeInventory && InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                InventoryUI.Instance.CloseInventory();

            inventory = null;
            return true;
        }

        private bool ReturnBenchItems()
        {
            if (toolSlot != null && !toolSlot.Stack.IsEmpty && !inventory.TryAddStack(toolSlot.Stack))
                return false;

            if (artifactSlot != null && !artifactSlot.Stack.IsEmpty && !inventory.TryAddStack(artifactSlot.Stack))
                return false;

            return true;
        }

        private void Repair()
        {
            if (inventory == null || toolSlot == null) return;
            ItemStack tool = toolSlot.Stack;
            int cost = DurabilitySystem.GetRepairCost(tool);
            if (cost <= 0) return;

            if (!DurabilitySystem.TryGetRepairRecipe(tool, out string repairItemId, out _))
                return;

            if (inventory.CountItem(repairItemId) < cost)
            {
                infoText.text = "Недостаточно материалов для ремонта.";
                return;
            }

            if (!inventory.TryConsumeItem(repairItemId, cost)) return;
            DurabilitySystem.RepairFully(tool);
            inventory.NotifyExternalChange();
            infoText.text = "Предмет полностью починен.";
            Refresh();
        }

        private void Upgrade()
        {
            if (toolSlot == null || artifactSlot == null) return;
            bool success = DurabilitySystem.ApplyUpgrade(toolSlot.Stack, artifactSlot.Stack, out string reason);
            infoText.text = reason;
            if (success && inventory != null) inventory.NotifyExternalChange();
            Refresh();
        }

        private void Refresh()
        {
            if (panel == null || toolSlot == null || artifactSlot == null) return;

            toolSlot.Refresh();
            artifactSlot.Refresh();

            ItemStack tool = toolSlot.Stack;
            ItemStack artifact = artifactSlot.Stack;

            if (tool == null || tool.IsEmpty || !DurabilitySystem.HasDurability(tool))
            {
                repairButton.interactable = false;
                upgradeButton.interactable = false;
                repairText.text = "Помести предмет с прочностью в левый слот.";
                return;
            }

            DurabilitySystem.EnsureInitialized(tool);
            int current = DurabilitySystem.GetCurrentDurability(tool);
            int max = DurabilitySystem.GetMaxDurability(tool);
            int cost = DurabilitySystem.GetRepairCost(tool);

            bool recipe = DurabilitySystem.TryGetRepairRecipe(tool, out string repairItemId, out _);
            int owned = recipe && inventory != null ? inventory.CountItem(repairItemId) : 0;
            repairButton.interactable = recipe && cost > 0 && owned >= cost;

            repairText.text = recipe
                ? $"Прочность: {current} / {max}\nРемонт: {cost} x {repairItemId} (есть {owned})"
                : $"Прочность: {current} / {max}\nRepairItem / RepairItemCount не настроены.";

            upgradeButton.interactable = DurabilitySystem.CanApplyUpgrade(tool, artifact, out _);
        }

        private void BuildUI()
        {
            if (panel != null) return;

            Canvas canvas = InventoryUI.Instance != null
                ? InventoryUI.Instance.GetComponentInParent<Canvas>()
                : null;
            if (canvas == null) canvas = FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "ToolBenchCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            panel = new GameObject("ToolBenchPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 1f);
            pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 1f);
            pr.sizeDelta = new Vector2(560f, 260f);
            pr.anchoredPosition = new Vector2(0f, -24f);
            panel.GetComponent<Image>().color = new Color(0.015f, 0.015f, 0.02f, 0.97f);

            CreateText(panel.transform, "Title", "TOOL BENCH", new Vector2(0f, -24f), new Vector2(360f, 36f), 22, TextAnchor.MiddleCenter);

            toolSlot = CreateSlot(panel.transform, "ToolSlot", new Vector2(-90f, -96f), ToolBenchSlotKind.Tool);
            artifactSlot = CreateSlot(panel.transform, "ArtifactSlot", new Vector2(90f, -96f), ToolBenchSlotKind.Artifact);
            CreateText(panel.transform, "Plus", "+", new Vector2(0f, -96f), new Vector2(50f, 50f), 26, TextAnchor.MiddleCenter);

            repairButton = CreateButton(panel.transform, "RepairButton", "ПОЧИНИТЬ", new Vector2(-95f, -180f), new Vector2(165f, 38f), Repair);
            upgradeButton = CreateButton(panel.transform, "UpgradeButton", "УЛУЧШИТЬ", new Vector2(95f, -180f), new Vector2(165f, 38f), Upgrade);

            repairText = CreateText(panel.transform, "RepairInfo", "", new Vector2(-145f, -226f), new Vector2(420f, 52f), 13, TextAnchor.MiddleLeft);
            infoText = CreateText(panel.transform, "Status", "", new Vector2(170f, -226f), new Vector2(300f, 52f), 13, TextAnchor.MiddleRight);

            toolSlot.Changed += Refresh;
            artifactSlot.Changed += Refresh;
            panel.SetActive(false);
        }

        private ToolBenchSlotUI CreateSlot(Transform parent, string name, Vector2 position, ToolBenchSlotKind kind)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ToolBenchSlotUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(62f, 62f);
            rect.anchoredPosition = position;

            ToolBenchSlotUI slot = go.GetComponent<ToolBenchSlotUI>();
            slot.Configure(kind);
            return slot;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1f);

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(action);

            Text text = CreateText(go.transform, "Label", label, Vector2.zero, size, 14, TextAnchor.MiddleCenter);
            RectTransform tr = text.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            return button;
        }

        private Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text text = go.GetComponent<Text>();
            text.font = GetRuntimeFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }
}
