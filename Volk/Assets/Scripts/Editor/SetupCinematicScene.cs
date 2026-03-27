using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using Cinemachine;
#endif

/// <summary>
/// PLA-119: Creates the Cinematic.unity scene with all 7 Cinemachine shots,
/// Timeline, post-processing, and CinematicDirector.
/// Menu: VOLK > Cinematic > Setup Cinematic Scene
/// </summary>
public class SetupCinematicScene
{
    [MenuItem("VOLK/Cinematic/Setup Cinematic Scene")]
    public static void Setup()
    {
        // 1. Create scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "Cinematic";

        // Get main camera
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        // 2. Add Cinemachine Brain to main camera
#if UNITY_EDITOR
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();
#endif

        // 3. Create 7 virtual cameras
        var vcams = new GameObject("VirtualCameras");

#if UNITY_EDITOR
        // Shot 1: Opening — low angle, arena ground, upward tilt
        var shot1 = CreateVCam("Shot1_Opening", vcams.transform);
        shot1.m_Lens.FieldOfView = 60f;
        shot1.transform.position = new Vector3(0, 0.3f, -5f);
        shot1.transform.rotation = Quaternion.Euler(-15f, 0, 0);

        // Shot 2: YILDIZ — orbital dolly
        var shot2 = CreateVCam("Shot2_YILDIZ", vcams.transform);
        shot2.m_Lens.FieldOfView = 40f;
        shot2.transform.position = new Vector3(3, 1.5f, -3f);
        // Orbital body would be set at runtime via Cinemachine Transposer

        // Shot 3: KAYA — low angle static close-up
        var shot3 = CreateVCam("Shot3_KAYA", vcams.transform);
        shot3.m_Lens.FieldOfView = 35f;
        shot3.transform.position = new Vector3(-2, 0.5f, -1.5f);
        shot3.transform.rotation = Quaternion.Euler(-10f, 15f, 0);

        // Shot 4: Fight — dynamic handheld
        var shot4 = CreateVCam("Shot4_Fight", vcams.transform);
        shot4.m_Lens.FieldOfView = 55f;
        shot4.transform.position = new Vector3(0, 1.8f, -4f);
        // Add noise for handheld feel
        var noise = shot4.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null)
            noise = shot4.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = 0.5f;
        noise.m_FrequencyGain = 0.3f;

        // Shot 5: Ghost — wide shot
        var shot5 = CreateVCam("Shot5_Ghost", vcams.transform);
        shot5.m_Lens.FieldOfView = 70f;
        shot5.transform.position = new Vector3(0, 3f, -8f);
        shot5.transform.rotation = Quaternion.Euler(10f, 0, 0);

        // Shot 6: Roster — 6 rapid-cut cameras (one parent with 6 children)
        var rosterParent = new GameObject("Shot6_Roster");
        rosterParent.transform.SetParent(vcams.transform);
        string[] chars = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };
        for (int i = 0; i < chars.Length; i++)
        {
            var rCam = CreateVCam($"Shot6_{chars[i]}", rosterParent.transform);
            rCam.m_Lens.FieldOfView = 35f;
            rCam.transform.position = new Vector3(-3f + i * 1.2f, 1.2f, -2f);
            rCam.Priority = 0; // Controlled by Timeline
        }

        // Shot 7: Logo — fade to black
        var shot7 = CreateVCam("Shot7_Logo", vcams.transform);
        shot7.m_Lens.FieldOfView = 50f;
        shot7.transform.position = new Vector3(0, 2f, -6f);
#endif

        // 4. Create Timeline + PlayableDirector
        string timelineDir = "Assets/Cinematic";
        if (!AssetDatabase.IsValidFolder(timelineDir))
            AssetDatabase.CreateFolder("Assets", "Cinematic");

        var timelineAsset = TimelineAsset.CreateInstance<TimelineAsset>();
        string timelinePath = $"{timelineDir}/VOLKTrailer.playable";
        AssetDatabase.CreateAsset(timelineAsset, timelinePath);

        // Create PlayableDirector
        var directorGO = new GameObject("CinematicDirector");
        var playableDir = directorGO.AddComponent<PlayableDirector>();
        playableDir.playableAsset = timelineAsset;
        playableDir.playOnAwake = false;

        // Add CinematicDirector script
        directorGO.AddComponent<Volk.Cinematic.CinematicDirector>();

