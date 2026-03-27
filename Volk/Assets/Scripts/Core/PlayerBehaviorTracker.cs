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
    public class BehaviorMetrics
    {
        public float aggressionScore;    // 0-1: ratio of attack actions to total
        public float avgReactionDelay;   // Average time between situation change and action
        public float reactionDelayMs;    // PLA-150: milliseconds for display
        public float comboDropRate;      // Ratio of combos dropped (no follow-up in window)
        public int totalActions;
        public int totalMatches;
        public float avgActionsPerMatch;

        // PLA-149: Distance histogram — how much time at close/mid/far range
        public float distanceClose;      // 0-1 normalized time at close range
        public float distanceMid;        // 0-1 normalized time at mid range
        public float distanceFar;        // 0-1 normalized time at far range
    }

    [System.Serializable]
    public class BehaviorProfile
    {
        public List<SituationHistory> situations = new List<SituationHistory>();
        public BehaviorMetrics metrics = new BehaviorMetrics();
    }

    public class PlayerBehaviorTracker : MonoBehaviour
    {
        public static PlayerBehaviorTracker Instance { get; private set; }

        private Dictionary<string, List<ActionRecord>> situationTable = new Dictionary<string, List<ActionRecord>>();
        private string currentMatchup = "unknown";

        // Per-match metrics tracking
        private int matchActionCount;
        private int matchAttackCount;
        private int matchComboAttempts;
        private int matchComboDrops;
        private float lastSituationChangeTime;
        private List<float> reactionDelays = new List<float>();

        // PLA-149: Distance histogram per-match
        private float matchTimeClose;
        private float matchTimeMid;
        private float matchTimeFar;
        private float lastDistanceUpdate;

        // Running metrics
        public BehaviorMetrics Metrics { get; private set; } = new BehaviorMetrics();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetMatchup(string playerChar, string enemyChar)
        {
            currentMatchup = $"{playerChar}_vs_{enemyChar}";
            matchActionCount = 0;
            matchAttackCount = 0;
            matchComboAttempts = 0;
            matchComboDrops = 0;
            matchTimeClose = 0;
            matchTimeMid = 0;
            matchTimeFar = 0;
            lastDistanceUpdate = Time.time;
            reactionDelays.Clear();
            lastSituationChangeTime = Time.time;
        }

        public void Record(GameSituation situation, PlayerAction action)
        {
            string key = $"{currentMatchup}_{situation}";
            if (!situationTable.ContainsKey(key))
                situationTable[key] = new List<ActionRecord>();

            float now = Time.time;
            situationTable[key].Add(new ActionRecord
            {
                action = action,
                timestamp = now
            });

            // Track metrics
            matchActionCount++;
            bool isAttack = action == PlayerAction.Punch || action == PlayerAction.Kick ||
                           action == PlayerAction.HeavyPunch || action == PlayerAction.Skill1 ||
                           action == PlayerAction.Skill2;
            if (isAttack) matchAttackCount++;

            // Reaction delay
            float reactionDelay = now - lastSituationChangeTime;
            if (reactionDelay > 0 && reactionDelay < 5f) // Filter out stale values
                reactionDelays.Add(reactionDelay);
        }

        public void OnSituationChanged()
        {
            lastSituationChangeTime = Time.time;
        }

        /// <summary>
        /// PLA-149: Call from Fighter.Update() to track distance over time.
        /// </summary>
        public void UpdateDistanceHistogram(float distanceToEnemy)
        {
            float dt = Time.time - lastDistanceUpdate;
            lastDistanceUpdate = Time.time;
            if (dt <= 0 || dt > 1f) return; // skip stale

            if (distanceToEnemy < 2f) matchTimeClose += dt;
            else if (distanceToEnemy < 4f) matchTimeMid += dt;
            else matchTimeFar += dt;
        }

        public void OnComboAttempt(bool completed)
        {
            matchComboAttempts++;
            if (!completed) matchComboDrops++;
        }

        public void OnMatchEnd()
        {
            Metrics.totalMatches++;
            Metrics.totalActions += matchActionCount;

            // Aggression: ratio of attacks to total actions
            if (matchActionCount > 0)
            {
                float matchAggression = (float)matchAttackCount / matchActionCount;
                // Exponential moving average
                Metrics.aggressionScore = Metrics.aggressionScore * 0.8f + matchAggression * 0.2f;
            }

            // Reaction delay average
            if (reactionDelays.Count > 0)
            {
                float sum = 0;
                foreach (float d in reactionDelays) sum += d;
                float matchAvg = sum / reactionDelays.Count;
                Metrics.avgReactionDelay = Metrics.avgReactionDelay * 0.7f + matchAvg * 0.3f;
            }

            // Combo drop rate
            if (matchComboAttempts > 0)
            {
                float matchDropRate = (float)matchComboDrops / matchComboAttempts;
                Metrics.comboDropRate = Metrics.comboDropRate * 0.7f + matchDropRate * 0.3f;
            }

            Metrics.avgActionsPerMatch = Metrics.totalMatches > 0
                ? (float)Metrics.totalActions / Metrics.totalMatches : 0f;

            // PLA-150: Reaction delay in ms
            Metrics.reactionDelayMs = Metrics.avgReactionDelay * 1000f;

            // PLA-149: Distance histogram (EMA)
            float totalDist = matchTimeClose + matchTimeMid + matchTimeFar;
            if (totalDist > 0)
            {
                float close = matchTimeClose / totalDist;
                float mid = matchTimeMid / totalDist;
                float far = matchTimeFar / totalDist;
                Metrics.distanceClose = Metrics.distanceClose * 0.7f + close * 0.3f;
                Metrics.distanceMid = Metrics.distanceMid * 0.7f + mid * 0.3f;
                Metrics.distanceFar = Metrics.distanceFar * 0.7f + far * 0.3f;
            }
        }

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

        /// <summary>
        /// Get action distribution weights for a situation (for GhostFSM).
        /// </summary>
        public Dictionary<PlayerAction, float> GetActionWeights(string matchup, GameSituation situation)
        {
            var weights = new Dictionary<PlayerAction, float>();
            string key = $"{matchup}_{situation}";

            if (!situationTable.ContainsKey(key) || situationTable[key].Count == 0)
                return weights;

            var counts = new Dictionary<PlayerAction, int>();
            int total = 0;
            foreach (var record in situationTable[key])
            {
                if (!counts.ContainsKey(record.action))
                    counts[record.action] = 0;
                counts[record.action]++;
                total++;
            }

            if (total > 0)
            {
                foreach (var kvp in counts)
                    weights[kvp.Key] = (float)kvp.Value / total;
            }
            return weights;
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
            profile.metrics = Metrics;

            string json = JsonUtility.ToJson(profile, true);
            string path = Path.Combine(Application.persistentDataPath, "ghost_profile.json");
            File.WriteAllText(path, json);
            Debug.Log($"[Behavior] Profile saved: {path} ({situationTable.Count} situations, aggression={Metrics.aggressionScore:F2})");
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

            if (profile.metrics != null)
                Metrics = profile.metrics;

            Debug.Log($"[Behavior] Profile loaded: {situationTable.Count} situations, {Metrics.totalMatches} matches");
        }

        public void ClearMatch()
        {
            currentMatchup = "unknown";
        }

        public int GetTotalRecordedActions()
        {
            int total = 0;
            foreach (var kvp in situationTable)
                total += kvp.Value.Count;
            return total;
        }
    }
}
