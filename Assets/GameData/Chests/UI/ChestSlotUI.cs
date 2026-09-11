using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Game.Inventory;
using Game.Inventory.UI;


namespace Game.Chests.UI
{

    public class ChestSlotUI :
        MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {

        [SerializeField]
        private Image backgroundImage;


        [SerializeField]
        private Image iconImage;


        [SerializeField]
        private TMP_Text countText;


        private ChestUIController owner;


        private int slotIndex;


        public int SlotIndex =>
            slotIndex;


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


        public void Bind(
            ChestUIController controller,
            int index
        )
        {

            owner =
                controller;


            slotIndex =
                index;

        }


        public void Refresh(
            ItemStack stack,
            Sprite slotSprite
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
                backgroundImage != null
                &&
                slotSprite != null
            )
            {

                backgroundImage.sprite =
                    slotSprite;

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

                owner.LeftClickChest(
                    slotIndex
                );

            }

        }


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
