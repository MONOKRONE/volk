using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SetupGlobalPostProcess
{
    [MenuItem("VOLK/Setup Combat Post-Processing")]
    public static void SetupCombat()
    {
        string profileDir = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(profileDir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        // Create combat volume profile
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.7f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.9f;

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.saturation.overrideState = true;
        colorAdj.saturation.value = 15f;
        colorAdj.contrast.overrideState = true;
        colorAdj.contrast.value = 10f;

        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.3f;

        var chromatic = profile.Add<ChromaticAberration>();
        chromatic.active = true;
        chromatic.intensity.overrideState = true;
        chromatic.intensity.value = 0f; // Off by default, animated on KO

        AssetDatabase.DeleteAsset($"{profileDir}/CombatVolumeProfile.asset");
        AssetDatabase.CreateAsset(profile, $"{profileDir}/CombatVolumeProfile.asset");

        // Add to scene
        SetupVolumeInScene("CombatGlobalVolume", profile);

        Debug.Log("[VOLK] Combat post-processing profile created and applied!");
    }

    [MenuItem("VOLK/Setup MainMenu Post-Processing")]
    public static void SetupMainMenu()
    {
        string profileDir = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(profileDir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.4f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.1f;

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.saturation.overrideState = true;
        colorAdj.saturation.value = 10f;
        colorAdj.contrast.overrideState = true;
        colorAdj.contrast.value = 5f;
        colorAdj.colorFilter.overrideState = true;
        colorAdj.colorFilter.value = new Color(1f, 0.95f, 0.9f); // Warm tint

        var vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.2f;

        AssetDatabase.DeleteAsset($"{profileDir}/MainMenuVolumeProfile.asset");
        AssetDatabase.CreateAsset(profile, $"{profileDir}/MainMenuVolumeProfile.asset");

        SetupVolumeInScene("MainMenuGlobalVolume", profile);

        Debug.Log("[VOLK] MainMenu post-processing profile created and applied!");
    }

    static void SetupVolumeInScene(string name, VolumeProfile profile)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var go = new GameObject(name);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.profile = profile;

        // Add PostProcessAnimator for combat
        if (name.Contains("Combat"))
        {
            var animator = go.AddComponent<PostProcessAnimator>();
            animator.globalVolume = volume;
        }

        EditorUtility.SetDirty(go);
    }
}
