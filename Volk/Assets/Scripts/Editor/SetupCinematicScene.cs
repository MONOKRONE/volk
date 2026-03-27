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
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// PLA-119/PLA-120: Creates the Cinematic.unity scene with all 7 Cinemachine shots,
/// Timeline, character materials, VFX signals, letterbox, post-processing, and recorder.
/// Menu: VOLK > Cinematic > Setup Cinematic Scene
/// </summary>
public class SetupCinematicScene
{
    static readonly string TimelineDir = "Assets/Cinematic";
    static readonly string[] AllChars = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };

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

        // 2. Add Cinemachine Brain + Letterbox to main camera
#if UNITY_EDITOR
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            mainCam.gameObject.AddComponent<CinemachineBrain>();
#endif
        mainCam.gameObject.AddComponent<Volk.Cinematic.CinematicLetterbox>();

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

        // Shot 3: KAYA — low angle static close-up, slow dolly in
        var shot3 = CreateVCam("Shot3_KAYA", vcams.transform);
        shot3.m_Lens.FieldOfView = 35f;
        shot3.transform.position = new Vector3(-2, 0.5f, -1.5f);
        shot3.transform.rotation = Quaternion.Euler(-10f, 15f, 0);

        // Shot 4: Fight — dynamic handheld
        var shot4 = CreateVCam("Shot4_Fight", vcams.transform);
        shot4.m_Lens.FieldOfView = 55f;
        shot4.transform.position = new Vector3(0, 1.8f, -4f);
        var noise = shot4.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null)
            noise = shot4.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_AmplitudeGain = 0.5f;
        noise.m_FrequencyGain = 0.3f;

        // Shot 5: Ghost — wide shot, SIS ghost effect
        var shot5 = CreateVCam("Shot5_Ghost", vcams.transform);
        shot5.m_Lens.FieldOfView = 70f;
        shot5.transform.position = new Vector3(0, 3f, -8f);
        shot5.transform.rotation = Quaternion.Euler(10f, 0, 0);

        // Shot 6: Roster — 6 rapid-cut cameras (1s each)
        var rosterParent = new GameObject("Shot6_Roster");
        rosterParent.transform.SetParent(vcams.transform);
        for (int i = 0; i < AllChars.Length; i++)
        {
            var rCam = CreateVCam($"Shot6_{AllChars[i]}", rosterParent.transform);
            rCam.m_Lens.FieldOfView = 35f;
            rCam.transform.position = new Vector3(-3f + i * 1.2f, 1.2f, -2f);
            rCam.Priority = 0;
        }

        // Shot 7: Logo — fade to black
        var shot7 = CreateVCam("Shot7_Logo", vcams.transform);
        shot7.m_Lens.FieldOfView = 50f;
        shot7.transform.position = new Vector3(0, 2f, -6f);
#endif

        // 4. Create Timeline + PlayableDirector
        if (!AssetDatabase.IsValidFolder(TimelineDir))
            AssetDatabase.CreateFolder("Assets", "Cinematic");

        var timelineAsset = TimelineAsset.CreateInstance<TimelineAsset>();
        string timelinePath = $"{TimelineDir}/VOLKTrailer.playable";
        AssetDatabase.CreateAsset(timelineAsset, timelinePath);

        // Create PlayableDirector
        var directorGO = new GameObject("CinematicDirector");
        var playableDir = directorGO.AddComponent<PlayableDirector>();
        playableDir.playableAsset = timelineAsset;
        playableDir.playOnAwake = false;

        // Add CinematicDirector + VFX receiver
        directorGO.AddComponent<Volk.Cinematic.CinematicDirector>();
        directorGO.AddComponent<Volk.Cinematic.CinematicVFXReceiver>();

#if UNITY_EDITOR
        // Cinemachine tracks for each shot
        AddCinemachineTrack(timelineAsset, "Shot1_Opening", 0, 5);
        AddCinemachineTrack(timelineAsset, "Shot2_YILDIZ", 5, 5);
        AddCinemachineTrack(timelineAsset, "Shot3_KAYA", 10, 5);
        AddCinemachineTrack(timelineAsset, "Shot4_Fight", 15, 20);
        AddCinemachineTrack(timelineAsset, "Shot5_Ghost", 35, 10);
        // Shot6: 6 sub-cameras, ~1.17s each over 7s total
        for (int i = 0; i < AllChars.Length; i++)
        {
            AddCinemachineTrack(timelineAsset, $"Shot6_{AllChars[i]}", 45 + i * 1.167f, 1.167f);
        }
        AddCinemachineTrack(timelineAsset, "Shot7_Logo", 52, 8);
