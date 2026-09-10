using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Game.Inventory.UI
{

    public class InventorySlotUI :
        MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {

        // =====================================================
        // UI
        // =====================================================

        [SerializeField]
        private Image backgroundImage;


        [SerializeField]
        private Image iconImage;


        [SerializeField]
        private TMP_Text countText;


        [SerializeField]
        private GameObject selectedFrame;


        // =====================================================
        // DATA
        // =====================================================

        private InventoryUI owner;


        private int slotIndex;


        public int SlotIndex =>
            slotIndex;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            if (
                backgroundImage == null
            )
            {

                backgroundImage =
                    GetComponent<Image>();

            }

        }


        // =====================================================
        // BIND
        // =====================================================

        public void Bind(
            InventoryUI inventoryUI,
            int index
        )
        {

            owner =
                inventoryUI;


            slotIndex =
                index;

        }


        // =====================================================
        // REFRESH
        // =====================================================

        public void Refresh(
            ItemStack stack,
            bool selected,
            float selectedScale = 1.04f
        )
        {

            transform.localScale =
                selected
                    ? Vector3.one *
                      selectedScale
                    : Vector3.one;


            if (
                selectedFrame != null
            )
            {

                selectedFrame.SetActive(
                    selected
                );

            }


            if (
                stack == null
                ||
                stack.IsEmpty
            )
            {

                if (
                    iconImage != null
                )
                {

                    iconImage.sprite =
                        null;


                    iconImage.enabled =
                        false;

                }


                if (
                    countText != null
                )
                {

                    countText.text =
                        string.Empty;

                }


                return;

            }


            if (
                iconImage != null
            )
            {

                iconImage.sprite =
                    ItemIconProvider.GetIcon(
                        stack.ItemId
                    );


                iconImage.enabled =
                    iconImage.sprite != null;

            }


            if (
                countText != null
            )
            {

                countText.text =
                    stack.Count > 1
                        ? stack.Count.ToString()
                        : string.Empty;

            }

        }


        // =====================================================
        // BACKGROUND
        // =====================================================

        public void ApplyBackground(
            Sprite normalSprite,
            Sprite selectedSprite,
            bool selected
        )
        {

            if (
                backgroundImage == null
            )
            {

                backgroundImage =
                    GetComponent<Image>();

            }


            if (
                backgroundImage == null
            )
            {

                return;

            }


            Sprite target =
                selected
                &&
                selectedSprite != null
                    ? selectedSprite
                    : normalSprite;


            if (
                target != null
            )
            {

                backgroundImage.sprite =
                    target;

            }

        }


        // =====================================================
        // LEFT CLICK
        // =====================================================

        public void OnPointerClick(
            PointerEventData eventData
        )
        {

            if (
                owner == null
            )
            {

                return;

            }


            if (
                eventData.button ==
                PointerEventData.InputButton.Left
            )
            {

                owner.LeftClick(
                    slotIndex
                );

            }

        }


        // =====================================================
        // RIGHT BUTTON DOWN
        // =====================================================
        //
        // ПКМ обрабатывается на MouseDown,
        // чтобы можно было сразу начать drag-раздачу.
        //
        // =====================================================

        public void OnPointerDown(
            PointerEventData eventData
        )
        {

            if (
                owner == null
            )
            {

                return;

            }


            if (
                eventData.button ==
                PointerEventData.InputButton.Right
            )
            {

                owner.RightPointerDown(
                    slotIndex
                );

            }

        }


        // =====================================================
        // HOVER
        // =====================================================

        public void OnPointerEnter(
            PointerEventData eventData
        )
        {

            owner?.SetHoveredSlot(
                this
            );

        }


        public void OnPointerExit(
            PointerEventData eventData
        )
        {

            owner?.ClearHoveredSlot(
                this
            );

        }

    }

}
