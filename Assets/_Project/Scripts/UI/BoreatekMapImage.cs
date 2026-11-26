using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boreatek.UI
{
    internal sealed class BoreatekMapImage : RawImage, IPointerClickHandler, IScrollHandler, IDragHandler
    {
        internal Action<Vector2> MapClick;
        internal Action<float> Zoom;
        internal Action<Vector2> Pan;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || MapClick == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                eventData.pressEventCamera, out Vector2 local)) return;
            Rect rect = rectTransform.rect;
            MapClick(new Vector2(Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y)));
        }

        public void OnScroll(PointerEventData eventData) => Zoom?.Invoke(eventData.scrollDelta.y);

        public void OnDrag(PointerEventData eventData)
        {
            if (Pan == null || eventData.button == PointerEventData.InputButton.Left) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position,
                eventData.pressEventCamera, out Vector2 point) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position - eventData.delta,
                    eventData.pressEventCamera, out Vector2 previous))
            {
                Pan(Vector2.Scale(point - previous, new Vector2(1f / rectTransform.rect.width, 1f / rectTransform.rect.height)));
            }
        }
    }
}
