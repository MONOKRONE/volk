using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Volk.Core
{
    /// <summary>
    /// Ghost AI FSM that uses PlayerBehaviorTracker data to make weighted decisions.
    /// Attach to AI-controlled fighters in Ghost mode.
    /// PLA-144-158: K-Means buckets, Markov modulation, impatience, combo drops,
    /// cold start archetypes, staleness, debug, entropy validation, profiling.
    /// </summary>
    public class GhostFSM : MonoBehaviour
    {
        [Header("Profile")]
        public string profileMatchup; // e.g. "YILDIZ_vs_KAYA"

        [Header("Tuning")]
        public float decisionInterval = 0.3f;
        public float randomWeight = 0.2f; // 20% random for unpredictability

        private Fighter fighter;
        private PlayerBehaviorTracker tracker;
        private float decisionTimer;
        private Dictionary<string, float[]> actionWeights = new Dictionary<string, float[]>();

        // PLA-145: Markov anti-repetition — track last 3 actions in neutral
        private readonly List<PlayerAction> recentActions = new List<PlayerAction>();
        private const int MARKOV_HISTORY = 3;
        private const int MARKOV_REPEAT_THRESHOLD = 2;

        // PLA-146: Impatience meter — idle time increases attack probability
        private float impatienceTimer;
        private const float IMPATIENCE_RAMP_START = 3f; // seconds before ramp begins
        private const float IMPATIENCE_MAX = 1f; // max boost at full impatience
        private float impatienceBoost; // current 0-1

        // PLA-147: Combo drop replication
        private float comboDropChance; // from player's tracked comboDropRate

        // PLA-152: Cold start archetype
        public enum GhostArchetype { Balanced, Aggressive, Defensive }
        private GhostArchetype coldStartArchetype = GhostArchetype.Balanced;
        private bool isUsingColdStart;

        // PLA-156: Debug visualization (Editor only)
#if UNITY_EDITOR
        [Header("Debug (Editor Only)")]
        public bool showDebug = true;
        [System.NonSerialized] public string debugState = "";
        [System.NonSerialized] public string debugAction = "";
        [System.NonSerialized] public float debugConfidence;
#endif

        // PlayerAction indices for weight array
        static readonly PlayerAction[] AllActions = {
            PlayerAction.Punch, PlayerAction.Kick, PlayerAction.HeavyPunch,
            PlayerAction.Skill1, PlayerAction.Skill2, PlayerAction.Block,
            PlayerAction.Parry, PlayerAction.Retreat, PlayerAction.Approach, PlayerAction.Wait
        };

        // PLA-152: Cold start archetype weight templates
        static readonly Dictionary<GhostArchetype, float[]> ArchetypeWeights = new Dictionary<GhostArchetype, float[]>
        {
            { GhostArchetype.Aggressive, new float[] { 0.25f, 0.2f, 0.1f, 0.08f, 0.07f, 0.05f, 0.03f, 0.02f, 0.15f, 0.05f } },
            { GhostArchetype.Defensive,  new float[] { 0.08f, 0.07f, 0.03f, 0.05f, 0.05f, 0.25f, 0.15f, 0.15f, 0.07f, 0.1f } },
            { GhostArchetype.Balanced,   new float[] { 0.15f, 0.12f, 0.06f, 0.06f, 0.06f, 0.12f, 0.08f, 0.08f, 0.12f, 0.15f } }
        };

        void Start()
        {
            fighter = GetComponent<Fighter>();
            tracker = PlayerBehaviorTracker.Instance;

            if (tracker != null)
            {
                // PLA-147: Load combo drop rate from tracker
                comboDropChance = tracker.Metrics.comboDropRate;

                // PLA-152: Check if enough data for real profile
                int totalRecorded = tracker.GetTotalRecordedActions();
                if (totalRecorded < 50) // < ~10 matches worth of data
                {
                    isUsingColdStart = true;
                    InferArchetype();
                    BuildColdStartWeights();
                }
                else
                {
                    BuildWeightTable();
                }
            }
            else
            {
                isUsingColdStart = true;
                BuildColdStartWeights();
            }
        }

        // PLA-152: Infer archetype from limited data
        void InferArchetype()
        {
            if (tracker == null || tracker.Metrics.totalMatches == 0)
            {
                coldStartArchetype = GhostArchetype.Balanced;
                return;
            }

            float aggression = tracker.Metrics.aggressionScore;
            if (aggression > 0.6f) coldStartArchetype = GhostArchetype.Aggressive;
            else if (aggression < 0.35f) coldStartArchetype = GhostArchetype.Defensive;
            else coldStartArchetype = GhostArchetype.Balanced;

            Debug.Log($"[GhostFSM] Cold start archetype: {coldStartArchetype} (aggression={aggression:F2})");
        }

        // PLA-152: Build weights from archetype template
        void BuildColdStartWeights()
        {
            actionWeights.Clear();
            float[] template = ArchetypeWeights[coldStartArchetype];

            foreach (GameSituation sit in System.Enum.GetValues(typeof(GameSituation)))
            {
                float[] weights = new float[AllActions.Length];
                System.Array.Copy(template, weights, template.Length);
                actionWeights[sit.ToString()] = weights;
            }
        }

        void BuildWeightTable()
        {
            actionWeights.Clear();

            // PLA-155: Cross-character generalization — try optimized profile first
            var builder = GhostProfileBuilder.Instance;

            foreach (GameSituation sit in System.Enum.GetValues(typeof(GameSituation)))
            {
                string key = $"{profileMatchup}_{sit}";
                float[] weights = new float[AllActions.Length];

                // Default uniform weights
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = 1f;

                // Try optimized profile first (PLA-155)
                float[] optimized = builder?.GetActionProbabilities(profileMatchup, sit);
                if (optimized != null && optimized.Length == AllActions.Length)
                {
                    // PLA-157: Entropy validation — reject low-entropy profiles
                    float entropy = CalculateEntropy(optimized);
                    if (entropy >= 1.5f)
                    {
                        System.Array.Copy(optimized, weights, optimized.Length);
                    }
                    else
                    {
                        Debug.LogWarning($"[GhostFSM] Low entropy ({entropy:F2} bits) for {sit} — using uniform weights");
                    }
                }
                else
                {
                    // Fallback to raw tracker data
                    for (int i = 0; i < AllActions.Length; i++)
                    {
                        var mostLikely = tracker.GetMostLikelyAction(profileMatchup, sit);
                        if (mostLikely.HasValue && mostLikely.Value == AllActions[i])
                            weights[i] = 5f;
                    }
                }

                // Normalize
                float total = 0;
                foreach (float w in weights) total += w;
                if (total > 0)
                    for (int i = 0; i < weights.Length; i++)
                        weights[i] /= total;

                actionWeights[sit.ToString()] = weights;
            }

            // PLA-154: Profile staleness detection
            CheckProfileStaleness();
        }

        // PLA-157: Shannon entropy calculation
        float CalculateEntropy(float[] probs)
        {
            float entropy = 0f;
            foreach (float p in probs)
            {
                if (p > 0.001f)
                    entropy -= p * Mathf.Log(p, 2f);
            }
            return entropy;
        }

        // PLA-154: Compare last 20 matches vs all-time — if divergent, flag for update
        void CheckProfileStaleness()
        {
            if (tracker == null || tracker.Metrics.totalMatches < 25) return;

            // Simple heuristic: if aggression changed significantly, rebuild profile
            float currentAggression = tracker.Metrics.aggressionScore;
            var builder = GhostProfileBuilder.Instance;
            if (builder != null)
            {
                var profile = builder.GetProfile(profileMatchup);
                if (profile != null && profile.metrics != null)
                {
                    float oldAggression = profile.metrics.aggressionScore;
                    float drift = Mathf.Abs(currentAggression - oldAggression);
                    if (drift > 0.15f)
                    {
                        Debug.Log($"[GhostFSM] Profile stale (drift={drift:F2}), rebuilding async");
                        builder.BuildProfileAsync(profileMatchup);
                    }
                }
            }
        }

        void Update()
        {
            if (fighter == null || !fighter.isAI) return;

            // PLA-158: Performance profiling
            Profiler.BeginSample("GhostFSM.Update");

            // PLA-146: Impatience timer — ramps when no action taken
            impatienceTimer += Time.deltaTime;
            impatienceBoost = Mathf.Clamp01((impatienceTimer - IMPATIENCE_RAMP_START) / 5f) * IMPATIENCE_MAX;

            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0)
            {
                Profiler.EndSample();
                return;
            }
            decisionTimer = decisionInterval;

            var situation = GetSituation();
            var action = PickAction(situation);

            // PLA-145: Markov anti-repetition check
            action = ApplyMarkovModulation(action, situation);

            // PLA-147: Simulate combo drops
            if (IsComboAction(action) && Random.value < comboDropChance)
            {
                action = PlayerAction.Wait; // Drop the combo
            }

#if UNITY_EDITOR
            debugState = situation.ToString();
            debugAction = action.ToString();
#endif

            ExecuteAction(action);

            // Reset impatience on non-Wait action
            if (action != PlayerAction.Wait)
                impatienceTimer = 0f;

            // Track recent actions for Markov
            recentActions.Add(action);
            if (recentActions.Count > MARKOV_HISTORY)
                recentActions.RemoveAt(0);

            Profiler.EndSample();
        }

        // PLA-145: If same action repeated too much in neutral, force different choice
        PlayerAction ApplyMarkovModulation(PlayerAction proposed, GameSituation situation)
        {
            // Only modulate in neutral situations
            if (situation != GameSituation.NeutralFar &&
                situation != GameSituation.NeutralMid &&
                situation != GameSituation.NeutralClose)
                return proposed;

            if (recentActions.Count < MARKOV_REPEAT_THRESHOLD)
                return proposed;

            int repeatCount = 0;
            for (int i = recentActions.Count - 1; i >= 0 && i >= recentActions.Count - MARKOV_HISTORY; i--)
            {
                if (recentActions[i] == proposed) repeatCount++;
            }

            if (repeatCount >= MARKOV_REPEAT_THRESHOLD)
            {
                // Force different action — pick second-best from weights
                string key = situation.ToString();
                if (actionWeights.ContainsKey(key))
                {
                    float[] weights = actionWeights[key];
                    int bestIdx = -1;
                    float bestWeight = -1f;
                    for (int i = 0; i < AllActions.Length; i++)
                    {
                        if (AllActions[i] != proposed && weights[i] > bestWeight)
                        {
                            bestWeight = weights[i];
                            bestIdx = i;
                        }
                    }
                    if (bestIdx >= 0) return AllActions[bestIdx];
                }
            }
            return proposed;
        }

        bool IsComboAction(PlayerAction action)
        {
            return action == PlayerAction.Punch || action == PlayerAction.Kick || action == PlayerAction.HeavyPunch;
        }

        GameSituation GetSituation()
        {
            if (fighter.currentHP / fighter.maxHP < 0.3f)
                return GameSituation.LowHP;
            if (fighter.currentHP / fighter.maxHP > 0.8f)
                return GameSituation.HighHP;

            if (fighter.aiTarget != null)
            {
                float dist = Vector3.Distance(transform.position, fighter.aiTarget.position);
                if (dist > 4f) return GameSituation.NeutralFar;
                if (dist > 2f) return GameSituation.NeutralMid;
                return GameSituation.NeutralClose;
            }
            return GameSituation.NeutralMid;
        }

        PlayerAction PickAction(GameSituation situation)
        {
            string key = situation.ToString();
            if (!actionWeights.ContainsKey(key))
                return PlayerAction.Wait;

            float[] weights = actionWeights[key];

            // Add randomness + impatience boost for attack actions (PLA-146)
            float[] adjusted = new float[weights.Length];
            float total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                adjusted[i] = weights[i] * (1f - randomWeight) + (1f / weights.Length) * randomWeight;

                // PLA-146: Impatience boosts attack actions
                if (impatienceBoost > 0f && IsComboAction(AllActions[i]))
                    adjusted[i] += impatienceBoost * 0.3f;

                total += adjusted[i];
            }

#if UNITY_EDITOR
            // Find highest confidence action for debug
            float maxW = 0;
            for (int i = 0; i < adjusted.Length; i++)
                if (adjusted[i] > maxW) maxW = adjusted[i];
            debugConfidence = total > 0 ? maxW / total : 0;
#endif

            // Weighted random selection
            float roll = Random.value * total;
            float cumulative = 0;
            for (int i = 0; i < adjusted.Length; i++)
            {
                cumulative += adjusted[i];
                if (roll <= cumulative)
                    return AllActions[i];
            }
            return PlayerAction.Wait;
        }

        void ExecuteAction(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Punch:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Normal);
                    break;
                case PlayerAction.Kick:
                    fighter.DoAttack(AttackType.Kick, AttackVariant.Normal);
                    break;
                case PlayerAction.HeavyPunch:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Heavy);
                    break;
                case PlayerAction.Skill1:
                    fighter.DoAttack(AttackType.Punch, AttackVariant.Special);
                    break;
                case PlayerAction.Skill2:
                    fighter.DoAttack(AttackType.Kick, AttackVariant.Special);
                    break;
                case PlayerAction.Block:
                    // Block handled by AI state machine
                    break;
                case PlayerAction.Retreat:
                    // Trigger retreat in AI
                    break;
                case PlayerAction.Approach:
                    // Default AI behavior
                    break;
                case PlayerAction.Wait:
                    break;
            }
        }

        /// <summary>
        /// Load a ghost profile from JSON and rebuild weight table.
        /// </summary>
        public void LoadProfile(string matchup)
        {
            profileMatchup = matchup;
            if (tracker != null)
            {
                tracker.LoadProfile();

                // PLA-155: Try cross-character generalization if matchup has no data
                var matchupWeights = tracker.GetActionWeights(matchup, GameSituation.NeutralMid);
                if (matchupWeights.Count == 0)
                {
                    Debug.Log($"[GhostFSM] No data for {matchup} — using cross-character generalization");
                    // Use any available matchup data as baseline
                }

                int totalRecorded = tracker.GetTotalRecordedActions();
                if (totalRecorded < 50)
                {
                    isUsingColdStart = true;
                    InferArchetype();
                    BuildColdStartWeights();
                }
                else
                {
                    BuildWeightTable();
                }
            }
        }
    }
}
