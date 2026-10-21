using Game.Inventory.UI;
using Game.UI.Tooltips;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Crafting.UI
{
    public class CraftIngredientIconUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private float cycleInterval = 1f;

        private Image icon;

        // Œ‰ËÌ ËÁ ‰‚Ûı ‚‡Ë‡ÌÚÓ‚.
        private string itemId;
        private string tag;

        private readonly List<string> tagItems = new List<string>();
        private int currentIndex;
        private float timer;

        public string CurrentItemId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(itemId))
                    return itemId;

                if (tagItems.Count > 0)
                    return tagItems[currentIndex];

                return null;
            }
        }

        private void Awake()
        {
            icon = GetComponent<Image>();
        }

        // ----- œ–»¬ﬂ« ¿ -----

        public void Bind(string id)
        {
            itemId = id;
            tag = null;
            tagItems.Clear();
            currentIndex = 0;
            timer = 0f;
            UpdateIcon();
        }

        public void BindTag(string tagName, IList<string> items)
        {
            itemId = null;
            tag = tagName;
            tagItems.Clear();

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(items[i]))
                        tagItems.Add(items[i]);
                }
            }

            currentIndex = 0;
            timer = 0f;
            UpdateIcon();
        }

        // ----- ÷» À -----

        private void Update()
        {
            if (tagItems.Count <= 1)
                return;

            timer += Time.unscaledDeltaTime;

            if (timer < cycleInterval)
                return;

            timer -= cycleInterval;

            currentIndex++;
            if (currentIndex >= tagItems.Count)
                currentIndex = 0;

            UpdateIcon();
        }

        private void UpdateIcon()
        {
            if (icon == null)
                icon = GetComponent<Image>();

            if (icon == null)
                return;

            string id = CurrentItemId;

            if (string.IsNullOrWhiteSpace(id))
            {
                icon.enabled = false;
                return;
            }

            Sprite sprite = ItemIconProvider.GetIcon(id);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.enabled = sprite != null;
        }

        // ----- “”À“»œ -----

        public void OnPointerEnter(PointerEventData eventData)
        {
            string id = CurrentItemId;

            if (!string.IsNullOrWhiteSpace(id))
                ItemTooltipUI.ShowItem(id, eventData.position, 30f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Hide();
        }
    }
}