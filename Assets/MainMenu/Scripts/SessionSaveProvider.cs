using System;
using System.Collections;
using UnityEngine;

namespace SilentSuns.Menu
{
    [Serializable]
    public class SavedSession
    {
        public string id;
        public string description;
    }

    public class SessionRestoreResult
    {
        public bool succeeded;
        public string error = "SESSION COULD NOT BE RESTORED.";
    }

    // The team's future save script should inherit from this class.
    // TryGetLatest checks for a save without changing it. Restore loads that save and its scene.
    public abstract class SessionSaveProvider : ScriptableObject
    {
        public abstract bool TryGetLatest(out SavedSession session);
        public abstract IEnumerator Restore(SavedSession session, SessionRestoreResult result);
    }
}
