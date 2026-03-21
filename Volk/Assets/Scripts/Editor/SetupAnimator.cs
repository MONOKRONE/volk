using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SetupAnimator
{
    [MenuItem("Tools/Setup Animator Controller")]
    public static void Setup()
    {
        // Step 1: Set Maria.fbx rig to Humanoid
        SetRigToHumanoid("Assets/Characters/Maria.fbx");
        SetRigToHumanoid("Assets/Characters/Kachujin.fbx");

        // Step 2: Set all animation FBX rigs to Humanoid
        string[] animFiles = Directory.GetFiles("Assets/Animations", "*.fbx");
        foreach (string file in animFiles)
        {
            SetRigToHumanoid(file.Replace("\\", "/"));
        }

        // Step 3: Create Animator Controller
        string controllerPath = "Assets/Animations/PlayerAnimator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        // Animation clip names in order, first one is default
        string[] clipNames = { "Idle", "Walk", "Run", "HookPunch", "MMAKick", "BodyBlock", "TakingPunch", "ReceivingUppercut", "Death", "Jump" };

        AnimatorState defaultState = null;
        foreach (string clipName in clipNames)
        {
            string clipPath = "Assets/Animations/" + clipName + ".fbx";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                // FBX files contain clips as sub-assets
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(clipPath);
                foreach (Object asset in assets)
                {
                    if (asset is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                    {
                        clip = ac;
                        break;
                    }
                }
            }

            if (clip != null)
            {
                var state = rootStateMachine.AddState(clipName);
                state.motion = clip;
                if (defaultState == null)
                {
                    defaultState = state;
                    rootStateMachine.defaultState = state;
                }
            }
            else
            {
                Debug.LogWarning("Could not find animation clip at: " + clipPath);
            }
        }

        AssetDatabase.SaveAssets();

        // Step 4: Assign controller to Player_Maria
        GameObject playerMaria = GameObject.Find("Player_Maria");
        if (playerMaria != null)
        {
            Animator animator = playerMaria.GetComponent<Animator>();
            if (animator == null)
                animator = playerMaria.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(playerMaria);
            Debug.Log("Animator Controller assigned to Player_Maria");
        }
        else
        {
            Debug.LogError("Player_Maria not found in scene!");
        }

        AssetDatabase.Refresh();
        Debug.Log("Setup complete!");
    }

    static void SetRigToHumanoid(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
            Debug.Log("Set Humanoid rig: " + path);
        }
    }
}
