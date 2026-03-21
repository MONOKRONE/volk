using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupAudio
{
    [MenuItem("Tools/Setup Audio Manager")]
    public static void Setup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        // Find or create AudioManager
        var existing = GameObject.Find("AudioManager");
        if (existing != null) Object.DestroyImmediate(existing);

        var go = new GameObject("AudioManager");
        var am = go.AddComponent<AudioManager>();

        // Load clips
        var punch01 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/punch_01.wav");
        var kick01 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/kick_01.wav");
        var bodyFall = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/body_fall.wav");
        var crowdCheer = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/crowd_cheer.mp3");

        // Assign
        am.punchSounds = punch01 != null ? new AudioClip[] { punch01 } : new AudioClip[0];
        am.kickSounds = kick01 != null ? new AudioClip[] { kick01 } : new AudioClip[0];
        am.hitReceiveSounds = punch01 != null ? new AudioClip[] { punch01 } : new AudioClip[0]; // reuse punch as hit for now
        am.bodyFallSound = bodyFall;
        am.crowdCheerSound = crowdCheer;
        am.roundStartSound = crowdCheer; // reuse crowd cheer as round start for now

        EditorUtility.SetDirty(go);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("AudioManager setup complete!");
        Debug.Log($"  punchSounds: {am.punchSounds.Length}");
        Debug.Log($"  kickSounds: {am.kickSounds.Length}");
        Debug.Log($"  hitReceiveSounds: {am.hitReceiveSounds.Length}");
        Debug.Log($"  bodyFallSound: {(am.bodyFallSound != null ? am.bodyFallSound.name : "NULL")}");
        Debug.Log($"  crowdCheerSound: {(am.crowdCheerSound != null ? am.crowdCheerSound.name : "NULL")}");
        Debug.Log($"  roundStartSound: {(am.roundStartSound != null ? am.roundStartSound.name : "NULL")}");
    }
}
