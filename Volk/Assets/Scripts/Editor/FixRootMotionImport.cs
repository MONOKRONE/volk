using UnityEngine;
using UnityEditor;
using System.IO;

public class FixRootMotionImport
{
    [MenuItem("Tools/Fix Root Motion Import Settings")]
    public static void Fix()
    {
        string[] files = { "Run", "Walk", "Idle", "HookPunch", "MMAKick", "BodyBlock", "TakingPunch", "ReceivingUppercut", "Death", "Jump" };
        bool[] looping = { true, true, true, false, false, false, false, false, false, false };

        for (int i = 0; i < files.Length; i++)
        {
            string path = $"Assets/Animations/{files[i]}.fbx";
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { Debug.LogWarning($"No importer for {path}"); continue; }

            var clips = importer.clipAnimations;
            if (clips.Length == 0) clips = importer.defaultClipAnimations;

            for (int c = 0; c < clips.Length; c++)
            {
                clips[c].lockRootRotation = true;
                clips[c].lockRootHeightY = true;
                clips[c].lockRootPositionXZ = true;
                clips[c].keepOriginalOrientation = false;
                clips[c].keepOriginalPositionY = false;
                clips[c].keepOriginalPositionXZ = false;

                if (looping[i])
                {
                    clips[c].loopTime = true;
                    clips[c].loopPose = true;
                }
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"Fixed root motion: {files[i]}.fbx (loop={looping[i]})");
        }

        // Clean missing scripts from scene objects
        CleanMissingScripts("Player_Root");
        CleanMissingScripts("Enemy_Root");

        Debug.Log("Root motion import fix complete!");
    }

    static void CleanMissingScripts(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0) Debug.Log($"  Removed {removed} missing scripts from {name}");
        }
    }
}
