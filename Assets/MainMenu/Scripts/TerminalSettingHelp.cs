using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalSettingHelp : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public Action show;
        public void OnPointerEnter(PointerEventData e)
        {
            show?.Invoke();
        }
        public void OnSelect(BaseEventData e)
        {
            show?.Invoke();
            // Keep keyboard/controller focus visible in long settings lists.
            var scroll = GetComponentInParent<ScrollRect>();
            if (scroll == null)
                return;
            var rect = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, rect);
            float shift = bounds.max.y > scroll.viewport.rect.yMax ? scroll.viewport.rect.yMax - bounds.max.y :
                bounds.min.y < scroll.viewport.rect.yMin ? scroll.viewport.rect.yMin - bounds.min.y : 0;
            if (scroll is TerminalScroll smooth)
                smooth.SmoothTo(scroll.content.anchoredPosition.y + shift);
            else scroll.content.anchoredPosition += Vector2.up * shift;
        }
    }
}
