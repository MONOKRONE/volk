using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Volk.Core
{
    public enum GameSituation
    {
        NeutralFar, NeutralMid, NeutralClose,
        AfterHit, AfterBlock, AfterMiss,
        SkillReceived1, SkillReceived2,
        LowHP, HighHP,
        RoundLeading, RoundBehind,
        WakeUp
    }

    public enum PlayerAction
    {
        Punch, Kick, HeavyPunch, Skill1, Skill2, Block, Parry, Retreat, Approach, Wait
    }

    [System.Serializable]
    public class ActionRecord
    {
        public PlayerAction action;
        public float timestamp;
    }

    [System.Serializable]
    public class SituationHistory
    {
        public string key;
        public List<ActionRecord> actions = new List<ActionRecord>();
    }

    [System.Serializable]
    public class BehaviorProfile
    {
        public List<SituationHistory> situations = new List<SituationHistory>();
    }

    public class PlayerBehaviorTracker : MonoBehaviour
    {
        public static PlayerBehaviorTracker Instance { get; private set; }

        private Dictionary<string, List<ActionRecord>> situationTable = new Dictionary<string, List<ActionRecord>>();
        private string currentMatchup = "unknown";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetMatchup(string playerChar, string enemyChar)
        {
            currentMatchup = $"{playerChar}_vs_{enemyChar}";
        }

        public void Record(GameSituation situation, PlayerAction action)
        {
            string key = $"{currentMatchup}_{situation}";
            if (!situationTable.ContainsKey(key))
                situationTable[key] = new List<ActionRecord>();

            situationTable[key].Add(new ActionRecord
            {
                action = action,
                timestamp = Time.time
            });
        }

        /// <summary>
        /// Get the most common action for a situation (for Ghost AI).
        /// </summary>
        public PlayerAction? GetMostLikelyAction(string matchup, GameSituation situation)
        {
            string key = $"{matchup}_{situation}";
            if (!situationTable.ContainsKey(key) || situationTable[key].Count == 0)
                return null;

            var counts = new Dictionary<PlayerAction, int>();
            foreach (var record in situationTable[key])
            {
                if (!counts.ContainsKey(record.action))
                    counts[record.action] = 0;
                counts[record.action]++;
            }

            PlayerAction best = PlayerAction.Wait;
            int bestCount = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > bestCount)
                {
                    best = kvp.Key;
                    bestCount = kvp.Value;
                }
            }
            return best;
        }

        public void SaveProfile()
        {
            var profile = new BehaviorProfile();
            foreach (var kvp in situationTable)
            {
                profile.situations.Add(new SituationHistory
                {
                    key = kvp.Key,
                    actions = kvp.Value
                });
            }

            string json = JsonUtility.ToJson(profile, true);
            string path = Path.Combine(Application.persistentDataPath, "ghost_profile.json");
            File.WriteAllText(path, json);
            Debug.Log($"[Behavior] Profile saved: {path} ({situationTable.Count} situations)");
        }

        public void LoadProfile()
        {
            string path = Path.Combine(Application.persistentDataPath, "ghost_profile.json");
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var profile = JsonUtility.FromJson<BehaviorProfile>(json);

            situationTable.Clear();
            foreach (var sit in profile.situations)
                situationTable[sit.key] = sit.actions;

            Debug.Log($"[Behavior] Profile loaded: {situationTable.Count} situations");
        }

        public void ClearMatch()
        {
            // Keep accumulated data, just reset matchup for next match
            currentMatchup = "unknown";
        }
    }
}
