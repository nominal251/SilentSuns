using UnityEngine;

namespace SilentSuns.Menu
{
    public static class GameProgress
    {
        public static bool HasCompletedGame => PlayerPrefs.GetInt("SilentSuns.Progress.Completed", 0) == 1;

        // This flag survives new surveys and restarts. It does not save a survey in progress.
        public static void MarkCompleted()
        {
            PlayerPrefs.SetInt("SilentSuns.Progress.Completed", 1);
            PlayerPrefs.Save();
        }
    }
}
