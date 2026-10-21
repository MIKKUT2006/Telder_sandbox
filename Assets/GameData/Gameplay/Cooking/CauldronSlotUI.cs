using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Cooking
{
    public sealed class CauldronSlotUI : MonoBehaviour, IPointerClickHandler
    {
        internal CauldronUI Owner;
        internal int Index;
        public void OnPointerClick(PointerEventData e)
        {
            Owner?.Click(Index, e.button == PointerEventData.InputButton.Right, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
    }
}
