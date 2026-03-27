#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Volk.Core;

/// <summary>
/// PLA-151: Ghost accuracy test + PLA-156: Debug visualization inspector.
/// Shows GhostFSM internal state in the Inspector during Play Mode.
/// </summary>
[CustomEditor(typeof(GhostFSM))]
public class GhostDebugInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var fsm = (GhostFSM)target;

        if (!Application.isPlaying) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ghost AI Debug", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("State", fsm.debugState);
        EditorGUILayout.LabelField("Last Action", fsm.debugAction);
        EditorGUILayout.LabelField("Confidence", fsm.debugConfidence.ToString("F2"));

        // Show tracker metrics if available
        var tracker = PlayerBehaviorTracker.Instance;
        if (tracker != null)
        {
            var m = tracker.Metrics;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Metrics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Aggression", m.aggressionScore.ToString("F2"));
            EditorGUILayout.LabelField("Reaction (ms)", m.reactionDelayMs.ToString("F0"));
            EditorGUILayout.LabelField("Combo Drop %", (m.comboDropRate * 100f).ToString("F1") + "%");
            EditorGUILayout.LabelField("Distance", $"Close:{m.distanceClose:F2} Mid:{m.distanceMid:F2} Far:{m.distanceFar:F2}");
            EditorGUILayout.LabelField("Total Matches", m.totalMatches.ToString());
            EditorGUILayout.LabelField("Total Actions", m.totalActions.ToString());
        }

        // Profile builder info
        var builder = GhostProfileBuilder.Instance;
        if (builder != null)
        {
            var profile = builder.GetProfile(fsm.profileMatchup);
            if (profile != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Optimized Profile", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Matchup", profile.matchup);
                EditorGUILayout.LabelField("Buckets", profile.buckets.Count.ToString());
                EditorGUILayout.LabelField("Overall Confidence", profile.overallConfidence.ToString("F2"));

                // PLA-157: Show entropy per bucket
                foreach (var bucket in profile.buckets)
                {
                    if (bucket.actionProbabilities == null) continue;
                    float entropy = GhostProfileBuilder.CalculateEntropy(bucket.actionProbabilities);
                    string quality = entropy < 1.5f ? " [LOW]" : "";
                    EditorGUILayout.LabelField($"  {bucket.situation}", $"entropy={entropy:F2} samples={bucket.sampleCount}{quality}");
                }
            }
        }

        if (Application.isPlaying)
            Repaint(); // Continuously refresh in play mode
    }
}
#endif
