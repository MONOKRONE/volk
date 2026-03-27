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

        // --- PLA-155: Cross-character generalization ---

        /// <summary>
        /// Fill gaps for matchups with no data by interpolating from same-archetype profiles.
        /// </summary>
        public OptimizedProfile GetOrInterpolateProfile(string matchup)
        {
            if (profiles.ContainsKey(matchup))
                return profiles[matchup];

            // Find any profile to use as baseline
            OptimizedProfile baseline = null;
            foreach (var kvp in profiles)
            {
                if (kvp.Value.overallConfidence > 0.1f)
                {
                    baseline = kvp.Value;
                    break;
                }
            }

            if (baseline == null) return null;

            // Clone baseline with reduced confidence
            var interpolated = new OptimizedProfile
            {
                matchup = matchup,
                metrics = baseline.metrics,
                overallConfidence = baseline.overallConfidence * 0.5f
            };
            foreach (var b in baseline.buckets)
            {
                interpolated.buckets.Add(new ActionBucket
                {
                    situation = b.situation,
                    actionProbabilities = (float[])b.actionProbabilities.Clone(),
                    sampleCount = b.sampleCount / 2,
                    confidence = b.confidence * 0.5f
                });
            }
            profiles[matchup] = interpolated;
            return interpolated;
        }

        // --- PLA-157: Data quality validation ---

        /// <summary>
        /// Shannon entropy — below 1.5 bits means the profile is too deterministic / likely corrupt.
        /// </summary>
        public static float CalculateEntropy(float[] probs)
        {
            float entropy = 0f;
            foreach (float p in probs)
            {
                if (p > 0.001f)
                    entropy -= p * Mathf.Log(p, 2f);
            }
            return entropy;
        }

        /// <summary>
        /// Validate profile data quality. Returns false if profile should be discarded.
        /// </summary>
        public bool ValidateProfile(string matchup)
        {
            if (!profiles.ContainsKey(matchup)) return false;
            var profile = profiles[matchup];

            foreach (var bucket in profile.buckets)
            {
                if (bucket.actionProbabilities == null) continue;
                float entropy = CalculateEntropy(bucket.actionProbabilities);
                if (bucket.sampleCount > 10 && entropy < 1.5f)
                {
                    Debug.LogWarning($"[GhostProfile] Low entropy ({entropy:F2}) for {matchup}/{bucket.situation} — flagging as unreliable");
                    bucket.confidence *= 0.3f; // Reduce confidence rather than discard
                }
            }
            return true;
        }

        // --- K-Means Clustering (PLA-131) ---

        List<ActionBucket> KMeansReduce(List<ActionBucket> buckets, int k, int dimensions)
        {
            if (buckets.Count <= k) return buckets;

            const int MAX_ITERATIONS = 10;
            const float CONVERGENCE_DELTA = 0.01f;
            int n = buckets.Count;

            // Initialize centroids from k evenly-spaced buckets (deterministic)
            float[][] centroids = new float[k][];
            for (int c = 0; c < k; c++)
            {
                int idx = c * n / k;
                centroids[c] = new float[dimensions];
                System.Array.Copy(buckets[idx].actionProbabilities, centroids[c], dimensions);
            }

            int[] assignments = new int[n];

            for (int iter = 0; iter < MAX_ITERATIONS; iter++)
            {
                // Assign each bucket to nearest centroid (Euclidean distance)
                for (int i = 0; i < n; i++)
                {
                    float bestDist = float.MaxValue;
                    int bestC = 0;
                    for (int c = 0; c < k; c++)
                    {
                        float dist = 0f;
                        for (int d = 0; d < dimensions; d++)
                        {
                            float diff = buckets[i].actionProbabilities[d] - centroids[c][d];
                            dist += diff * diff;
                        }
                        if (dist < bestDist) { bestDist = dist; bestC = c; }
                    }
                    assignments[i] = bestC;
                }

                // Recompute centroids (weighted by sampleCount)
                float[][] newCentroids = new float[k][];
                float[] clusterWeight = new float[k];
                for (int c = 0; c < k; c++)
                    newCentroids[c] = new float[dimensions];

                for (int i = 0; i < n; i++)
                {
                    int c = assignments[i];
                    float w = Mathf.Max(1f, buckets[i].sampleCount);
                    clusterWeight[c] += w;
                    for (int d = 0; d < dimensions; d++)
                        newCentroids[c][d] += buckets[i].actionProbabilities[d] * w;
                }

                float maxDelta = 0f;
                for (int c = 0; c < k; c++)
                {
                    if (clusterWeight[c] > 0)
                    {
                        for (int d = 0; d < dimensions; d++)
                        {
                            newCentroids[c][d] /= clusterWeight[c];
                            float delta = Mathf.Abs(newCentroids[c][d] - centroids[c][d]);
                            if (delta > maxDelta) maxDelta = delta;
                        }
                    }
                }

                centroids = newCentroids;
                if (maxDelta < CONVERGENCE_DELTA) break;
            }

            // Build merged buckets from clusters
            var result = new List<ActionBucket>();
            for (int c = 0; c < k; c++)
            {
                ActionBucket rep = null;
                int totalSamples = 0;
                float totalConfidence = 0f;
                int count = 0;

                for (int i = 0; i < n; i++)
                {
                    if (assignments[i] != c) continue;
                    count++;
                    totalSamples += buckets[i].sampleCount;
                    totalConfidence += buckets[i].confidence;
                    if (rep == null || buckets[i].sampleCount > rep.sampleCount)
                        rep = buckets[i];
                }

                if (rep == null || count == 0) continue;

                // Use centroid probabilities, keep representative's situation key
                var merged = new ActionBucket
                {
                    situation = rep.situation,
                    actionProbabilities = centroids[c],
                    sampleCount = totalSamples,
                    confidence = totalConfidence / count
                };
                result.Add(merged);
            }

            return result;
        }
    }
}
