using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SetupAudioVariations
{
    [MenuItem("VOLK/Setup Audio Manager Clips")]
    static void Setup()
    {
        // Find AudioManager in scene or prefab
        var am = Object.FindFirstObjectByType<AudioManager>();
        if (am == null)
        {
            Debug.LogError("[VOLK] AudioManager not found in scene!");
            return;
        }

        // Auto-assign clips from organized folders
        am.punchSounds = LoadClipsFromFolder("Assets/Audio/SFX/Punch");
        am.kickSounds = LoadClipsFromFolder("Assets/Audio/SFX/Kick");
        am.blockSounds = LoadClipsFromFolder("Assets/Audio/SFX/Block");
        am.hitReceiveSounds = LoadClipsFromFolder("Assets/Audio/SFX/Punch"); // reuse punch as hit for now

        var koClips = LoadClipsFromFolder("Assets/Audio/SFX/KO");
        if (koClips.Length > 0) am.bodyFallSound = koClips[0];

        var ambientClips = LoadClipsFromFolder("Assets/Audio/SFX/Ambient");
        if (ambientClips.Length > 0) am.crowdCheerSound = ambientClips[0];

        EditorUtility.SetDirty(am);
        Debug.Log($"[VOLK] AudioManager clips assigned! Punch:{am.punchSounds.Length} Kick:{am.kickSounds.Length} Block:{am.blockSounds.Length}");
    }

    static AudioClip[] LoadClipsFromFolder(string folderPath)
    {
        var clips = new List<AudioClip>();
        if (!Directory.Exists(folderPath)) return clips.ToArray();

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) clips.Add(clip);
        }
        return clips.ToArray();
    }
}
