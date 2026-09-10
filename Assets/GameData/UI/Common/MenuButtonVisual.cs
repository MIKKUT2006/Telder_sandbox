using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Game.UI
{
    public class MenuButtonVisual :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField]
        private Image image;


        [Header("Sprites")]

        [SerializeField]
        private Sprite normalSprite;

        [SerializeField]
        private Sprite hoverSprite;

        [SerializeField]
        private Sprite pressedSprite;


        [Header("Text")]

        [SerializeField]
        private TMP_FontAsset pixelFont;


        private bool hovered;


        private void Awake()
        {
            if (
                image == null
            )
            {
                image =
                    GetComponent<Image>();
            }


            ApplyFont();

            SetNormal();
        }


        public void OnPointerEnter(
            PointerEventData eventData
        )
        {
            hovered =
                true;

            SetSprite(
                hoverSprite != null
                    ? hoverSprite
                    : normalSprite
            );
        }


        public void OnPointerExit(
            PointerEventData eventData
        )
        {
            hovered =
                false;

            SetNormal();
        }


        public void OnPointerDown(
            PointerEventData eventData
        )
        {
            SetSprite(
                pressedSprite != null
                    ? pressedSprite
                    : (
                        hoverSprite != null
                            ? hoverSprite
                            : normalSprite
                    )
            );
        }


        public void OnPointerUp(
            PointerEventData eventData
        )
        {
            if (
                hovered &&
                hoverSprite != null
            )
            {
                SetSprite(
                    hoverSprite
                );
            }
            else
            {
                SetNormal();
            }
        }


        private void SetNormal()
        {
            SetSprite(
                normalSprite
            );
        }


        private void SetSprite(
            Sprite sprite
        )
        {
            if (
                image != null &&
                sprite != null
            )
            {
                image.sprite =
                    sprite;
            }
        }


        private void ApplyFont()
        {
            if (
                pixelFont == null
            )
            {
                return;
            }


            TMP_Text[] texts =
                GetComponentsInChildren<
                    TMP_Text
                >(
                    true
                );


            for (
                int i = 0;
                i < texts.Length;
                i++
            )
            {
                texts[i].font =
                    pixelFont;
            }
        }
    }
}
