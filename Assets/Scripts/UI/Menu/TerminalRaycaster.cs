using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalRaycaster : GraphicRaycaster
    {
        [SerializeField] private TerminalDisplay display;

        // Ask TerminalDisplay where the mouse landed on the telescope, then let Unity find the button.
        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            Vector2 original = eventData.position;
            if (!display.TryGetContentPoint(original, out Vector2 point))
                return;
            eventData.position = point;
            base.Raycast(eventData, results);
            eventData.position = original;
        }
    }
}
