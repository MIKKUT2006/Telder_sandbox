using UnityEngine;
using UnityEngine.EventSystems;
using Game.UI.Tooltips;

namespace Game.Crafting.UI
{
    public class CraftIngredientIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string itemId;
        public void Bind(string id) { itemId = id; }
        public void OnPointerEnter(PointerEventData eventData) { ItemTooltipUI.ShowItem(itemId, eventData.position, 30f); }
        public void OnPointerExit(PointerEventData eventData) { ItemTooltipUI.Hide(); }
    }
}
