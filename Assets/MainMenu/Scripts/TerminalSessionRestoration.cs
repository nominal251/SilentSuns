using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalSessionRestoration : MonoBehaviour
    {
        [Header("Save integration")]
        [Tooltip("Leave empty until the gameplay save system is connected.")]
        [SerializeField] private SessionSaveProvider saveProvider;
        [SerializeField] private Button resumeButton;
        [SerializeField] private TMP_Text availability;
        [Header("Presentation")]
        [SerializeField] private TerminalMenu menu;
        [SerializeField] private TerminalDisplay display;
        [SerializeField] private CanvasGroup screen;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text savedInfo;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text diagnostics;
        private bool skip;
        private int startFrame;
        public bool IsActive { get; private set; }

        private void Start()
        {
            screen.gameObject.SetActive(false);
            screen.blocksRaycasts = false;
            screen.interactable = false;
            RefreshAvailability();
            if (availability != null && !string.IsNullOrEmpty(TerminalTransition.RestoreError))
            {
                availability.text = TerminalTransition.RestoreError;
                TerminalTransition.ClearRestoreError();
            }
        }

        public void RefreshAvailability()
        {
            bool valid = TryGetSession(out SavedSession session);
            if (resumeButton != null) resumeButton.interactable = valid;
            if (availability != null) availability.text = valid ? "SESSION AVAILABLE" : "RESUME UNAVAILABLE";
        }

        private bool TryGetSession(out SavedSession session)
        {
            session = null;
            if (saveProvider == null)
                return false;
            try
            {
                return saveProvider.TryGetLatest(out session) && session != null && !string.IsNullOrEmpty(session.id);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Begin()
        {
            if (IsActive || TerminalTransition.IsActive || !menu.CanRestore)
                return;
            if (!TryGetSession(out SavedSession session))
            {
                RefreshAvailability();
                return;
            }
            IsActive = true;
            skip = false;
            startFrame = Time.frameCount;
            menu.ShowRestoration(screen, skipButton.gameObject);
            savedInfo.text = session.description;
            status.text = "SAVED SESSION FOUND";
            diagnostics.text = "";
            StartCoroutine(Present(session));
        }

        private void Update()
        {
            if (!IsActive || TerminalTransition.IsActive || Time.frameCount <= startFrame)
                return;
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            if ((keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) ||
                (gamepad != null && gamepad.buttonEast.wasPressedThisFrame))
                Skip();
        }

        public void Skip() { skip = true; }

        private IEnumerator Present(SavedSession session)
        {
            diagnostics.text = "SAVE PROVIDER / SESSION HANDLE VALIDATED\nRESTORE REQUEST QUEUED";
            for (float elapsed = 0f; elapsed < .6f && !skip; elapsed += Time.unscaledDeltaTime)
                yield return null;
            status.text = "RESTORING SAVED GAME...";
            skipButton.interactable = false;
            // Only the provider can restore gameplay. This path never starts a new game.
            TerminalTransition.RestoreSession(saveProvider, session, OnRestored);
        }

        private void OnRestored(SessionRestoreResult result)
        {
            if (this == null)
                return;
            IsActive = false;
            skipButton.interactable = true;
            if (!result.succeeded)
            {
                menu.BackToMenu();
                RefreshAvailability();
                if (availability != null) availability.text = "RESTORE FAILED / SAVE PRESERVED";
            }
        }

    }
}
