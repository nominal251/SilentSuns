using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SilentSuns.Menu
{
    // The tool borrows the pointer while choosing a tracker. It does not pause the world.
    public static class TerminalToolInput
    {
        private static UnityEngine.Object owner;
        private static Action close;
        private static CursorLockMode previousLock;
        private static bool previousVisible;
        private static int releaseFrame = -1;
        public static bool BlocksInput => owner != null || Time.frameCount <= releaseFrame;

        public static void Begin(UnityEngine.Object tool, Action closeTool)
        {
            CloseActive();
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            owner = tool;
            close = closeTool;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void End(UnityEngine.Object tool)
        {
            if (owner != tool) return;
            owner = null;
            close = null;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            // The selection click must not also interact with something in the world.
            releaseFrame = Time.frameCount;
        }

        // Pause and game over close the tool before taking over the cursor themselves.
        public static void CloseActive() => close?.Invoke();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            owner = null;
            close = null;
            releaseFrame = -1;
        }
    }
}
