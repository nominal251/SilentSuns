using System.Text;
using TMPro;
using UnityEngine;

namespace SilentSuns.Menu
{
    // The POST clock is independent of audio playback, mixer volume and time scale.
    public class TerminalBootLog : MonoBehaviour
    {
        [SerializeField] private TMP_Text output;
        [SerializeField] private TerminalTypingAudio typing;
        [SerializeField] private TerminalDisplay display;
        public const float Duration = 28f;
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public float Progress => Mathf.Clamp01(Elapsed / Duration);
        private int previousCharacters;
        private readonly StringBuilder buffer = new StringBuilder(1600);

        private readonly struct Entry
        {
            public readonly float time;
            public readonly string text;
            public readonly bool command;
            public readonly bool anomaly;
            public Entry(float time, string text, bool command = false, bool anomaly = false)
            {
                this.time = time;
                this.text = text;
                this.command = command;
                this.anomaly = anomaly;
            }
        }

        // Time is seconds since the log started. Commands type out; anomalies briefly glitch.
        private static readonly Entry[] Entries =
        {
            new Entry(0f, @"A:\SYS> BOOTSTRAP /LOCAL", true),
            new Entry(1.2f, "[00.016] ARCA/OS 4.2              KERNEL CHECKSUM  7F42 : OK"),
            new Entry(1.8f, "[00.032] MEMORY BANKS            640K BASE / 32768K EXT : OK"),
            new Entry(2.3f, "[00.048] MOUNT SYS:               READ ONLY / JOURNAL CLEAN"),
            new Entry(2.8f, "[00.064] MOUNT USR:               OPERATOR STATE / LOCAL"),
            new Entry(3.5f, @"A:\SYS> SERVICE /START SHIP", true),
            new Entry(5f, "[01.104] PWR.BUS                 AUXILIARY -> INTERNAL"),
            new Entry(5.6f, "[01.208] ATM.REG                 PRESSURE HOLD / 101.3 KPA"),
            new Entry(6.1f, "[01.312] AIR.CYCLE               FILTER 02 / FLOW NOMINAL"),
            new Entry(6.6f, "[01.416] THERMAL                 COMPARTMENT LOOP / 294.1 K"),
            new Entry(7.2f, "[01.520] DOOR.CTRL               C07 SEALED / INTERLOCK SET"),
            new Entry(7.45f, "[01.521] C07 SENSOR              MOTION: 04 RETURNS / MANIFEST: 0", anomaly: true),
            new Entry(8.9f, "[01.522] C07 INTERLOCK           RELEASE REQUEST / ORIGIN INSIDE", anomaly: true),
            new Entry(10.35f, "[01.521] C07 SENSOR              DOOR OPEN / LATCH SENSOR: CLOSED"),
            new Entry(10.55f, @"A:\SYS> NET /POLL UPLINK", true),
            new Entry(11.7f, "[02.016] COMMS.0                 NO CARRIER / UPLINK DISCONNECTED"),
            new Entry(12.2f, "[02.048] COMMS.0                 RX: 128 BYTES / ANTENNA DISCONNECTED", anomaly: true),
            new Entry(13.8f, "[02.049] REMOTE SOURCE           FRAME TIME: +00:08 / CLOCK SYNC: OK", anomaly: true),
            new Entry(15f, @"A:\SYS> SESSION /ENUM LOCAL", true),
            new Entry(16.1f, "[03.008] CABIN THERMAL           BODY COUNT: 01 / MANIFEST: 01"),
            new Entry(16.65f, "[03.008] CABIN THERMAL           BODY COUNT: 02 / MANIFEST: 01", anomaly: true),
            new Entry(17.8f, "[03.009] CABIN THERMAL           SECOND RETURN / 310.2 K", anomaly: true),
            new Entry(19.2f, "[03.010] CABIN RANGE             RETURN 02: 0.4 M / REAR SENSOR", anomaly: true),
            new Entry(20.85f, "[03.024] CABIN THERMAL           BODY COUNT: 01 / MANIFEST: 01"),
            new Entry(21.1f, "[03.040] SENSOR SELF TEST        THERMAL ARRAY / PASS"),
            new Entry(21.65f, "[03.041] C07 INTERLOCK           INNER HANDLE ACTIVE / MANIFEST: 0", anomaly: true),
            new Entry(23.35f, "[03.042] C07 PRESSURE            0.0 KPA / MOTION: 06 RETURNS", anomaly: true),
            new Entry(24.7f, @"A:\SYS> LOAD SILENT.SUNS", true),
            new Entry(26f, "[04.016] SURVEY.EXE              LOCAL INTERFACE / READY"),
            new Entry(26.4f, "[04.031] EVENT BUFFER            06 FLAGS / UPLOAD QUEUED"),
            new Entry(27f, "[04.032] INPUT                   OPERATOR CONTROL ENABLED"),
            new Entry(27.4f, @"A:\SYS> _")
        };

        public void Begin()
        {
            ResetLog();
            Running = true;
            Render();
        }

        public void ResetLog()
        {
            Stop();
            Elapsed = 0f;
            previousCharacters = 0;
            output.text = "";
        }

        public void Stop()
        {
            Running = false;
            if (display != null) display.DiagnosticGlitch = 0f;
        }

        private void Update()
        {
            if (!Running) return;
            Elapsed = Mathf.Min(Duration, Elapsed + Time.unscaledDeltaTime);
            Render();
            if (Elapsed >= Duration) Stop();
        }

        private void OnDisable() { Stop(); }

        private void Render()
        {
            int current = 0;
            while (current + 1 < Entries.Length && Entries[current + 1].time <= Elapsed) current++;
            buffer.Clear();
            int totalCharacters = 0;
            for (int i = Mathf.Max(0, current - 13); i <= current; i++)
            {
                Entry entry = Entries[i];
                int count = entry.text.Length;
                if (i == current && entry.command && !GamePreferences.ReducedMotion)
                    count = Mathf.Min(count, Mathf.FloorToInt((Elapsed - entry.time) * 32f));
                bool anomaly = entry.anomaly && Elapsed - entry.time < 4f;
                if (anomaly) buffer.Append("<color=#FFD3A0>");
                buffer.Append(entry.text, 0, count);
                if (anomaly) buffer.Append("</color>");
                totalCharacters += count;
                if (i < current) buffer.Append('\n');
                else if (entry.command && count < entry.text.Length) buffer.Append('_');
            }
            output.text = buffer.ToString();
            if (Entries[current].command && totalCharacters > previousCharacters && !GamePreferences.ReducedMotion)
                typing?.Type();
            previousCharacters = totalCharacters;
            // Tell TerminalDisplay to glitch briefly, then leave the warning readable.
            if (display != null)
                display.DiagnosticGlitch = !GamePreferences.ReducedMotion && Entries[current].anomaly && Elapsed - Entries[current].time < .22f ? .8f : 0f;
        }
    }
}
