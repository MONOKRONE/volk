using UnityEngine;
using UnityEditor;
using Volk.Core;
#if UNITY_EDITOR
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Creates 3 ProBuilder-style arenas with URP post-processing volumes.
/// Run: VOLK > Create 3 ProBuilder Arenas
/// Note: Uses primitive cubes since ProBuilder API may not be available.
/// If ProBuilder is installed, you can convert these to ProBuilder meshes.
/// </summary>
public class CreateProBuilderArenas
{
    [MenuItem("VOLK/Create 3 ProBuilder Arenas")]
    public static void CreateAll()
    {
        string prefabDir = "Assets/Prefabs/Arenas";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Arenas");

        string arenaDir = "Assets/ScriptableObjects/Arenas";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Arenas"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Arenas");
        }

        CreateArena1_Night(prefabDir, arenaDir);
        CreateArena2_Day(prefabDir, arenaDir);
        CreateArena3_Industrial(prefabDir, arenaDir);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VOLK] 3 ProBuilder arenas created!");
    }

    static void CreateArena1_Night(string prefabDir, string arenaDir)
    {
        var root = BuildArenaGeometry("Arena1_Night");
        SetupPostProcessVolume(root, bloomIntensity: 0.7f, bloomThreshold: 0.9f, saturation: 15f, contrast: 10f, vignetteIntensity: 0.3f);
        SetupLighting(root,
            ambientColor: HexColor("#1a1a4e"),
            mainLightColor: new Color(0.4f, 0.3f, 0.8f),
            mainLightIntensity: 0.5f,
            accentColor: new Color(0.2f, 0.5f, 1f),
            accentIntensity: 2f);

        PrefabUtility.SaveAsPrefabAsset(root, $"{prefabDir}/Arena1_Night.prefab");
        Object.DestroyImmediate(root);

        // ArenaData SO
        var data = ScriptableObject.CreateInstance<ArenaData>();
        data.arenaName = "Gece Arenasi";
        data.description = "Neon mavi isikli gece arenasi";
        data.ambientColor = HexColor("#1a1a4e");
        data.mainLightColor = new Color(0.4f, 0.3f, 0.8f);
        data.mainLightIntensity = 0.5f;
        data.accentLightColors = new[] { new Color(0.2f, 0.5f, 1f), new Color(0.2f, 0.5f, 1f) };
        data.accentLightIntensity = 2f;
        data.bloomIntensity = 0.7f;
        data.bloomThreshold = 0.9f;
        data.vignetteIntensity = 0.3f;
        data.saturation = 15f;
        data.contrast = 10f;
        data.floorColor = new Color(0.1f, 0.1f, 0.2f);
        data.wallColor = new Color(0.08f, 0.08f, 0.15f);
        data.particleType = ParticleType.Stars;
        data.particleColor = new Color(0.3f, 0.5f, 1f, 0.4f);
        data.particleRate = 20f;
        AssetDatabase.DeleteAsset($"{arenaDir}/Arena_Night.asset");
        AssetDatabase.CreateAsset(data, $"{arenaDir}/Arena_Night.asset");
    }

    static void CreateArena2_Day(string prefabDir, string arenaDir)
    {
        var root = BuildArenaGeometry("Arena2_Day");
        SetupPostProcessVolume(root, bloomIntensity: 0.7f, bloomThreshold: 0.9f, saturation: 15f, contrast: 10f, vignetteIntensity: 0.3f);
        SetupLighting(root,
            ambientColor: HexColor("#4e1a1a"),
            mainLightColor: new Color(1f, 0.7f, 0.4f),
            mainLightIntensity: 1.2f,
            accentColor: new Color(1f, 0.4f, 0.2f),
            accentIntensity: 1.5f);

        PrefabUtility.SaveAsPrefabAsset(root, $"{prefabDir}/Arena2_Day.prefab");
        Object.DestroyImmediate(root);

        var data = ScriptableObject.CreateInstance<ArenaData>();
        data.arenaName = "Gunduz Arenasi";
        data.description = "Kirmizi endustriyel gunduz arenasi";
        data.ambientColor = HexColor("#4e1a1a");
        data.mainLightColor = new Color(1f, 0.7f, 0.4f);
        data.mainLightIntensity = 1.2f;
        data.accentLightColors = new[] { new Color(1f, 0.4f, 0.2f), new Color(1f, 0.4f, 0.2f) };
        data.accentLightIntensity = 1.5f;
        data.bloomIntensity = 0.7f;
        data.bloomThreshold = 0.9f;
        data.vignetteIntensity = 0.3f;
        data.saturation = 15f;
        data.contrast = 10f;
        data.floorColor = new Color(0.3f, 0.15f, 0.1f);
        data.wallColor = new Color(0.25f, 0.12f, 0.08f);
        data.particleType = ParticleType.Dust;
        data.particleColor = new Color(0.8f, 0.5f, 0.3f, 0.2f);
        data.particleRate = 12f;
        AssetDatabase.DeleteAsset($"{arenaDir}/Arena_Day.asset");
        AssetDatabase.CreateAsset(data, $"{arenaDir}/Arena_Day.asset");
    }

    static void CreateArena3_Industrial(string prefabDir, string arenaDir)
    {
        var root = BuildArenaGeometry("Arena3_Industrial");
        SetupPostProcessVolume(root, bloomIntensity: 0.7f, bloomThreshold: 0.9f, saturation: 15f, contrast: 10f, vignetteIntensity: 0.3f);
        SetupLighting(root,
            ambientColor: HexColor("#3a3a1a"),
            mainLightColor: new Color(1f, 0.95f, 0.5f),
            mainLightIntensity: 1.0f,
            accentColor: new Color(1f, 0.9f, 0.3f),
            accentIntensity: 2f);

        PrefabUtility.SaveAsPrefabAsset(root, $"{prefabDir}/Arena3_Industrial.prefab");
        Object.DestroyImmediate(root);

        var data = ScriptableObject.CreateInstance<ArenaData>();
        data.arenaName = "Endustriyel Arena";
        data.description = "Sert sari isikli endustriyel arena";
        data.ambientColor = HexColor("#3a3a1a");
        data.mainLightColor = new Color(1f, 0.95f, 0.5f);
        data.mainLightIntensity = 1.0f;
        data.accentLightColors = new[] { new Color(1f, 0.9f, 0.3f), new Color(1f, 0.9f, 0.3f) };
        data.accentLightIntensity = 2f;
        data.bloomIntensity = 0.7f;
        data.bloomThreshold = 0.9f;
        data.vignetteIntensity = 0.3f;
        data.saturation = 15f;
        data.contrast = 10f;
        data.floorColor = new Color(0.25f, 0.22f, 0.1f);
        data.wallColor = new Color(0.2f, 0.18f, 0.08f);
        data.particleType = ParticleType.Sparks;
        data.particleColor = new Color(1f, 0.8f, 0.3f, 0.6f);
        data.particleRate = 8f;
        AssetDatabase.DeleteAsset($"{arenaDir}/Arena_Industrial.asset");
        AssetDatabase.CreateAsset(data, $"{arenaDir}/Arena_Industrial.asset");
    }

    static GameObject BuildArenaGeometry(string name)
    {
        var root = new GameObject(name);

        // Floor: 20x12
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(20f, 0.1f, 12f);

        // Walls: 3m height
        float wallHeight = 3f;
        CreateWall(root.transform, "Wall_North", new Vector3(0f, wallHeight / 2f, 6f), new Vector3(20f, wallHeight, 0.2f));
        CreateWall(root.transform, "Wall_South", new Vector3(0f, wallHeight / 2f, -6f), new Vector3(20f, wallHeight, 0.2f));
        CreateWall(root.transform, "Wall_East", new Vector3(10f, wallHeight / 2f, 0f), new Vector3(0.2f, wallHeight, 12f));
        CreateWall(root.transform, "Wall_West", new Vector3(-10f, wallHeight / 2f, 0f), new Vector3(0.2f, wallHeight, 12f));

        return root;
    }

    static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = pos;
        wall.transform.localScale = scale;
    }

    static void SetupPostProcessVolume(GameObject root, float bloomIntensity, float bloomThreshold, float saturation, float contrast, float vignetteIntensity)
    {
        var volumeGo = new GameObject("PostProcessVolume");
        volumeGo.transform.SetParent(root.transform);
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>();
        bloom.intensity.overrideState = true;
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = bloomThreshold;

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.saturation.overrideState = true;
        colorAdj.saturation.value = saturation;
        colorAdj.contrast.overrideState = true;
        colorAdj.contrast.value = contrast;

        var vignette = profile.Add<Vignette>();
        vignette.intensity.overrideState = true;
        vignette.intensity.value = vignetteIntensity;

        volume.profile = profile;
    }

    static void SetupLighting(GameObject root, Color ambientColor, Color mainLightColor, float mainLightIntensity, Color accentColor, float accentIntensity)
    {
        // Directional light
        var mainLightGo = new GameObject("MainLight");
        mainLightGo.transform.SetParent(root.transform);
        mainLightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var mainLight = mainLightGo.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        mainLight.color = mainLightColor;
        mainLight.intensity = mainLightIntensity;

        // Accent point lights (corners)
        Vector3[] accentPositions = new[]
        {
            new Vector3(-8f, 2.5f, -4f),
            new Vector3(8f, 2.5f, 4f),
        };

        for (int i = 0; i < accentPositions.Length; i++)
        {
            var lightGo = new GameObject($"AccentLight_{i}");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.localPosition = accentPositions[i];
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = accentColor;
            light.intensity = accentIntensity;
            light.range = 10f;
        }
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
