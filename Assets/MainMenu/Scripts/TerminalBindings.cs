using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SilentSuns.Menu
{
    // InputHandler owns the live controls. Settings changes this same copy.
    public static class TerminalBindings
    {
        public static InputActionAsset Controls
        {
            get; private set;
        }
        public static bool Listening => operation != null;
        public static int ConsumedFrame { get; private set; } = -1;
        private static InputActionRebindingExtensions.RebindingOperation operation;
        private const string Key = "SilentSuns.Input.Bindings";
        private static bool editing;
        private static string beforeEdit;
        public static bool HasChanges => editing && Controls != null && beforeEdit != Controls.SaveBindingOverridesAsJson();

        public static void BeginEdit()
        {
            if (editing) return;
            editing = true;
            beforeEdit = Controls?.SaveBindingOverridesAsJson();
        }
        public static bool ApplyEdits()
        {
            if (Listening || ConflictWarning().Length > 0) return false;
            if (Controls != null && HasChanges)
            {
                beforeEdit = Controls.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString(Key, beforeEdit);
                PlayerPrefs.Save();
            }
            return true;
        }
        public static void DiscardEdits()
        {
            Cancel();
            if (editing && Controls != null && beforeEdit != null)
                Controls.LoadBindingOverridesFromJson(beforeEdit);
        }
        public static void EndEdit() { DiscardEdits(); editing = false; beforeEdit = null; }

        public static string ConflictWarning()
        {
            var conflicts = new List<string>();
            var bindings = new List<(InputAction action, int index)>(Editable());
            for (int i = 0; i < bindings.Count; i++)
            {
                var first = bindings[i];
                string path = Normalize(first.action.bindings[first.index].effectivePath);
                if (path.Length == 0) continue;
                string reserved = ReservedAction(path);
                if (reserved.Length > 0)
                    conflicts.Add(Label(first.action, first.index) + " + " + reserved);
                for (int j = i + 1; j < bindings.Count; j++)
                {
                    var second = bindings[j];
                    if (path == Normalize(second.action.bindings[second.index].effectivePath))
                        conflicts.Add(Label(first.action, first.index) + " + " + Label(second.action, second.index));
                }
            }
            return conflicts.Count == 0 ? "" : "You have conflicting keybinds:\n\n" + string.Join("\n\n", conflicts);
        }

        public static InputActionAsset Initialize(InputActionAsset original)
        {
            if (Controls != null)
                return Controls;
            Controls = UnityEngine.Object.Instantiate(original);
            string saved = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    Controls.LoadBindingOverridesFromJson(saved);
                }
                catch (Exception) { Debug.LogWarning("Saved controls could not be read. Using defaults."); }
            }
            if (editing) beforeEdit = Controls.SaveBindingOverridesAsJson();
            return Controls;
        }

        public static IEnumerable<(InputAction action, int index)> Editable()
        {
            if (Controls == null)
                yield break;
            foreach (var action in Controls.FindActionMap("Player").actions)
            {
                if (action.name == "Debug" || action.name == "Look" || action.name == "ControllerLook")
                    continue;
                for (int i = 0; i < action.bindings.Count; i++)
                    if (!action.bindings[i].isComposite)
                        yield return (action, i);
            }
        }

        public static string Label(InputAction action, int index)
        {
            if (action.name == "Physics Interact") return "PICK UP / DROP";
            if (action.name == "Physics Rotate") return "ROTATE HELD OBJECT";
            if (action.name == "Physics Extend")
                return action.bindings[index].name == "positive" ? "MOVE OBJECT FARTHER" : "MOVE OBJECT CLOSER";
            string part = action.bindings[index].isPartOfComposite ? " " + action.bindings[index].name : "";
            string context = MovementContext(action, index);
            return (action.name + part).ToUpperInvariant() + (context.Length > 0 ? " (" + context + ")" : "");
        }

        public static string Description(InputAction action)
        {
            switch (action.name)
            {
                case "Physics Interact": return "Pick up the object you are looking at. Press again to drop it.";
                case "Physics Rotate": return "Hold and move the mouse or right stick to turn the held object. The camera stays still.";
                case "Physics Extend": return "Move the held object closer or farther. Scroll for small steps, or hold a key or controller button.";
                default: return "This binding applies both on foot and in ship. ESC cancels rebinding.";
            }
        }

        public static string MovementContext(InputAction action, int index)
        {
            // PlayerMovement ignores vertical movement on foot and sprint in zero gravity.
            if (action.name == "Sprint") return "ON FOOT";
            if (action.name == "Roll" || action.name == "Reorient") return "IN SHIP";
            if (action.name == "Move")
                return action.bindings[index].name == "up" || action.bindings[index].name == "down" ?
                    "IN SHIP" : "ON FOOT + IN SHIP";
            return "";
        }

        private static string ReservedAction(string path)
        {
            switch (Normalize(path))
            {
                case "<keyboard>/escape": case "<gamepad>/start": return "PAUSE MENU";
                case "<gamepad>/buttoneast": return "MENU BACK";
                case "<keyboard>/f12": return "SCREENSHOT";
                case "<keyboard>/f1": return "DEBUG GRAVITY TOGGLE";
                default: return "";
            }
        }

        public static bool IsController(InputAction action, int index)
        {
            // Use the original device, even when the player has remapped its control.
            return action.bindings[index].path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase);
        }

        public static bool Conflicts(InputAction action, int index, string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            path = Normalize(path);
            if (ReservedAction(path).Length > 0) return true;
            foreach (var pair in Editable())
                if ((pair.action != action || pair.index != index) &&
                    string.Equals(Normalize(pair.action.bindings[pair.index].effectivePath), path, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string Normalize(string path)
        {
            // Generic Shift/Ctrl bindings also cover the left and right versions.
            return (path ?? "").ToLowerInvariant().Replace("/leftshift", "/shift").Replace("/rightshift", "/shift")
                .Replace("/leftctrl", "/ctrl").Replace("/rightctrl", "/ctrl").Replace("/leftalt", "/alt").Replace("/rightalt", "/alt");
        }

        public static void Rebind(InputAction action, int index, Action<string> finished)
        {
            Cancel();
            bool pad = IsController(action, index);
            Controls.Disable();
            operation = action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Pointer>/position").WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding(pad ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding(pad ? "<Mouse>" : "<Touchscreen>")
                .WithCancelingThrough("<Keyboard>/escape").WithTimeout(10)
                .OnCancel(op => Finish("BINDING UNCHANGED", finished))
                .OnComplete(op =>
                {
                    // Keep duplicate choices visible so the player can fix either assignment.
                    Finish(ConflictWarning().Length > 0 ? "CONFLICT / CHANGE EITHER ASSIGNMENT BEFORE APPLYING" :
                        "BINDING CHANGED / APPLY TO SAVE", finished);
                });
            operation.Start();
        }

        private static void Finish(string message, Action<string> finished)
        {
            var old = operation;
            operation = null;
            old?.Dispose();
            Controls?.Enable();
            ConsumedFrame = Time.frameCount;
            finished?.Invoke(message);
        }

        public static void Cancel()
        {
            operation?.Cancel();
        }
        public static void Reset()
        {
            Cancel();
            Controls?.RemoveAllBindingOverrides();
            if (!editing) { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
        }

        public static void ResetDevice(bool controller)
        {
            Cancel();
            if (Controls == null) return;
            foreach (var pair in Editable())
                if (IsController(pair.action, pair.index) == controller)
                    pair.action.RemoveBindingOverride(pair.index);
            if (!editing)
            {
                PlayerPrefs.SetString(Key, Controls.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            Controls = null;
            operation = null;
            ConsumedFrame = -1;
            editing = false;
            beforeEdit = null;
        }
    }
}