#if UNITY_EDITOR
        // Add Cinemachine tracks for each shot
        AddCinemachineTrack(timelineAsset, "Shot1_Opening", 0, 5);
        AddCinemachineTrack(timelineAsset, "Shot2_YILDIZ", 5, 5);
        AddCinemachineTrack(timelineAsset, "Shot3_KAYA", 10, 5);
        AddCinemachineTrack(timelineAsset, "Shot4_Fight", 15, 20);
        AddCinemachineTrack(timelineAsset, "Shot5_Ghost", 35, 10);
        AddCinemachineTrack(timelineAsset, "Shot6_Roster", 45, 7);
        AddCinemachineTrack(timelineAsset, "Shot7_Logo", 52, 8);
#endif

        // 7. Post-process Volume
        var ppGO = new GameObject("PostProcessVolume");
        var volume = ppGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, $"{timelineDir}/CinematicPostProcess.asset");

        var bloom = profile.Add<Bloom>();
        bloom.intensity.value = 1.5f;
        bloom.intensity.overrideState = true;
        bloom.threshold.value = 0.9f;
        bloom.threshold.overrideState = true;

        var vignette = profile.Add<Vignette>();
        vignette.intensity.value = 0.4f;
        vignette.intensity.overrideState = true;

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.saturation.value = 10f;
        colorAdj.saturation.overrideState = true;
        colorAdj.contrast.value = 15f;
        colorAdj.contrast.overrideState = true;

        // Teal-orange color grading via split toning
        var splitTone = profile.Add<SplitToning>();
        splitTone.shadows.value = new Color(0.0f, 0.4f, 0.5f); // Teal shadows
        splitTone.shadows.overrideState = true;
        splitTone.highlights.value = new Color(1.0f, 0.6f, 0.3f); // Orange highlights
        splitTone.highlights.overrideState = true;
        splitTone.balance.value = -20f;
        splitTone.balance.overrideState = true;

        volume.profile = profile;

        // 5. Spawn character prefabs (YILDIZ + KAYA for key shots)
        SpawnCharacterPrefab("YILDIZ", new Vector3(1, 0, 0));
        SpawnCharacterPrefab("KAYA", new Vector3(-1, 0, 0));

        // Save scene
        string scenePath = "Assets/Scenes/Cinematic.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Add to Build Settings
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes) if (s.path == scenePath) { found = true; break; }
        if (!found)
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Cinematic] Scene setup complete! Open Assets/Scenes/Cinematic.unity and run VOLK > Cinematic > Setup Cinematic Scene");
    }

#if UNITY_EDITOR
    static CinemachineVirtualCamera CreateVCam(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 0; // Controlled by Timeline, not priority
        return vcam;
    }

    static void AddCinemachineTrack(TimelineAsset timeline, string shotName, float startSec, float durationSec)
    {
        var track = timeline.CreateTrack<CinemachineTrack>(null, shotName);
        var clip = track.CreateClip<CinemachineShot>();
        clip.start = startSec;
        clip.duration = durationSec;
        clip.displayName = shotName;
    }
#endif

    static void SpawnCharacterPrefab(string charName, Vector3 position)
    {
        string prefabPath = $"Assets/Prefabs/Characters/{charName}_Fighter.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"{charName}_Cinematic";
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0, charName == "YILDIZ" ? -30f : 30f, 0);

            // Disable AI and input so they just stand in the scene
            var fighter = instance.GetComponent<Fighter>();
            if (fighter != null)
            {
                fighter.isAI = false;
                fighter.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"[Cinematic] Prefab not found: {prefabPath}");
        }
    }
}
