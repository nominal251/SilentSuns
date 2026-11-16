using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalSlider : Slider
    {
        [SerializeField] private TerminalDisplay display;
        public void SetDisplay(TerminalDisplay value) { display = value; }

        // Slider dragging needs the same coordinates as the curved screen's raycaster.
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
                TerminalInteractionAudio.Instance?.Mouse();
            Vector2 original = eventData.position;
            if (display != null)
            {
                display.TryGetContentPoint(original, out Vector2 point);
                eventData.position = point;
            }
            base.OnPointerDown(eventData);
            eventData.position = original;
        }

        public override void OnMove(AxisEventData eventData)
        {
            float previousValue = value;
            var previousSelection = EventSystem.current.currentSelectedGameObject;
            base.OnMove(eventData);
            if (previousValue != value || previousSelection != EventSystem.current.currentSelectedGameObject)
                TerminalInteractionAudio.Instance?.Keyboard();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            Vector2 original = eventData.position;
            if (display != null)
            {
                display.TryGetContentPoint(original, out Vector2 point);
                eventData.position = point;
            }
            base.OnDrag(eventData);
            eventData.position = original;
        }
    }
}
