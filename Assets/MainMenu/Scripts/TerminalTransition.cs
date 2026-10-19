using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    public class TerminalTransition : MonoBehaviour
    {
        private static TerminalTransition instance;
        private CanvasGroup cover;
        public static bool IsActive => instance != null;
        public static string RestoreError { get; private set; }

        public static void LoadScene(string scene, Action beforeLoad = null)
        {
            if (IsActive || !Application.CanStreamedLevelBeLoaded(scene))
                return;
            CreateCover();
            instance.StartCoroutine(instance.Handoff(scene, beforeLoad));
        }

        public static void RestoreSession(SessionSaveProvider provider, SavedSession session, Action<SessionRestoreResult> complete)
        {
            if (IsActive)
                return;
            CreateCover();
            instance.StartCoroutine(instance.Restore(provider, session, complete));
        }

        public static void ClearRestoreError() { RestoreError = null; }

        private static void CreateCover()
        {
            GameObject root = new GameObject("Session Transition", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(TerminalTransition));
            instance = root.GetComponent<TerminalTransition>();
            DontDestroyOnLoad(root);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            instance.cover = root.GetComponent<CanvasGroup>();
            GameObject face = new GameObject("Black", typeof(RectTransform), typeof(Image));
            face.transform.SetParent(root.transform, false);
            RectTransform rect = face.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            face.GetComponent<Image>().color = Color.black;
        }

        // The backend runs under an opaque cover, including nested scene-loading operations.
        private IEnumerator Restore(SessionSaveProvider provider, SavedSession session, Action<SessionRestoreResult> complete)
        {
            yield return Fade(0f, 1f, 0.55f);
            var result = new SessionRestoreResult();
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            IEnumerator operation = null;
            try { operation = provider.Restore(session, result); }
            catch (Exception) { result.succeeded = false; }
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            if (operation != null)
                stack.Push(operation);
            while (stack.Count > 0)
            {
                object step = null;
                bool moved = false;
                try { moved = stack.Peek().MoveNext(); if (moved) step = stack.Peek().Current; }
                catch (Exception) { result.succeeded = false; stack.Clear(); break; }
                if (!moved) { stack.Pop(); continue; }
                if (step is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return step;
            }
            if (!result.succeeded)
            {
                RestoreError = "RESTORE FAILED / SAVE PRESERVED";
                if (SceneManager.GetActiveScene().name != "MainMenu")
                    yield return SceneManager.LoadSceneAsync("MainMenu");
            }
            complete?.Invoke(result);
            yield return null;
            yield return Fade(1f, 0f, 0.75f);
            Time.timeScale = previousScale;
            instance = null;
            Destroy(gameObject);
        }

        // The cover survives scene activation, hiding the first frame of the new camera.
        private IEnumerator Handoff(string scene, Action beforeLoad)
        {
            yield return Fade(0f, 1f, 0.55f);
            beforeLoad?.Invoke();
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return new WaitForSecondsRealtime(0.15f);
            yield return Fade(1f, 0f, 0.75f);
            Time.timeScale = previousScale;
            instance = null;
            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (GamePreferences.ReducedMotion)
                duration = 0.08f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                cover.alpha = Mathf.SmoothStep(from, to, elapsed / duration);
                yield return null;
            }
            cover.alpha = to;
        }
    }
}
