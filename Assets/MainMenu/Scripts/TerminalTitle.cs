using TMPro;
using UnityEngine;

namespace SilentSuns.Menu
{
    [RequireComponent(typeof(TMP_Text))]
    public class TerminalTitle : MonoBehaviour
    {
        [Header("Typing")]
        [SerializeField] private RectTransform cursor;
        [SerializeField] private TerminalTypingAudio typingAudio;
        [SerializeField, Min(0f)] private float startDelay = 0.35f;
        [SerializeField, Min(0.01f)] private float characterDelay = 0.09f;
        [SerializeField, Min(0f)] private float wordPause = 0.22f;
        [SerializeField, Min(0.2f)] private float blinkInterval = 0.55f;

        private TMP_Text title;
        private float[] cursorPositions;
        private float cursorTop;
        private float nextCharacter;
        private float nextBlink;
        private int visibleCharacters;
        private bool hasTyped;
        private bool reducedMotion;

        // Measure the complete line once so hidden letters do not move the cursor's baseline.
        private void Awake()
        {
            title = GetComponent<TMP_Text>();
            title.maxVisibleCharacters = int.MaxValue;
            title.ForceMeshUpdate();
            int count = title.textInfo.characterCount;
            cursorPositions = new float[count + 1];
            TMP_CharacterInfo first = title.textInfo.characterInfo[0];
            cursorPositions[0] = first.origin;
            cursorTop = first.topRight.y;
            cursor.sizeDelta = new Vector2(title.fontSize * 0.28f, first.topRight.y - first.bottomLeft.y);
            for (int i = 0; i < count; i++)
                cursorPositions[i + 1] = title.textInfo.characterInfo[i].xAdvance;
        }

        public void ResetForBoot()
        {
            hasTyped = false;
        }
        public void ShowComplete()
        {
            hasTyped = true;
            // An inactive menu label has not run Awake yet. OnEnable will reveal it.
            if (cursorPositions == null) return;
            visibleCharacters = cursorPositions.Length - 1;
            UpdateCursor();
        }

        // Returning from another panel keeps the title complete instead of replaying the intro.
        private void OnEnable()
        {
            visibleCharacters = hasTyped || reducedMotion ? cursorPositions.Length - 1 : 0;
            nextCharacter = Time.unscaledTime + startDelay;
            nextBlink = Time.unscaledTime + blinkInterval;
            cursor.gameObject.SetActive(true);
            UpdateCursor();
        }

        // Leaving early also finishes the intro, without delaying menu navigation.
        private void OnDisable()
        {
            hasTyped = true;
        }

        // Keep the block solid while typing, then blink at a slow terminal cadence.
        private void Update()
        {
            int count = cursorPositions.Length - 1;
            if (visibleCharacters < count)
            {
                while (visibleCharacters < count && Time.unscaledTime >= nextCharacter)
                {
                    visibleCharacters++;
                    typingAudio.Type();
                    nextCharacter += characterDelay;
                    if (title.text[visibleCharacters - 1] == '.')
                        nextCharacter += wordPause;
                    nextBlink = Time.unscaledTime + blinkInterval;
                    UpdateCursor();
                }
                if (visibleCharacters == count)
                    hasTyped = true;
            }
            else if (!reducedMotion && Time.unscaledTime >= nextBlink)
            {
                cursor.gameObject.SetActive(!cursor.gameObject.activeSelf);
                nextBlink = Time.unscaledTime + blinkInterval;
            }
        }

        // Reduced Motion reveals the whole title and leaves a steady block cursor.
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (!isActiveAndEnabled)
                return;
            if (value)
            {
                visibleCharacters = cursorPositions.Length - 1;
                hasTyped = true;
                cursor.gameObject.SetActive(true);
                UpdateCursor();
            }
            nextBlink = Time.unscaledTime + blinkInterval;
        }

        // A UI rectangle keeps the block crisp without relying on a font fallback glyph.
        private void UpdateCursor()
        {
            title.maxVisibleCharacters = visibleCharacters;
            cursor.anchoredPosition = new Vector2(cursorPositions[visibleCharacters] + title.fontSize * 0.08f, cursorTop);
        }
    }
}
