using UnityEngine;
using UnityEngine.EventSystems;

namespace SilentSuns.Menu
{
    public class TerminalPowerButton : TerminalButton
    {
        [SerializeField] private TerminalMenu menu;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (menu.CanTogglePower) base.OnPointerClick(eventData);
        }

        // Enter remains a menu confirmation key once the terminal has power.
        public override void OnSubmit(BaseEventData eventData)
        {
            if (menu.State == TerminalMenu.PowerState.WaitingForTouch && menu.CanTogglePower)
                base.OnSubmit(eventData);
        }
    }
}
