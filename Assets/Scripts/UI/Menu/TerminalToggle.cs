using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalToggle : Toggle
    {
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
                return;
            TerminalInteractionAudio.Instance?.Mouse();
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;
            TerminalInteractionAudio.Instance?.Keyboard();
            base.OnSubmit(eventData);
        }

        public override void OnMove(AxisEventData eventData)
        {
            var previous = EventSystem.current.currentSelectedGameObject;
            base.OnMove(eventData);
            if (previous != EventSystem.current.currentSelectedGameObject)
                TerminalInteractionAudio.Instance?.Keyboard();
        }
    }
}
