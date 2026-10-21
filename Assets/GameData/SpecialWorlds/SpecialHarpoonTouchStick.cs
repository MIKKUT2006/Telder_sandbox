using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.SpecialWorlds
{
    /// <summary>One-pointer-per-stick virtual thumbstick, safe for multitouch.</summary>
    public sealed class SpecialHarpoonTouchStick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform rect;
        private int pointerId = int.MinValue;
        private Action<Vector2> changed;
        public void Initialize(Action<Vector2> onChange)
        {
            rect = transform as RectTransform;
            changed = onChange;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (pointerId != int.MinValue) return;
            pointerId = data.pointerId;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId != pointerId) return;
            if (rect == null) rect = transform as RectTransform;
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,
                data.position, data.pressEventCamera, out local))
            {
                var value = Vector2.ClampMagnitude(local /
                    Mathf.Max(1f, Mathf.Min(rect.rect.width, rect.rect.height) * .43f), 1f);
                if (changed != null) changed(value);
            }
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId != pointerId) return;
            pointerId = int.MinValue;
            if (changed != null) changed(Vector2.zero);
        }
        private void OnDisable()
        {
            pointerId = int.MinValue;
            if (changed != null) changed(Vector2.zero);
        }
    }
}
