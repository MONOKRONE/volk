using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ensures MainMenu and CombatTest scenes are in Build Settings.
/// Run from: VOLK > Setup Build Settings
/// </summary>
public class SetupBuildSettings
{
    static readonly string[] requiredScenes = {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/CombatTest.unity"
    };

    [MenuItem("VOLK/Setup Build Settings")]
    public static void Execute()
    {
        var current = EditorBuildSettings.scenes.ToList();
        var paths = current.Select(s => s.path).ToHashSet();

        foreach (var scene in requiredScenes)
        {
            if (!paths.Contains(scene))
            {
                current.Add(new EditorBuildSettingsScene(scene, true));
                UnityEngine.Debug.Log($"[BuildSettings] Added: {scene}");
            }
            else
            {
                // Ensure enabled
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i].path == scene && !current[i].enabled)
                    {
                        current[i] = new EditorBuildSettingsScene(scene, true);
                        UnityEngine.Debug.Log($"[BuildSettings] Enabled: {scene}");
                    }
                }
            }
        }

        // Ensure MainMenu is at index 0 (startup scene)
        int mainMenuIdx = current.FindIndex(s => s.path.Contains("MainMenu"));
        if (mainMenuIdx > 0)
        {
            var mm = current[mainMenuIdx];
            current.RemoveAt(mainMenuIdx);
            current.Insert(0, mm);
            UnityEngine.Debug.Log("[BuildSettings] Moved MainMenu to index 0 (startup scene)");
        }

        EditorBuildSettings.scenes = current.ToArray();
        UnityEngine.Debug.Log($"[BuildSettings] Total scenes: {current.Count}");
    }
}
