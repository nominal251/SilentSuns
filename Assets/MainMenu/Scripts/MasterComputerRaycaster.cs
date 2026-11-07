using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class MasterComputerRaycaster : GraphicRaycaster
    {
        public MasterComputerDisplay Display { get; set; }
        public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
        {
            Vector2 original = eventData.position;
            if (Display == null || !Display.TryGetContentPoint(original, out Vector2 point)) return;
            // Convert a click on the physical glass into a position on its UI texture.
            eventData.position = point;
            base.Raycast(eventData, results);
            eventData.position = original;
        }
    }
}
