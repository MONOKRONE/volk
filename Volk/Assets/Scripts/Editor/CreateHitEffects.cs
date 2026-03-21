using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CreateHitEffects
{
    [MenuItem("Tools/Create Hit Effect Prefabs And Setup")]
    public static void Create()
    {
        // Ensure VFX folder exists
        if (!AssetDatabase.IsValidFolder("Assets/VFX"))
            AssetDatabase.CreateFolder("Assets", "VFX");

        // Create punch hit prefab
        var punchPrefab = CreateParticlePrefab("PunchHitFX",
            new Color(1f, 0.6f, 0.1f), // orange
            new Color(1f, 0.2f, 0f),    // red-orange
            20, 0.15f, 0.8f, 0.3f);

        // Create kick hit prefab
        var kickPrefab = CreateParticlePrefab("KickHitFX",
            new Color(0.3f, 0.6f, 1f),  // blue
            new Color(0.1f, 0.3f, 1f),  // deep blue
            25, 0.2f, 1.0f, 0.4f);

        // Create block hit prefab
        var blockPrefab = CreateParticlePrefab("BlockHitFX",
            new Color(1f, 1f, 1f),      // white
            new Color(0.7f, 0.7f, 1f),  // light blue
            10, 0.1f, 0.5f, 0.2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Load scene and setup
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        // Find or create HitEffectManager
        var existing = GameObject.Find("HitEffectManager");
        if (existing != null) Object.DestroyImmediate(existing);

        var managerGO = new GameObject("HitEffectManager");
        var manager = managerGO.AddComponent<HitEffectManager>();
        manager.punchHitPrefab = punchPrefab;
        manager.kickHitPrefab = kickPrefab;
        manager.blockHitPrefab = blockPrefab;
        EditorUtility.SetDirty(managerGO);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Hit effect prefabs created and HitEffectManager setup!");
        Debug.Log($"  PunchHitFX: {AssetDatabase.GetAssetPath(punchPrefab)}");
        Debug.Log($"  KickHitFX: {AssetDatabase.GetAssetPath(kickPrefab)}");
        Debug.Log($"  BlockHitFX: {AssetDatabase.GetAssetPath(blockPrefab)}");
    }

    static GameObject CreateParticlePrefab(string name, Color startColor, Color endColor,
        int maxParticles, float startSize, float startSpeed, float lifetime)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();

        // Main module
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
        main.playOnAwake = true;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // Emission
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, maxParticles)
        });

        // Shape - sphere
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Size over lifetime - shrink
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        // Color over lifetime - fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }
        );
        col.color = gradient;

        // Use default particle material
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = startColor;

        // Save as prefab
        string path = $"Assets/VFX/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        Debug.Log($"Created prefab: {path}");
        return prefab;
    }
}
