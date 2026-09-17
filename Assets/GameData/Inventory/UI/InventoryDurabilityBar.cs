using UnityEngine;
using UnityEngine.UI;
using Game.Inventory;
using Game.Items.Durability;

namespace Game.Inventory.UI
{
    [DisallowMultipleComponent]
    public class InventoryDurabilityBar : MonoBehaviour
    {
        private InventorySlotUI slotUI;
        private PlayerInventory inventory;
        private RectTransform root;
        private RectTransform fill;
        private Image fillImage;

        private static readonly Color FullColor = new Color(0.12f, 0.48f, 1f, 1f);
        private static readonly Color BrokenColor = new Color(0.95f, 0.08f, 0.08f, 1f);

        private void Awake()
        {
            slotUI = GetComponent<InventorySlotUI>();
            BuildVisual();
        }

        private void Start() => FindInventory();

        private void LateUpdate()
        {
            if (slotUI == null) return;
            if (inventory == null) FindInventory();

            if (inventory == null || slotUI.SlotIndex < 0)
            {
                SetVisible(false);
                return;
            }

            ItemStack stack = inventory.GetSlot(slotUI.SlotIndex);
            if (stack == null || stack.IsEmpty || !DurabilitySystem.HasDurability(stack))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            float normalized = DurabilitySystem.GetNormalized(stack);
            Vector2 max = fill.anchorMax;
            max.x = normalized;
            fill.anchorMax = max;
            fillImage.color = Color.Lerp(BrokenColor, FullColor, normalized);
        }

        private void FindInventory()
        {
            inventory = FindObjectOfType<PlayerInventory>();
        }

        private void BuildVisual()
        {
            if (root != null) return;

            GameObject rootObject = new GameObject("DurabilityBar", typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.08f, 0.045f);
            root.anchorMax = new Vector2(0.92f, 0.105f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Image background = rootObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.72f);
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(rootObject.transform, false);
            fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;

            fillImage = fillObject.GetComponent<Image>();
            fillImage.color = FullColor;
            fillImage.raycastTarget = false;

            rootObject.transform.SetAsLastSibling();
            SetVisible(false);
        }

        private void SetVisible(bool value)
        {
            if (root != null && root.gameObject.activeSelf != value)
                root.gameObject.SetActive(value);
        }
    }
}
