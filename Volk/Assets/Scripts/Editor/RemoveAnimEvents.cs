using UnityEngine;
using UnityEditor;

public class RemoveAnimEvents : AssetPostprocessor
{
    [MenuItem("Tools/Remove All Anim Events")]
    static void RemoveAll()
    {
        string[] fbxPaths = {
            "Assets/Animations/HookPunch.fbx",
            "Assets/Animations/MMAKick.fbx",
            "Assets/Animations/Walk.fbx",
            "Assets/Animations/Run.fbx",
            "Assets/Animations/Idle.fbx",
            "Assets/Animations/BodyBlock.fbx",
            "Assets/Animations/Death.fbx",
            "Assets/Animations/Jump.fbx",
            "Assets/Animations/TakingPunch.fbx",
            "Assets/Animations/ReceivingUppercut.fbx"
        };

        foreach (var path in fbxPaths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            foreach (var clip in clips)
                clip.events = new AnimationEvent[0];

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"Cleared events from {path}");
        }
        AssetDatabase.Refresh();
        Debug.Log("Done removing all animation events");
    }
}
