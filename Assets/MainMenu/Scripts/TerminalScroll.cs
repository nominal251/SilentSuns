using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalScroll : ScrollRect
    {
        private TerminalDisplay display;
        private bool easing;
        private bool updatingScrollbar;
        private float targetY, easeVelocity;
        public void SmoothTo(float y)
        {
            Canvas.ForceUpdateCanvases();
            targetY = Mathf.Clamp(y, 0, Mathf.Max(0, content.rect.height - viewport.rect.height));
            easing = true;
            StopMovement();
        }
        public void ResetPosition()
        {
            easing = false;
            easeVelocity = 0;
            StopMovement();
            content.anchoredPosition = Vector2.zero;
        }
        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive()) return;
            // Wheel input can arrive as large steps. Ease toward a bounded target instead of jumping.
            float wheel = Mathf.Clamp(data.scrollDelta.y, -3, 3);
            SmoothTo((easing ? targetY : content.anchoredPosition.y) - wheel * 35);
        }
        protected override void LateUpdate()
        {
            updatingScrollbar = true;
            base.LateUpdate();
            updatingScrollbar = false;
            if (!easing || content == null) return;
            float y = Mathf.SmoothDamp(content.anchoredPosition.y, targetY, ref easeVelocity, .14f,
                Mathf.Infinity, Time.unscaledDeltaTime);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
            if (Mathf.Abs(y - targetY) < .1f) { easing = false; easeVelocity = 0; }
        }
        protected override void SetNormalizedPosition(float value, int axis)
        {
            if (!updatingScrollbar) { easing = false; easeVelocity = 0; }
            base.SetNormalizedPosition(value, axis);
        }
        protected override void Start()
        {
            base.Start();
            var menu = GetComponentInParent<TerminalMenu>();
            if (menu != null)
                display = menu.GetComponentInChildren<TerminalDisplay>(true);
        }
        public override void OnBeginDrag(PointerEventData data)
        {
            easing = false;
            easeVelocity = 0;
            Vector2 original = data.position;
            if (display != null && display.TryGetContentPoint(original, out var point))
                data.position = point;
            base.OnBeginDrag(data);
            data.position = original;
        }
        public override void OnDrag(PointerEventData data)
        {
            Vector2 original = data.position;
            if (display != null && display.TryGetContentPoint(original, out var point))
                data.position = point;
            base.OnDrag(data);
            data.position = original;
        }
    }
}
