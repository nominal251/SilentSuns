using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalMenuOption : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private TMP_Text commandLine;
        [SerializeField] private string command;
        [SerializeField] private Graphic arrow;
        [SerializeField] private Graphic edge;
        private readonly Color idleEdge = new Color32(115, 124, 185, 255);
        private readonly Color selectedEdge = new Color32(239, 240, 255, 255);

        // Only the active option carries the bright edge and arrow.
        private void OnEnable()
        {
            arrow.enabled = false;
            edge.color = idleEdge;
            transform.Find("Label").GetComponent<TMP_Text>().color = new Color32(222,222,230,255);
        }

        // Keyboard and controller focus update the same command readout.
        public void OnSelect(BaseEventData eventData)
        {
            commandLine.text = "> " + command;
            arrow.enabled = true;
            edge.color = selectedEdge;
            transform.Find("Label").GetComponent<TMP_Text>().color = new Color32(0,0,100,255);
            arrow.color = new Color32(0,0,100,255);
        }

        // Leave a quiet outline on the remaining options.
        public void OnDeselect(BaseEventData eventData)
        {
            arrow.enabled = false;
            edge.color = idleEdge;
            transform.Find("Label").GetComponent<TMP_Text>().color = new Color32(222,222,230,255);
        }

        // Hovering an option moves the terminal selection to the mouse.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!GetComponent<Selectable>().IsInteractable())
                return;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
