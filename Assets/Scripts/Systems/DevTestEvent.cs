using UnityEngine;
using UnityEngine.InputSystem;

public class DevTestEvent : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EventManager eventManager;
    [SerializeField] private Transform playerController;

    [Header("Testing")]
    [SerializeField] private string testTriggerId = "DevTest";
    private readonly GameEventCategory[] categories =
    {
        GameEventCategory.mainStory,
        GameEventCategory.spook,
        GameEventCategory.violentSpook,
        GameEventCategory.storyRandom,
        GameEventCategory.cryoSleep
    };

private void Update()
{
    if (eventManager == null || Keyboard.current == null)
        return;

    bool shiftHeld =
        Keyboard.current.leftShiftKey.isPressed ||
        Keyboard.current.rightShiftKey.isPressed;

    for (int i = 0; i < categories.Length; i++)
    {
        Key key = (Key)((int)Key.Digit1 + i);

        if (!Keyboard.current[key].wasPressedThisFrame)
            continue;

        GameEventCategory category = categories[i];

        if (shiftHeld)
        {
            Debug.Log($"[DevTestEvent] Forced test requested | Key: Shift+{i + 1} | Category: {category}");
            bool success = eventManager.TryDevTestCategory(category, testTriggerId, playerController);
            LogTestResult(category, true, success);
        }
        else
        {
            Debug.Log($"[DevTestEvent] Normal roll requested | Key: {i + 1} | Category: {category}");
            bool success = eventManager.TryTriggerCategory(category, testTriggerId, playerController);
            LogTestResult(category, false, success);
        }
    }
}

    //Reports the final result of an attempt
    private void LogTestResult(GameEventCategory category, bool forced, bool success)
    {
        string testType = forced ? "FORCED TEST" : "NORMAL ROLL";
        string result = success ? "SUCCEEDED" : "FAILED";

        if (success)
            Debug.Log($"[DevTestEvent] {testType} {result} | Category: {category}");
        else
            Debug.LogWarning($"[DevTestEvent] {testType} {result} | Category: {category} | Inspect the EventManager logs above for the cause");
    }

    public void ForceTestCategory(GameEventCategory category)
    {
        if (eventManager == null)
        {
            Debug.LogError("[DevTestEvent] Cannot test category | EventManager is not assigned");
            return;
        }

        bool success = eventManager.TryDevTestCategory(category, testTriggerId, playerController);
        LogTestResult(category, true, success);
    }
}
