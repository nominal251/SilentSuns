using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalButton : Button
    {
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            if (targetGraphic == null || targetGraphic.color.a == 0f)
                return;
            bool selected = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
            Color ink = selected ? new Color32(0, 0, 100, 255) : new Color32(208, 210, 215, 255);
            Transform label = transform.Find("Label");
            if (label != null) label.GetComponent<TMP_Text>().color = ink;
            Transform arrow = transform.Find("Arrow");
            if (arrow != null) arrow.GetComponent<TMP_Text>().color = ink;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (TerminalBindings.Listening || Time.frameCount <= TerminalBindings.ConsumedFrame) return;
            if (!IsActive() || !IsInteractable() || eventData.button != PointerEventData.InputButton.Left)
                return;
            TerminalInteractionAudio.Instance?.Mouse();
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (TerminalBindings.Listening || Time.frameCount <= TerminalBindings.ConsumedFrame) return;
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
