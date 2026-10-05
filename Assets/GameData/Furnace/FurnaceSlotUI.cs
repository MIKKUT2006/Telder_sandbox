using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Inventory.UI;
using Game.UI.Tooltips;

namespace Game.GameplaySystems.Furnace
{
    public enum FurnaceSlotKind
    {
        Input,
        Fuel,
        Output
    }

    /// <summary>
    /// One furnace slot rendered with the same icon/count interaction model as
    /// the normal inventory. The actual stack remains in FurnaceState so the
    /// furnace can keep running while the UI is closed.
    /// </summary>
    public sealed class FurnaceSlotUI :
        MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private FurnaceUI owner;
        private FurnaceSlotKind kind;
        private Image iconImage;
        private TMP_Text countText;

        private string lastItemId;
        private int lastCount = int.MinValue;

        public FurnaceSlotKind Kind => kind;

        public void Configure(
            FurnaceUI furnaceUI,
            FurnaceSlotKind slotKind,
            Image icon,
            TMP_Text count)
        {
            owner = furnaceUI;
            kind = slotKind;
            iconImage = icon;
            countText = count;
        }

        public void Refresh(string itemId, int count)
        {
            if (lastCount == count && lastItemId == itemId)
                return;

            lastItemId = itemId;
            lastCount = count;

            bool hasItem =
                !string.IsNullOrWhiteSpace(itemId)
                &&
                count > 0;

            if (iconImage != null)
            {
                iconImage.sprite =
                    hasItem
                        ? ItemIconProvider.GetIcon(itemId)
                        : null;

                iconImage.enabled =
                    hasItem
                    &&
                    iconImage.sprite != null;
            }

            if (countText != null)
            {
                countText.text =
                    hasItem && count > 1
                        ? count.ToString()
                        : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
                owner.LeftClick(kind);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
                owner.RightPointerDown(kind);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.SetHoveredSlot(this);

            if (owner == null)
                return;

            if (owner.TryGetStack(kind, out string itemId, out int count) &&
                count > 0 &&
                !string.IsNullOrWhiteSpace(itemId))
            {
                ItemTooltipUI.ShowItem(
                    itemId,
                    eventData.position,
                    38f
                );
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.ClearHoveredSlot(this);
            ItemTooltipUI.Hide();
        }
    }
}
