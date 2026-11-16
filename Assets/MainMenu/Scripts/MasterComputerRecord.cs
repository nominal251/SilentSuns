using UnityEngine;

namespace SilentSuns.Menu
{
    // One disk log or tool report. The reader/collection system passes this to the computer.
    [CreateAssetMenu(menuName = "Silent Suns/Master Computer Record")]
    public class MasterComputerRecord : ScriptableObject
    {
        public enum RecordType { Log, Tool }
        public RecordType type;
        public string id;
        public string title;
        public string author;
        public string recordedAt;
        public string foundOn;
        public string useType;
        [TextArea(5, 24)] public string body;
    }
}
