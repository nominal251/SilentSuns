using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Releasing the key or moving away cancels the hold.
    public class TerminalHoldConfirm : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        private Action confirm;
        private TMP_Text label;
        private string original;
        private float elapsed;
        private bool pointer;
        private bool holding;
        private RectTransform fill;
        private GameObject progressBar;
        private TMP_Text emptyText, filledText;
        public float Progress => Mathf.Clamp01(elapsed / 1.1f);
        public bool Completed
        {
            get; private set;
        }

        public static TerminalHoldConfirm Attach(GameObject button, Action action)
        {
            var hold = button.AddComponent<TerminalHoldConfirm>();
            hold.confirm = action;
            hold.label = button.transform.Find("Label").GetComponent<TMP_Text>();
            hold.original = hold.label.text;
            hold.label.text = hold.original + " [HOLD]";
            var track = TerminalUI.Panel("Hold progress", button.transform, 0, 0, 1, 1, TerminalUI.Dark);
            track.raycastTarget = false;
            hold.progressBar = track.gameObject;
            hold.emptyText = TerminalUI.Text("Hold label", track.transform, "", 0, 0, 1, 1);
            var white = TerminalUI.Panel("White fill", track.transform, 0, 0, 0, 1, Color.white);
            white.raycastTarget = false;
            white.gameObject.AddComponent<RectMask2D>();
            hold.fill = white.rectTransform;
            hold.filledText = TerminalUI.Text("Filled label", white.transform, "", 0, 0, 1, 1);
            hold.filledText.color = TerminalUI.Blue;
            track.gameObject.SetActive(false);
            return hold;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
            {
                pointer = true;
                holding = true;
            }
        }
        public void OnPointerUp(PointerEventData e)
        {
            Cancel();
        }
        public void OnPointerExit(PointerEventData e)
        {
            Cancel();
        }
        public void OnSubmit(BaseEventData e)
        {
            pointer = false;
            holding = true;
        }
        private void Update()
        {
            if (!holding)
                return;
            if (!pointer && EventSystem.current?.currentSelectedGameObject != gameObject)
            {
                Cancel();
                return;
            }
            bool pressed = pointer ? Mouse.current?.leftButton.isPressed == true :
                Keyboard.current?.enterKey.isPressed == true || Keyboard.current?.numpadEnterKey.isPressed == true || Gamepad.current?.buttonSouth.isPressed == true;
            if (!pressed || TerminalModal.BlocksInput)
            {
                Cancel();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            progressBar.SetActive(true);
            fill.anchorMax = new Vector2(Progress, 1);
            label.text = "HOLD " + Mathf.Clamp(Mathf.FloorToInt(elapsed / 1.1f * 100), 0, 100) + "%";
            AlignProgressLabel(emptyText);
            AlignProgressLabel(filledText);
            if (elapsed < 1.1f)
                return;
            Completed = true;
            holding = false;
            confirm?.Invoke();
        }
        private void AlignProgressLabel(TMP_Text text)
        {
            // The dark copy is clipped by the white fill, keeping the words readable on both sides.
            text.text = label.text;
            text.enableAutoSizing = false;
            text.fontSize = label.fontSize;
            text.alignment = label.alignment;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = label.rectTransform.rect.size;
            rect.position = label.rectTransform.TransformPoint(label.rectTransform.rect.center);
            rect.rotation = label.rectTransform.rotation;
        }
        private void Cancel()
        {
            holding = false;
            elapsed = 0;
            if (fill != null) fill.anchorMax = new Vector2(0, 1);
            if (progressBar != null) progressBar.SetActive(false);
            Completed = false;
            if (label != null)
                label.text = original + " [HOLD]";
        }
        private void OnDisable()
        {
            Cancel();
        }
    }
}
