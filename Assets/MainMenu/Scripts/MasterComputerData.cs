using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilentSuns.Menu
{
    // Lives on the run manager, so changing systems does not lose the computer's records.
    // A new run clears them. This is run memory, not a save/load system.
    public class MasterComputerData : MonoBehaviour
    {
        public class Alert
        {
            public string id, title, body;
            public bool critical, read;
        }

        private readonly List<MasterComputerRecord> logs = new List<MasterComputerRecord>();
        private readonly List<MasterComputerRecord> tools = new List<MasterComputerRecord>();
        private readonly List<Alert> alerts = new List<Alert>();
        private GameManager game;
        private bool hullWarning;
        private bool intrusionShown;
        private float nextStartupRoll;
        public GameManager Game => game;
        public IReadOnlyList<MasterComputerRecord> Logs => logs;
        public IReadOnlyList<MasterComputerRecord> Tools => tools;
        public IReadOnlyList<Alert> Alerts => alerts;
        public int UnreadCount
        {
            get
            {
                int count = 0;
                foreach (var alert in alerts) if (!alert.read) count++;
                return count;
            }
        }
        public event Action Changed;

        // One in 1,000 eligible starts by default. Reopening quickly cannot reroll it.
        // Once it happens, it cannot happen again until a new run.
        public bool TryStartupIntrusion(float roll, float chance)
        {
            if (chance <= 0 || intrusionShown || Time.unscaledTime < nextStartupRoll) return false;
            nextStartupRoll = Time.unscaledTime + 300;
            if (roll >= Mathf.Clamp01(chance)) return false;
            intrusionShown = true;
            return true;
        }

        public static MasterComputerData For(GameManager manager)
        {
            if (manager == null) return null;
            var data = manager.GetComponent<MasterComputerData>();
            if (data == null) data = manager.gameObject.AddComponent<MasterComputerData>();
            data.Connect(manager);
            return data;
        }

        private void Connect(GameManager manager)
        {
            if (game == manager) return;
            Disconnect();
            game = manager;
            game.RunStarted += ResetRun;
            game.PhaseChanged += PhaseChanged;
            game.SystemSubmitted += CatalogSubmitted;
            game.Resources.ResourcesChanged += ResourcesChanged;
            game.Resources.HullChanged += HullChanged;
            HullChanged(game.Resources.HullPips, game.Resources.MaxHullPips);
            if (game.CatalogSubmitted) CatalogSubmitted(game.CurrentSystemNumber);
            else PhaseChanged(game.Phase);
        }

        // Call after the player places a disk in the reader, not when walking near the disk.
        public bool InsertDisk(MasterComputerRecord record) => AddRecord(record, MasterComputerRecord.RecordType.Log, logs);
        public bool RegisterTool(MasterComputerRecord record) => AddRecord(record, MasterComputerRecord.RecordType.Tool, tools);

        private bool AddRecord(MasterComputerRecord record, MasterComputerRecord.RecordType type, List<MasterComputerRecord> list)
        {
            if (record == null || record.type != type || string.IsNullOrWhiteSpace(record.id) || list.Exists(r => r.id == record.id)) return false;
            list.Add(record);
            Changed?.Invoke();
            return true;
        }

        // Other ship systems can call this when they have an actual event to report.
        public void RaiseAlert(string id, string title, string body, bool critical = false)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            var existing = alerts.Find(a => a.id == id);
            if (existing != null) return;
            alerts.Insert(0, new Alert { id = id, title = title, body = body, critical = critical });
            Changed?.Invoke();
        }

        public void ReadAlert(int index)
        {
            if (index < 0 || index >= alerts.Count || alerts[index].read) return;
            alerts[index].read = true;
            Changed?.Invoke();
        }

        private void ResourcesChanged() => Changed?.Invoke();
        private void HullChanged(int remaining, int maximum)
        {
            bool critical = remaining > 0 && remaining <= Mathf.Max(1, maximum / 3);
            if (critical && !hullWarning)
                RaiseAlert("hull-" + Time.frameCount, "SHIP INTEGRITY CRITICAL", "HULL INTEGRITY IS BELOW OPERATING LIMITS. REPAIR THE SHIP BEFORE CONTINUING THE SURVEY.", true);
            hullWarning = critical;
            Changed?.Invoke();
        }

        private void PhaseChanged(GameManager.GamePhase phase)
        {
            if (phase == GameManager.GamePhase.SystemComplete)
                RaiseAlert("survey-" + game.CurrentSystemNumber, "SURVEY COMPLETE", "ALL REQUIRED SCANS ARE COMPLETE. SUBMIT THE CATALOG FROM STATUS TO PREPARE FOR CRYOSLEEP.");
            Changed?.Invoke();
        }

        private void CatalogSubmitted(int system)
        {
            RaiseAlert("cryo-" + system, "CRYOCHAMBER AVAILABLE", "CATALOG ACCEPTED. USE THE CRYOCHAMBER TO CONTINUE TO THE NEXT SYSTEM. ONE NITROGEN CANISTER IS REQUIRED.");
        }

        private void ResetRun()
        {
            logs.Clear(); tools.Clear(); alerts.Clear(); hullWarning = intrusionShown = false; nextStartupRoll = 0;
            Changed?.Invoke();
        }

        private void Disconnect()
        {
            if (game == null) return;
            game.RunStarted -= ResetRun;
            game.PhaseChanged -= PhaseChanged;
            game.SystemSubmitted -= CatalogSubmitted;
            if (game.Resources != null)
            {
                game.Resources.ResourcesChanged -= ResourcesChanged;
                game.Resources.HullChanged -= HullChanged;
            }
        }
        private void OnDestroy() => Disconnect();
    }
}
