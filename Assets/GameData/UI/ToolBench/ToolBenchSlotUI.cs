using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Items;
using Game.Items.Durability;
using Game.World.Items;

namespace Game.ToolBench
{
    public enum ToolBenchSlotKind
    {
        Tool,
        Artifact
    }

    public class ToolBenchSlotUI : MonoBehaviour, IPointerClickHandler
    {
        public event Action Changed;

        private readonly ItemStack stack = new ItemStack();
        private ToolBenchSlotKind kind;
        private Image icon;
        private Text countText;
        private RectTransform durabilityRoot;
        private RectTransform durabilityFill;
        private Image durabilityFillImage;

        private static readonly Color FullColor = new Color(0.12f, 0.48f, 1f, 1f);
        private static readonly Color BrokenColor = new Color(0.95f, 0.08f, 0.08f, 1f);

        public ItemStack Stack => stack;

        public void Configure(ToolBenchSlotKind value)
        {
            kind = value;
            BuildVisual();
            Refresh();
        }

        public void Refresh()
        {
            BuildVisual();

            if (stack.IsEmpty)
            {
                icon.enabled = false;
                countText.text = "";
                durabilityRoot.gameObject.SetActive(false);
                return;
            }

            icon.sprite = ItemIconProvider.GetIcon(stack.ItemId);
            icon.enabled = icon.sprite != null;
            countText.text = stack.Count > 1 ? stack.Count.ToString() : "";

            bool showDurability = kind == ToolBenchSlotKind.Tool && DurabilitySystem.HasDurability(stack);
            durabilityRoot.gameObject.SetActive(showDurability);

            if (showDurability)
            {
                float n = DurabilitySystem.GetNormalized(stack);
                Vector2 max = durabilityFill.anchorMax;
                max.x = n;
                durabilityFill.anchorMax = max;
                durabilityFillImage.color = Color.Lerp(BrokenColor, FullColor, n);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            InventoryUI ui = InventoryUI.Instance;
            ItemStack cursor = InventoryUIAccess.GetCursor(ui);
            if (ui == null || cursor == null) return;

            bool changed = false;
            if (eventData.button == PointerEventData.InputButton.Left)
                changed = LeftClick(cursor);
            else if (eventData.button == PointerEventData.InputButton.Right)
                changed = RightClick(cursor);

            if (!changed) return;
            Refresh();
            InventoryUIAccess.RefreshAll(ui);
            Changed?.Invoke();
        }

        private bool LeftClick(ItemStack cursor)
        {
            if (cursor.IsEmpty)
            {
                if (stack.IsEmpty) return false;
                cursor.Set(stack);
                stack.Clear();
                return true;
            }

            if (!Accepts(cursor)) return false;

            if (stack.IsEmpty)
            {
                stack.Set(cursor);
                cursor.Clear();
                return true;
            }

            ItemStack old = stack.Clone();
            stack.Set(cursor);
            cursor.Set(old);
            return true;
        }

        private bool RightClick(ItemStack cursor)
        {
            if (cursor.IsEmpty)
            {
                if (stack.IsEmpty) return false;

                if (kind == ToolBenchSlotKind.Tool)
                {
                    cursor.Set(stack);
                    stack.Clear();
                }
                else
                {
                    cursor.Set(stack.ItemId, 1);
                    stack.Count--;
                }
                return true;
            }

            if (!Accepts(cursor)) return false;

            if (kind == ToolBenchSlotKind.Tool)
            {
                if (!stack.IsEmpty) return false;
                stack.Set(cursor);
                cursor.Clear();
                return true;
            }

            if (stack.IsEmpty)
            {
                stack.Set(cursor.ItemId, 1);
                cursor.Count--;
                return true;
            }

            if (stack.ItemId != cursor.ItemId) return false;
            if (!ItemRegistry.TryGet(stack.ItemId, out ItemDefinition def)) return false;

            int maxStack = Mathf.Max(1, def.GetMaxStack());
            if (stack.Count >= maxStack) return false;

            stack.Count++;
            cursor.Count--;
            return true;
        }

        private bool Accepts(ItemStack candidate)
        {
            if (candidate == null || candidate.IsEmpty) return false;
            return kind == ToolBenchSlotKind.Tool
                ? DurabilitySystem.HasDurability(candidate)
                : DurabilitySystem.IsToolArtifact(candidate);
        }

        private void BuildVisual()
        {
            if (icon != null) return;

            Image background = GetComponent<Image>();
            if (background == null) background = gameObject.AddComponent<Image>();
            background.color = new Color(0.10f, 0.10f, 0.12f, 0.98f);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countObject.transform.SetParent(transform, false);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(4f, 2f);
            countRect.offsetMax = new Vector2(-4f, -2f);
            countText = countObject.GetComponent<Text>();
            countText.font = ToolBenchUI.GetRuntimeFont();
            countText.fontSize = 14;
            countText.alignment = TextAnchor.LowerRight;
            countText.color = Color.white;
            countText.raycastTarget = false;

            GameObject durabilityObject = new GameObject("Durability", typeof(RectTransform), typeof(Image));
            durabilityObject.transform.SetParent(transform, false);
            durabilityRoot = durabilityObject.GetComponent<RectTransform>();
            durabilityRoot.anchorMin = new Vector2(0.08f, 0.04f);
            durabilityRoot.anchorMax = new Vector2(0.92f, 0.10f);
            durabilityRoot.offsetMin = Vector2.zero;
            durabilityRoot.offsetMax = Vector2.zero;
            Image durabilityBg = durabilityObject.GetComponent<Image>();
            durabilityBg.color = new Color(0f, 0f, 0f, 0.75f);
            durabilityBg.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(durabilityObject.transform, false);
            durabilityFill = fillObject.GetComponent<RectTransform>();
            durabilityFill.anchorMin = Vector2.zero;
            durabilityFill.anchorMax = Vector2.one;
            durabilityFill.offsetMin = Vector2.zero;
            durabilityFill.offsetMax = Vector2.zero;
            durabilityFillImage = fillObject.GetComponent<Image>();
            durabilityFillImage.raycastTarget = false;
            durabilityFillImage.color = FullColor;
        }
    }
}
