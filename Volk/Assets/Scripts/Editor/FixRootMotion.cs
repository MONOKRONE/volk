using UnityEngine;
using UnityEditor;
using System.IO;

public class FixRootMotion
{
    [MenuItem("Tools/Fix Root Motion On All Animations")]
    public static void Fix()
    {
        string[] animFiles = Directory.GetFiles("Assets/Animations", "*.fbx");

        foreach (string file in animFiles)
        {
            string path = file.Replace("\\", "/");
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            // Rig tab: ensure Humanoid
            importer.animationType = ModelImporterAnimationType.Human;

            // Clear motion node
            importer.motionNodeName = "";

            // Get clip animations to modify
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                clips = importer.clipAnimations;

            string fileName = Path.GetFileNameWithoutExtension(path);
            bool isLooping = fileName == "Walk" || fileName == "Run" || fileName == "Idle";

            for (int i = 0; i < clips.Length; i++)
            {
                // Disable root motion baking
                clips[i].lockRootRotation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;

                clips[i].keepOriginalOrientation = false;
                clips[i].keepOriginalPositionY = false;
                clips[i].keepOriginalPositionXZ = false;

                // Loop settings for Walk, Run, Idle
                if (isLooping)
                {
                    clips[i].loopTime = true;
                    clips[i].loopPose = true;
                }

                Debug.Log($"Configured clip '{clips[i].name}' in {fileName}.fbx (loop={isLooping})");
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log($"Reimported: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Root motion fix complete!");
    }
}
