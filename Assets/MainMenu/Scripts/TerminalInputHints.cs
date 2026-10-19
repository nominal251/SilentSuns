using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.UI;

namespace SilentSuns.Menu
{
    public class TerminalInputHints : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputSystemUIInputModule inputModule;

        [Header("Prompts")]
        [SerializeField] private TMP_Text navigationHint;
        [SerializeField] private TMP_Text skipLabel;
        [SerializeField] private TMP_Text restoreSkipLabel;
        [SerializeField] private TMP_Text[] backLabels;
        [SerializeField] private TMP_Text guideNote;

        private int currentScheme = -1;

        // A connected controller does not change prompts until it is used.
        private void OnEnable()
        {
            InputSystem.onActionChange += OnActionChange;
            SetScheme(0);
        }

        // Stop listening when the terminal scene is unloaded.
        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
        }

        // Mouse movement and keys outside the navigation bindings also count as desktop input.
        private void Update()
        {
            bool keyUsed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mouseUsed = Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude >= 4f ||
                                                      Mouse.current.scroll.ReadValue().sqrMagnitude > 0f);
            if (keyUsed || mouseUsed)
                SetScheme(0);
        }

        // Use the UI's actual actions so disconnected devices and idle stick noise cannot take over.
        private void OnActionChange(object changed, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || !(changed is InputAction action))
                return;
            if (inputModule == null || action.actionMap == null || action.actionMap.asset != inputModule.actionsAsset)
                return;

            InputDevice device = action.activeControl?.device;
            if (device is Gamepad gamepad)
            {
                if (action == inputModule.move.action && action.ReadValue<Vector2>().sqrMagnitude < 0.0625f)
                    return;
                SetScheme(gamepad is DualShockGamepad ? 2 : 1);
            }
            else if (device is Keyboard)
                SetScheme(0);
            else if (device is Mouse mouse)
            {
                if (action == inputModule.point.action && mouse.delta.ReadValue().sqrMagnitude < 4f)
                    return;
                SetScheme(0);
            }
        }

        // Keep button names consistent across the power screen, menu and subpanels.
        private void SetScheme(int scheme)
        {
            if (scheme == currentScheme || navigationHint == null)
                return;
            currentScheme = scheme;
            bool controller = scheme != 0;
            string confirm = scheme == 2 ? "CROSS" : "A";
            string back = scheme == 2 ? "CIRCLE" : "B";
            navigationHint.text = controller
                ? "D-PAD  SELECT     " + confirm + "  CONFIRM     " + back + "  BACK"
                : "ARROWS  SELECT     ENTER  CONFIRM     ESC  BACK";
            skipLabel.text = controller ? "SKIP DIAGNOSTICS  [" + confirm + "]" : "SKIP DIAGNOSTICS  [SPACE]";
            if (restoreSkipLabel != null)
                restoreSkipLabel.text = controller ? "CONTINUE  [" + confirm + "]" : "CONTINUE  [SPACE]";
            foreach (TMP_Text label in backLabels)
                label.text = controller ? "BACK  [" + back + "]" : "BACK  [ESC]";
            guideNote.text = controller
                ? "Survey controls use keyboard and mouse in this prototype."
                : "Inspect the ship's instruments to begin your survey.";
        }
    }
}
