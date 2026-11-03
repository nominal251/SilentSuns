using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    [DefaultExecutionOrder(-250)]
    public class TerminalScreensaver : MonoBehaviour
    {
        private TerminalMenu menu;
        private CanvasGroup menuPanel;
        private GameObject cover;
        private RectTransform shuttle;
        private TerminalPipes pipes;
        private Vector2 velocity;
        private float lastInput;
        private int wakeFrame = -1;
        private GameObject previousSelection;
        public bool Showing => cover != null && cover.activeSelf;
        public bool BlocksActivation => Showing || Time.frameCount <= wakeFrame;
        public const float IdleSeconds = 60;
        public void Initialize(TerminalMenu owner, CanvasGroup panel)
        {
            menu = owner;
            menuPanel = panel;
            lastInput = Time.unscaledTime;
            var image = TerminalUI.Panel("Idle screensaver", transform, 0, 0, 1, 1, Color.black);
            cover = image.gameObject;
            pipes = TerminalUI.Rect("Pipework", cover.transform, 0, 0, 1, 1).gameObject.AddComponent<TerminalPipes>();
            var ship = TerminalUI.Panel("Space shuttle", cover.transform, .45f, .45f, .1f, .1f, TerminalUI.Ink);
            ship.sprite = TerminalUIAssets.Current.shuttle;
            ship.preserveAspect = true;
            shuttle = ship.rectTransform;
            shuttle.anchorMin = shuttle.anchorMax = new Vector2(.5f, .5f);
            shuttle.sizeDelta = new Vector2(125, 112);
            cover.SetActive(false);
        }
        public static bool InputUsed()
        {
            if (Keyboard.current?.anyKey.wasPressedThisFrame == true)
                return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 1 || mouse.scroll.ReadValue().sqrMagnitude > 0 || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            if (pad == null)
                return false;
            if (pad.leftStick.ReadValue().sqrMagnitude > .15f || pad.rightStick.ReadValue().sqrMagnitude > .15f || pad.dpad.ReadValue().sqrMagnitude > .1f)
                return true;
            foreach (var control in pad.allControls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                    return true;
            return false;
        }
        private void Update()
        {
            if (menu == null || cover == null)
                return;
            if (!menu.IdleAllowed || TerminalModal.Current != null)
            {
                lastInput = Time.unscaledTime;
                if (Showing)
                    Wake();
                return;
            }
            if (InputUsed())
            {
                lastInput = Time.unscaledTime;
                if (Showing)
                    Wake();
                return;
            }
            if (!Showing && Time.unscaledTime - lastInput >= IdleSeconds)
                StartSaver();
            if (!Showing || !shuttle.gameObject.activeSelf)
                return;
            Vector2 position = shuttle.anchoredPosition;
            float speed = GamePreferences.ReducedMotion ? 35 : 90;
            position += velocity * (Time.unscaledDeltaTime * speed);
            Vector2 bounds = ((RectTransform)cover.transform).rect.size * .5f - Vector2.one * 100;
            if (Mathf.Abs(position.x) > bounds.x)
            {
                position.x = Mathf.Clamp(position.x, -bounds.x, bounds.x);
                velocity.x = -velocity.x;
            }
            if (Mathf.Abs(position.y) > bounds.y)
            {
                position.y = Mathf.Clamp(position.y, -bounds.y, bounds.y);
                velocity.y = -velocity.y;
            }
            shuttle.anchoredPosition = position;
            shuttle.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }
        public void StartSaver()
        {
            if (!menu.IdleAllowed || TerminalModal.Current != null)
                return;
            previousSelection = EventSystem.current?.currentSelectedGameObject;
            EventSystem.current?.SetSelectedGameObject(null);
            menuPanel.interactable = false;
            cover.SetActive(true);
            bool usePipes = Random.value < .5f;
            pipes.enabled = usePipes;
            pipes.ResetPipes();
            shuttle.gameObject.SetActive(!usePipes);
            shuttle.anchoredPosition = Vector2.zero;
            velocity = new Vector2(Random.value < .5f ? -1 : 1, Random.Range(.5f, 1.2f) * (Random.value < .5f ? -1 : 1)).normalized;
        }
        private void Wake()
        {
            cover.SetActive(false);
            wakeFrame = Time.frameCount;
            menu.WakeFromIdle(previousSelection);
        }
    }
}