#endif

        // Animator Tracks — detailed per-shot animation
        // YILDIZ: 0-5s Idle, 5-10s Idle, 15-25s Attack (HookPunch), 25-35s Attack
        AddDetailedAnimTrack(timelineAsset, "YILDIZ_Cinematic", new[]
        {
            ("Idle",      0f,  5f),
            ("Idle",      5f,  5f),
            ("HookPunch", 15f, 10f),
            ("HookPunch", 25f, 10f),
        });
        // KAYA: 10-15s Idle, 25-35s StaggerHeavy (ReceivingUppercut)
        AddDetailedAnimTrack(timelineAsset, "KAYA_Cinematic", new[]
        {
            ("Idle",              10f, 5f),
            ("ReceivingUppercut", 25f, 10f),
        });

        // VFX Signal track — HitEffect at 20s, ScreenFlash at 25s, SlowMotionKO at 33s
        var signalTrack = timelineAsset.CreateTrack<SignalTrack>(null, "VFX_Signals");
        CreateSignalAsset("HitEffect", 20f, signalTrack);
        CreateSignalAsset("ScreenFlash", 25f, signalTrack);
        CreateSignalAsset("SlowMotionKO", 33f, signalTrack);

        // Post-process Volume
        var ppGO = new GameObject("PostProcessVolume");
        var volume = ppGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, $"{TimelineDir}/CinematicPostProcess.asset");

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

        var splitTone = profile.Add<SplitToning>();
        splitTone.shadows.value = new Color(0.0f, 0.4f, 0.5f);
        splitTone.shadows.overrideState = true;
        splitTone.highlights.value = new Color(1.0f, 0.6f, 0.3f);
        splitTone.highlights.overrideState = true;
        splitTone.balance.value = -20f;
        splitTone.balance.overrideState = true;

        volume.profile = profile;

        // Spawn YILDIZ + KAYA for fight shots, apply palette materials
        SpawnCharacterPrefab("YILDIZ", new Vector3(1, 0, 0));
        SpawnCharacterPrefab("KAYA", new Vector3(-1, 0, 0));

        // Spawn remaining roster characters for Shot6
        SpawnCharacterPrefab("RUZGAR", new Vector3(-3, 0, 3));
        SpawnCharacterPrefab("CELIK", new Vector3(-1, 0, 3));
        SpawnCharacterPrefab("SIS", new Vector3(1, 0, 3));
        SpawnCharacterPrefab("TOPRAK", new Vector3(3, 0, 3));

        // HitSpawnPoint for VFX
        var hitPoint = new GameObject("HitSpawnPoint");
        hitPoint.transform.position = new Vector3(0, 1f, 0);

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
        Debug.Log("[Cinematic] Scene setup complete. Run VOLK > Cinematic > Setup Recorder to configure recording.");
    }

    [MenuItem("VOLK/Cinematic/Setup Recorder (MP4 1080p60)")]
    public static void SetupRecorder()
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(TimelineDir))
            AssetDatabase.CreateFolder("Assets", "Cinematic");

        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.SetRecordModeToTimeInterval(0, 60);
        settings.FrameRate = 60f;

        var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieSettings.name = "VOLKTrailer_Recorder";
        movieSettings.Enabled = true;
        movieSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080,
        };
        movieSettings.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
        movieSettings.OutputFile = "VOLKTrailer";

        settings.AddRecorderSettings(movieSettings);
        AssetDatabase.CreateAsset(settings, $"{TimelineDir}/VOLKRecorderSettings.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("[Cinematic] Recorder settings saved. Use Window > General > Recorder to record in Play Mode.");
#endif
    }

    // --- Helpers ---

#if UNITY_EDITOR
    static CinemachineVirtualCamera CreateVCam(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 0;
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

    static void AddDetailedAnimTrack(TimelineAsset timeline, string objectName,
        (string clipName, float start, float duration)[] clips)
    {
        var track = timeline.CreateTrack<AnimationTrack>(null, $"{objectName}_Anim");
        foreach (var (clipName, start, duration) in clips)
        {
            var clip = track.CreateClip<AnimationPlayableAsset>();
            clip.start = start;
            clip.duration = duration;
            clip.displayName = clipName;
        }
    }

    static void CreateSignalAsset(string signalName, float timeSec, SignalTrack track)
    {
        var signal = ScriptableObject.CreateInstance<SignalAsset>();
        string path = $"{TimelineDir}/{signalName}Signal.asset";
        AssetDatabase.CreateAsset(signal, path);

        var marker = track.CreateMarker<SignalEmitter>(timeSec);
        marker.asset = signal;
    }

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

            // Disable AI/input
            var fighter = instance.GetComponent<Fighter>();
            if (fighter != null)
            {
                fighter.isAI = false;
                fighter.enabled = false;
            }

            // Apply palette material if it exists
            string matPath = $"Assets/Materials/Characters/{charName}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = smr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = mat;
                    smr.sharedMaterials = mats;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Cinematic] Prefab not found: {prefabPath}");
        }
    }
}
