using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Shared modal for calibration, build details, notices, and confirmations.
    [DefaultExecutionOrder(-200)]
    public class TerminalModal : MonoBehaviour
    {
        public static TerminalModal Current
        {
            get; private set;
        }
        public static int ClosedFrame { get; private set; } = -1;
        public static bool BlocksInput => Current != null || Time.frameCount <= ClosedFrame;
        public RectTransform Body
        {
            get; private set;
        }
        public TMP_Text Message
        {
            get; private set;
        }
        public Action closed;
        public Action tick;
        public bool AllowClose { get; set; } = true;
        public bool ClearDisplay { get; set; }
        private GameObject selection;
        private CursorLockMode oldLock;
        private bool oldVisible;
        private readonly List<GraphicRaycaster> blockedRaycasters = new List<GraphicRaycaster>();

        public static TerminalModal Open(string title, string message)
        {
            if (Current != null)
                Current.Close();
            var root = new GameObject("Terminal modal", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 800;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var modal = root.AddComponent<TerminalModal>();
            Current = modal;
            // The telescope and popup use different cameras. Only the popup should receive clicks.
            foreach (var raycaster in FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None))
                if (raycaster.gameObject != root && raycaster.enabled)
                {
                    modal.blockedRaycasters.Add(raycaster);
                    raycaster.enabled = false;
                }
            modal.selection = EventSystem.current?.currentSelectedGameObject;
            modal.oldLock = Cursor.lockState;
            modal.oldVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            TerminalUI.Panel("Input blocker", root.transform, 0, 0, 1, 1, Color.clear);
            var border = TerminalUI.Panel("Window border", root.transform, .12f, .12f, .76f, .76f, TerminalUI.Ink);
            var panel = TerminalUI.Panel("Window", border.transform, .003f, .005f, .994f, .99f, TerminalUI.Blue);
            TerminalUI.Text("Title", panel.transform, title, .04f, .02f, .84f, .09f, 30);
            var closeButton = TerminalUI.Button(panel.transform, "X", .9f, .025f, .07f, .08f, modal.Close);
            modal.Body = TerminalUI.Rect("Body", panel.transform, .04f, .14f, .92f, .72f);
            modal.Message = TerminalUI.Text("Message", modal.Body, message, 0, 0, 1, 1, 25);
            modal.Message.alignment = TextAlignmentOptions.TopLeft;
            TerminalUI.Text("Close hint", panel.transform, "PRESS ESC TO CLOSE", .04f, .9f, .6f, .06f, 22);
            root.AddComponent<TerminalPauseDisplay>().Configure(TerminalUIAssets.Current.crtMaterial, TerminalUIAssets.Current.crtSettings, 27, 850);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            return modal;
        }

        public void Close()
        {
            if (Current != this || !AllowClose)
                return;
            Current = null;
            ClosedFrame = Time.frameCount;
            tick = null;
            RestoreRaycasters();
            gameObject.SetActive(false);
            closed?.Invoke();
            Cursor.lockState = oldLock;
            Cursor.visible = oldVisible;
            if (selection != null && selection.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(selection);
            Destroy(gameObject);
        }

        private void Update()
        {
            tick?.Invoke();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                Gamepad.current?.buttonEast.wasPressedThisFrame == true)
                Close();
        }

        private void OnDestroy()
        {
            RestoreRaycasters();
            if (Current == this)
            {
                Current = null;
                closed?.Invoke();
            }
        }

        private void RestoreRaycasters()
        {
            foreach (var raycaster in blockedRaycasters)
                if (raycaster != null) raycaster.enabled = true;
            blockedRaycasters.Clear();
        }
    }
}
