using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateFighterPrefabs
{
    // Character → Model mapping
    static readonly (string charName, string modelPath, string modelChildName)[] CharacterModels = new[]
    {
        ("YILDIZ",  "Assets/Characters/Kachujin/Kachujin.fbx", "Kachujin"),
        ("KAYA",    "Assets/Characters/XBot/XBot.fbx",         "XBot"),
        ("RUZGAR",  "Assets/Characters/YBot/YBot.fbx",         "YBot"),
        ("CELIK",   "Assets/Characters/Remy/Remy.fbx",         "Remy"),
        ("SIS",     "Assets/Characters/Maria/Maria.fbx",       "Maria"),
        ("TOPRAK",  "Assets/Characters/Remy/Remy.fbx",         "Remy"),  // Remy variant
    };

    [MenuItem("VOLK/Create 6 Fighter Prefabs")]
    public static void CreateAll()
    {
        string prefabDir = "Assets/Prefabs/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Characters");

        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/PlayerAnimator.controller");

        int created = 0;

        foreach (var (charName, modelPath, modelChildName) in CharacterModels)
        {
            string prefabPath = $"{prefabDir}/{charName}_Fighter.prefab";

            // Load model
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[VOLK] Model not found: {modelPath}, skipping {charName}");
                continue;
            }

            // Create root GameObject
            var root = new GameObject($"{charName}_Fighter");

            // Add CharacterController
            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.3f;

            // Instantiate model as child
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            modelInstance.name = modelChildName;
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            // Setup Animator on model child
            var animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
                animator = modelInstance.AddComponent<Animator>();
            if (animController != null)
                animator.runtimeAnimatorController = animController;
            animator.applyRootMotion = false;

            // Add Fighter component
            var fighter = root.AddComponent<Fighter>();
            fighter.isAI = false;
            fighter.enemyTag = "Enemy";

            // Setup attack points from humanoid rig
            if (animator.isHuman)
            {
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null)
                {
                    var rhPoint = new GameObject("RightHandPoint");
                    rhPoint.transform.SetParent(rightHand, false);
                    rhPoint.transform.localPosition = Vector3.zero;
                    fighter.rightHandPoint = rhPoint.transform;
                }

                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (rightFoot != null)
                {
                    var rfPoint = new GameObject("RightFootPoint");
                    rfPoint.transform.SetParent(rightFoot, false);
                    rfPoint.transform.localPosition = Vector3.zero;
                    fighter.rightFootPoint = rfPoint.transform;
                }
            }

            // Link CharacterData if it exists
            string charDataPath = $"Assets/ScriptableObjects/Characters/{charName}.asset";
            var charData = AssetDatabase.LoadAssetAtPath<CharacterData>(charDataPath);
            if (charData != null)
            {
                fighter.characterData = charData;
            }

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            // Link prefab back to CharacterData
            if (charData != null)
            {
                charData.prefab = prefab;
                charData.animController = animController;
                EditorUtility.SetDirty(charData);
            }

            created++;
            Debug.Log($"[VOLK] Created prefab: {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VOLK] {created} Fighter prefabs created in {prefabDir}!");
    }
}
