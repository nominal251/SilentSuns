using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SilentSuns.Menu
{
    // Instruments borrow the pointer while in use. They do not pause the world.
    public static class TerminalToolInput
    {
        private static UnityEngine.Object owner;
        private static Action close;
        private static Action escape;
        private static CursorLockMode previousLock;
        private static bool previousVisible;
        private static int releaseFrame = -1;
        public static bool BlocksInput => owner != null || Time.frameCount <= releaseFrame;

        public static void Begin(UnityEngine.Object tool, Action closeTool, Action exitOnEscape = null)
        {
            CloseActive();
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            owner = tool;
            close = closeTool;
            escape = exitOnEscape;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void End(UnityEngine.Object tool)
        {
            if (owner != tool) return;
            owner = null;
            close = null;
            escape = null;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            // The selection click must not also interact with something in the world.
            releaseFrame = Time.frameCount;
        }

        // Pause and game over close the tool before taking over the cursor themselves.
        public static void CloseActive() => close?.Invoke();

        // A computer can use Escape for its exit animation before pause reads the key.
        public static bool TryExitOnEscape()
        {
            if (owner == null || escape == null) return false;
            escape();
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            owner = null;
            close = null;
            escape = null;
            releaseFrame = -1;
        }
    }
}
