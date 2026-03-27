using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PLA-124: Visual sprint — character colors, VFX, arena, animations.
/// Menu: VOLK > Visual Sprint > Apply All
/// </summary>
public class SetupVisualSprint
{
    static readonly (string name, Color color)[] CharDefs = new[]
    {
        ("YILDIZ", new Color(1f, 0.55f, 0f)),
        ("KAYA",   new Color(0.29f, 0.29f, 0.29f)),
        ("RUZGAR", new Color(0f, 0.4f, 1f)),
        ("CELIK",  new Color(0.75f, 0.75f, 0.75f)),
        ("SIS",    new Color(0.55f, 0f, 1f)),
        ("TOPRAK", new Color(0.55f, 0.27f, 0.07f)),
    };

    [MenuItem("VOLK/Visual Sprint/Apply All")]
    public static void ApplyAll()
    {
        ApplyCharacterMaterials();
        WireHitEffects();
        WireCharacterVFX();
        CheckAnimatorControllers();
        Debug.Log("[VisualSprint] All prefab modifications complete. Now run 'Setup CombatTest Scene' to update the scene.");
    }

    [MenuItem("VOLK/Visual Sprint/1. Apply Character Materials")]
    public static void ApplyCharacterMaterials()
    {
        var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Characters"))
            AssetDatabase.CreateFolder("Assets/Materials", "Characters");

        int count = 0;
        foreach (var (name, color) in CharDefs)
        {
            string prefabPath = $"Assets/Prefabs/Characters/{name}_Fighter.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[VisualSprint] Prefab not found: {prefabPath}");
                continue;
            }

            // Create or update material
            string matPath = $"Assets/Materials/Characters/{name}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLitShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.color = color;
            mat.SetColor("_BaseColor", color);

