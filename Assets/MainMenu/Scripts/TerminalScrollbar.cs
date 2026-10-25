using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // The ship screen is a texture. Its scrollbar needs that texture's pointer coordinates.
    public class TerminalScrollbar : Scrollbar
    {
        private TerminalDisplay display;

        protected override void Start()
        {
            base.Start();
            var menu = GetComponentInParent<TerminalMenu>();
            if (menu != null)
                display = menu.GetComponentInChildren<TerminalDisplay>(true);
        }

        private Vector2 Map(Vector2 position)
        {
            return display != null && display.TryGetContentPoint(position, out var point) ? point : position;
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
