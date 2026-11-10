using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // The ship screen is a texture. Its scrollbar needs that texture's pointer coordinates.
    public class TerminalScrollbar : Scrollbar
    {
        private TerminalDisplay display;
        private MasterComputerRaycaster computer;

        protected override void Start()
        {
            base.Start();
            var menu = GetComponentInParent<TerminalMenu>();
            if (menu != null)
                display = menu.GetComponentInChildren<TerminalDisplay>(true);
            computer = GetComponentInParent<MasterComputerRaycaster>();
        }

        private Vector2 Map(Vector2 position)
        {
            if (display != null && display.TryGetContentPoint(position, out var point)) return point;
            if (computer != null && computer.Display.TryGetContentPoint(position, out point)) return point;
            return position;
        }

        public override void OnPointerDown(PointerEventData data)
        {
            Vector2 original = data.position;
            data.position = Map(original);
            base.OnPointerDown(data);
            data.position = original;
        }

        public override void OnBeginDrag(PointerEventData data)
        {
            Vector2 original = data.position;
            data.position = Map(original);
            base.OnBeginDrag(data);
            data.position = original;
        }

        public override void OnDrag(PointerEventData data)
        {
            Vector2 original = data.position;
            data.position = Map(original);
            base.OnDrag(data);
            data.position = original;
        }
    }
}
