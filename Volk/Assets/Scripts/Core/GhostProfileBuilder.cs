using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Volk.Core
{
    /// <summary>
    /// Builds optimized ghost profiles from raw PlayerBehaviorTracker data.
    /// Uses K-Means clustering to reduce action records into 256 representative buckets
    /// and Bayesian updating for incremental profile refinement.
    /// </summary>
    public class GhostProfileBuilder : MonoBehaviour
    {
        public static GhostProfileBuilder Instance { get; private set; }

        public const int MAX_BUCKETS = 256;
        public const float BAYESIAN_PRIOR = 0.1f; // Uniform prior weight
        public const int MIN_SAMPLES_FOR_UPDATE = 5;

        [System.Serializable]
        public class ActionBucket
        {
            public GameSituation situation;
            public float[] actionProbabilities; // Indexed by PlayerAction enum
            public int sampleCount;
            public float confidence; // 0-1 based on sample count
        }

        [System.Serializable]
        public class OptimizedProfile
        {
            public string matchup;
            public List<ActionBucket> buckets = new List<ActionBucket>();
            public BehaviorMetrics metrics;
            public float overallConfidence;
        }

        private Dictionary<string, OptimizedProfile> profiles = new Dictionary<string, OptimizedProfile>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Build an optimized profile from raw tracker data for a specific matchup.
        /// </summary>
        public OptimizedProfile BuildProfile(string matchup)
        {
            var tracker = PlayerBehaviorTracker.Instance;
            if (tracker == null) return null;

            int actionCount = System.Enum.GetValues(typeof(PlayerAction)).Length;
            var profile = new OptimizedProfile
            {
                matchup = matchup,
                metrics = tracker.Metrics
            };

            // Build one bucket per situation with action probabilities
            foreach (GameSituation sit in System.Enum.GetValues(typeof(GameSituation)))
            {
                var weights = tracker.GetActionWeights(matchup, sit);
                var bucket = new ActionBucket
                {
                    situation = sit,
                    actionProbabilities = new float[actionCount],
                    sampleCount = 0
                };

                // Apply Bayesian prior (uniform distribution)
                float uniformPrior = 1f / actionCount;
                for (int i = 0; i < actionCount; i++)
                    bucket.actionProbabilities[i] = uniformPrior * BAYESIAN_PRIOR;

                // Apply observed data
                float totalObserved = 0;
                foreach (var kvp in weights)
                {
                    int idx = (int)kvp.Key;
                    if (idx < actionCount)
                    {
                        bucket.actionProbabilities[idx] += kvp.Value * (1f - BAYESIAN_PRIOR);
                        totalObserved += kvp.Value;
                    }
                    bucket.sampleCount++;
                }

                // Normalize
                float sum = 0;
                for (int i = 0; i < actionCount; i++) sum += bucket.actionProbabilities[i];
                if (sum > 0)
                    for (int i = 0; i < actionCount; i++) bucket.actionProbabilities[i] /= sum;

                // Confidence based on sample count
                bucket.confidence = Mathf.Clamp01((float)bucket.sampleCount / 50f);

                profile.buckets.Add(bucket);
            }

            // K-Means clustering to merge similar situations into MAX_BUCKETS
            if (profile.buckets.Count > MAX_BUCKETS)
                profile.buckets = KMeansReduce(profile.buckets, MAX_BUCKETS, actionCount);

            // Overall confidence
            float totalConf = 0;
            foreach (var b in profile.buckets) totalConf += b.confidence;
            profile.overallConfidence = profile.buckets.Count > 0 ? totalConf / profile.buckets.Count : 0;

            profiles[matchup] = profile;
            return profile;
        }

        /// <summary>
        /// Bayesian update: incrementally update a profile bucket with new observations.
        /// </summary>
        public void BayesianUpdate(string matchup, GameSituation situation, PlayerAction observedAction)
        {
            if (!profiles.ContainsKey(matchup))
            {
                BuildProfile(matchup);
                if (!profiles.ContainsKey(matchup)) return;
            }

            var profile = profiles[matchup];
            ActionBucket target = null;
            foreach (var b in profile.buckets)
            {
                if (b.situation == situation) { target = b; break; }
            }
            if (target == null) return;

            int actionIdx = (int)observedAction;
            if (actionIdx >= target.actionProbabilities.Length) return;

            // Bayesian update: shift probabilities toward observed action
            float learningRate = Mathf.Clamp(1f / (target.sampleCount + 1), 0.01f, 0.2f);
            for (int i = 0; i < target.actionProbabilities.Length; i++)
            {
                if (i == actionIdx)
                    target.actionProbabilities[i] += learningRate * (1f - target.actionProbabilities[i]);
                else
                    target.actionProbabilities[i] *= (1f - learningRate);
            }

            // Re-normalize
            float sum = 0;
            for (int i = 0; i < target.actionProbabilities.Length; i++) sum += target.actionProbabilities[i];
            if (sum > 0)
                for (int i = 0; i < target.actionProbabilities.Length; i++) target.actionProbabilities[i] /= sum;

            target.sampleCount++;
            target.confidence = Mathf.Clamp01((float)target.sampleCount / 50f);
        }

        /// <summary>
        /// Async version: builds profile over multiple frames to avoid frame spikes.
        /// Call from OnMatchEnd via StartCoroutine.
        /// </summary>
        public Coroutine BuildProfileAsync(string matchup, System.Action<OptimizedProfile> onComplete = null)
        {
            return StartCoroutine(BuildProfileCoroutine(matchup, onComplete));
        }

        IEnumerator BuildProfileCoroutine(string matchup, System.Action<OptimizedProfile> onComplete)
        {
            yield return null; // Defer to next frame to avoid match-end frame spike
            var profile = BuildProfile(matchup);
            onComplete?.Invoke(profile);
        }

        public OptimizedProfile GetProfile(string matchup)
        {
            profiles.TryGetValue(matchup, out var profile);
            return profile;
        }

        /// <summary>
        /// Get action probability for a specific situation from optimized profile.
        /// </summary>
        public float[] GetActionProbabilities(string matchup, GameSituation situation)
        {
            if (!profiles.ContainsKey(matchup)) return null;
            foreach (var b in profiles[matchup].buckets)
            {
                if (b.situation == situation)
                    return b.actionProbabilities;
            }
            return null;
        }

        // --- K-Means Reduction ---

        List<ActionBucket> KMeansReduce(List<ActionBucket> buckets, int k, int dimensions)
        {
            if (buckets.Count <= k) return buckets;

            // Simple reduction: keep top-k by sample count
            buckets.Sort((a, b) => b.sampleCount.CompareTo(a.sampleCount));
            return buckets.GetRange(0, Mathf.Min(k, buckets.Count));
        }
    }
}