            if (name == "YILDIZ")
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.3f);
            }

            EditorUtility.SetDirty(mat);

            // Apply to prefab
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (var renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                renderer.sharedMaterials = mats;
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[VisualSprint] Character materials applied to {count} prefabs");
    }

    [MenuItem("VOLK/Visual Sprint/2. Wire Hit Effects")]
    public static void WireHitEffects()
    {
        // Find HitEffectManager on any prefab or create configuration
        // Load VFX prefabs
        var basicHit = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Matthew Guz/Hits Effects FREE/Prefab/Basic Hit .prefab");
        var fireHit = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Matthew Guz/Hits Effects FREE/Prefab/Fire Hit .prefab");
        var lightningHit = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Matthew Guz/Hits Effects FREE/Prefab/Lightning Hit Blue.prefab");
        var shadowHit = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Matthew Guz/Hits Effects FREE/Prefab/1.2/Shadow Hit (NEW).prefab");
        var hit01 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Travis Game Assets/Hit Impact Effects/Prefabs/Hits/Hit_01.prefab");
        var hit02 = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Travis Game Assets/Hit Impact Effects/Prefabs/Hits/Hit_02.prefab");

        // Wire to each fighter prefab's HitEffectManager if it has one,
        // or check if there's a scene-level HitEffectManager
        // For now, ensure GameManager or a root object has HitEffectManager with correct refs
        // We'll create a prefab-level configuration asset

        // Check each fighter prefab for HitEffectManager component
        foreach (var (name, _) in CharDefs)
        {
            string prefabPath = $"Assets/Prefabs/Characters/{name}_Fighter.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var hitMgr = prefabRoot.GetComponentInChildren<HitEffectManager>(true);
            if (hitMgr != null)
            {
                if (basicHit != null) { hitMgr.punchHitPrefab = basicHit; hitMgr.lightHitPrefab = basicHit; }
                if (fireHit != null) { hitMgr.kickHitPrefab = fireHit; hitMgr.mediumHitPrefab = fireHit; }
                if (lightningHit != null) hitMgr.heavyHitPrefab = lightningHit;
                if (shadowHit != null) hitMgr.skillHitPrefab = shadowHit;
                if (hit01 != null && hitMgr.blockHitPrefab == null) hitMgr.blockHitPrefab = hit01;
                Debug.Log($"[VisualSprint] Wired HitEffectManager on {name}");
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log("[VisualSprint] Hit effects wiring complete");
    }

    [MenuItem("VOLK/Visual Sprint/3. Wire Character VFX")]
    public static void WireCharacterVFX()
    {
        // VFX assignments per character skill
        var trailFire = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Fire.prefab");
        var trailElectric = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Electric.prefab");
        var trailIce = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Vefects/Trails VFX URP/VFX/Particles/VFX_Trail_Ice.prefab");
        var shockwave = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Vefects/Easy Shockwaves VFX URP/VFX/Shockwaves/Particles/VFX_Shockwave_01_White_1s.prefab");
        var impactFrame = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Vefects/Easy Impact Frames/VFX/Impact Frames/Particles/VFX_Impact_Frame_01.prefab");

        // Map: character -> (trailPrefab for movement VFX)
        // RUZGAR and SIS get trail VFX as child objects
        WireTrailToCharacter("RUZGAR", trailElectric);
        WireTrailToCharacter("SIS", trailIce);

        Debug.Log("[VisualSprint] Character VFX wiring complete");
    }

    static void WireTrailToCharacter(string charName, GameObject trailPrefab)
    {
        if (trailPrefab == null) return;

        string prefabPath = $"Assets/Prefabs/Characters/{charName}_Fighter.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Check if trail already added
        var existingTrail = prefabRoot.transform.Find("TrailVFX");
        if (existingTrail == null)
        {
            var trailInstance = (GameObject)PrefabUtility.InstantiatePrefab(trailPrefab);
            trailInstance.name = "TrailVFX";
            trailInstance.transform.SetParent(prefabRoot.transform);
            trailInstance.transform.localPosition = new Vector3(0, 0.5f, -0.3f);
            trailInstance.SetActive(false); // Activated by movement scripts at runtime
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log($"[VisualSprint] Trail VFX added to {charName}");
    }

    [MenuItem("VOLK/Visual Sprint/4. Check Animator Controllers")]
    public static void CheckAnimatorControllers()
    {
        var playerAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/PlayerAnimator.controller");

        if (playerAnimController == null)
        {
            Debug.LogWarning("[VisualSprint] PlayerAnimator.controller not found at Assets/Animations/");
            return;
        }

        int fixedCount = 0;
        foreach (var (name, _) in CharDefs)
        {
            string prefabPath = $"Assets/Prefabs/Characters/{name}_Fighter.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            var animator = prefabRoot.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = playerAnimController;
                fixedCount++;
                Debug.Log($"[VisualSprint] Assigned PlayerAnimator to {name}");
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log($"[VisualSprint] Animator check complete. Fixed: {fixedCount}");
    }

    [MenuItem("VOLK/Visual Sprint/5. Setup CombatTest Scene")]
    public static void SetupCombatTestScene()
    {
        // Open CombatTest scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity", OpenSceneMode.Single);

        // Check if arena exists
        var arenaRoot = GameObject.Find("MMA_Arena") ?? GameObject.Find("Arena");
        if (arenaRoot == null)
        {
            arenaRoot = new GameObject("MMA_Arena");

            var octFloor = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MarpaStudio/Mesh/OctagonFloor.fbx");
            var octCage = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MarpaStudio/Mesh/OctagonCage.fbx");

            if (octFloor != null)
            {
                var floor = (GameObject)PrefabUtility.InstantiatePrefab(octFloor);
                floor.transform.SetParent(arenaRoot.transform);
                floor.transform.position = new Vector3(0, 0, 0);
            }
            if (octCage != null)
            {
                var cage = (GameObject)PrefabUtility.InstantiatePrefab(octCage);
                cage.transform.SetParent(arenaRoot.transform);
                cage.transform.position = new Vector3(0, 0, 0);
            }

            Debug.Log("[VisualSprint] Arena added to CombatTest");
        }
        else
        {
            Debug.Log("[VisualSprint] Arena already exists in CombatTest");
        }

        // Ensure HitEffectManager exists in scene
        var hitMgr = Object.FindObjectOfType<HitEffectManager>();
        if (hitMgr == null)
        {
            var hitMgrGO = new GameObject("HitEffectManager");
            hitMgr = hitMgrGO.AddComponent<HitEffectManager>();

            var basicHit = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Matthew Guz/Hits Effects FREE/Prefab/Basic Hit .prefab");
            var fireHit = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Matthew Guz/Hits Effects FREE/Prefab/Fire Hit .prefab");
            var lightningHit = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Matthew Guz/Hits Effects FREE/Prefab/Lightning Hit Blue.prefab");
            var shadowHit = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Matthew Guz/Hits Effects FREE/Prefab/1.2/Shadow Hit (NEW).prefab");

            if (basicHit != null) { hitMgr.punchHitPrefab = basicHit; hitMgr.lightHitPrefab = basicHit; }
            if (fireHit != null) { hitMgr.kickHitPrefab = fireHit; hitMgr.mediumHitPrefab = fireHit; }
            if (lightningHit != null) hitMgr.heavyHitPrefab = lightningHit;
            if (shadowHit != null) hitMgr.skillHitPrefab = shadowHit;

            Debug.Log("[VisualSprint] HitEffectManager created in CombatTest");
        }

        // Ensure JuiceManager exists in scene
        var juiceMgr = Object.FindObjectOfType<JuiceManager>();
        if (juiceMgr == null)
        {
            var juiceMgrGO = new GameObject("JuiceManager");
            juiceMgrGO.AddComponent<JuiceManager>();
            Debug.Log("[VisualSprint] JuiceManager created in CombatTest");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[VisualSprint] CombatTest scene setup complete");
    }
}
