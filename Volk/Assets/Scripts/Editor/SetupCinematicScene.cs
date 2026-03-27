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
/// PLA-123: Complete cinematic scene setup. Press Play and the 60s trailer runs automatically.
/// Menu: VOLK > Cinematic > Setup Cinematic Scene
/// </summary>
public class SetupCinematicScene
{
    static readonly string TimelineDir = "Assets/Cinematic";

    // Character definitions: name, FBX path, scale, color, position, yRotation
    struct CharDef
    {
        public string name;
        public string fbxPath;
        public float scale;
        public Color color;
        public Vector3 position;
        public float yRotation;
    }

    static readonly CharDef[] Characters = new[]
    {
        new CharDef { name = "YILDIZ", fbxPath = "Assets/Characters/YILDIZ/Dreyar.fbx",
            scale = 0.1f, color = new Color(1f, 0.55f, 0f),
            position = new Vector3(2, 0, 0), yRotation = 180f },
        new CharDef { name = "KAYA", fbxPath = "Assets/Characters/KAYA/AlienSoldier.fbx",
            scale = 0.1f, color = new Color(0.29f, 0.29f, 0.29f),
            position = new Vector3(-2, 0, 0), yRotation = 0f },
        new CharDef { name = "RUZGAR", fbxPath = "Assets/Characters/RUZGAR/Ninja.fbx",
            scale = 0.1f, color = new Color(0f, 0.4f, 1f),
            position = new Vector3(4, 0, 2), yRotation = 0f },
        new CharDef { name = "CELIK", fbxPath = "Assets/Characters/CELIK/Ely.fbx",
            scale = 1.0f, color = new Color(0.75f, 0.75f, 0.75f),
            position = new Vector3(-4, 0, 2), yRotation = 0f },
        new CharDef { name = "SIS", fbxPath = "Assets/Characters/SIS/Medea.fbx",
            scale = 10.0f, color = new Color(0.55f, 0f, 1f),
            position = new Vector3(0, 0, 4), yRotation = 0f },
        new CharDef { name = "TOPRAK", fbxPath = "Assets/Characters/TOPRAK/Astra.fbx",
            scale = 0.1f, color = new Color(0.55f, 0.27f, 0.07f),
            position = new Vector3(0, 0, -4), yRotation = 0f },
    };

    [MenuItem("VOLK/Cinematic/Setup Cinematic Scene")]
    public static void Setup()
    {
#if UNITY_EDITOR
        // 1. Open or create scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "Cinematic";

        // 2. Clean old CinematicRoot if present
        var oldRoot = GameObject.Find("CinematicRoot");
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        var root = new GameObject("CinematicRoot");
        int spawnedCount = 0;

        // === CHARACTERS ===

        // Ensure material directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Characters"))
            AssetDatabase.CreateFolder("Assets/Materials", "Characters");

        var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

        foreach (var ch in Characters)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ch.fbxPath);
            if (prefab == null)
            {
                // Fallback: try prefab path
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Prefabs/Characters/{ch.name}_Fighter.prefab");
            }
            if (prefab == null)
            {
                Debug.LogWarning($"[Cinematic] Character not found: {ch.name} ({ch.fbxPath})");
                continue;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"{ch.name}_Cinematic";
            go.transform.SetParent(root.transform);
            go.transform.position = ch.position;
            go.transform.rotation = Quaternion.Euler(0, ch.yRotation, 0);
            go.transform.localScale = Vector3.one * ch.scale;

            // Disable AI/input
            var fighter = go.GetComponent<Fighter>();
            if (fighter != null)
            {
                fighter.isAI = false;
                fighter.enabled = false;
            }

            // CharacterController fix for scaled characters
            var cc = go.GetComponent<CharacterController>();
            if (cc != null)
            {
                float s = ch.scale;
                cc.height = 1.8f * s;
                cc.radius = 0.3f * s;
                cc.stepOffset = Mathf.Min(0.3f * s, (cc.height + cc.radius * 2f) * 0.5f);
                cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            }

            // URP/Lit material with character color
            string matPath = $"Assets/Materials/Characters/{ch.name}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLitShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.color = ch.color;
            mat.SetColor("_BaseColor", ch.color);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // YILDIZ gets emission
            if (ch.name == "YILDIZ")
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", ch.color * 0.3f);
                EditorUtility.SetDirty(mat);
            }

            // Apply material to ALL renderers
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = mat;
                renderer.sharedMaterials = mats;
            }

            spawnedCount++;
        }

        Debug.Log($"[Cinematic] Characters spawned: {spawnedCount}");

        // === ARENA ===

        var arenaRoot = new GameObject("MMA_Arena");
        arenaRoot.transform.SetParent(root.transform);

        SpawnArenaMesh("Assets/MarpaStudio/Mesh/OctagonFloor.fbx", arenaRoot.transform,
            new Vector3(0, -0.01f, 0), Vector3.one);
        SpawnArenaMesh("Assets/MarpaStudio/Mesh/OctagonCage.fbx", arenaRoot.transform,
            Vector3.zero, Vector3.one);

        // Ambient + Lighting
        RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.15f);

        var existingLights = Object.FindObjectsOfType<Light>();
        foreach (var l in existingLights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 1.2f;
                l.transform.rotation = Quaternion.Euler(-30f, 45f, 0);
                l.color = new Color(1f, 0.95f, 0.8f);
            }
        }

        var rimLightGO = new GameObject("RimLight");
        rimLightGO.transform.SetParent(root.transform);
        var rimLight = rimLightGO.AddComponent<Light>();
        rimLight.type = LightType.Point;
        rimLightGO.transform.position = new Vector3(0, 3f, 3f);
        rimLight.intensity = 3f;
        rimLight.color = new Color(0.3f, 0.5f, 1f);
        rimLight.range = 15f;

        Debug.Log("[Cinematic] Arena setup complete");

        // === CAMERA + CINEMACHINE ===

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        // Clean existing brain then add fresh
        var existingBrain = mainCam.GetComponent<CinemachineBrain>();
        if (existingBrain != null) Object.DestroyImmediate(existingBrain);
        mainCam.gameObject.AddComponent<CinemachineBrain>();

        // Letterbox
        if (mainCam.GetComponent<Volk.Cinematic.CinematicLetterbox>() == null)
            mainCam.gameObject.AddComponent<Volk.Cinematic.CinematicLetterbox>();

        // 7 Virtual Cameras under CinematicRoot
        var vcamOpening = CreateVCam("VCam_Opening", root.transform,
            new Vector3(0, 1f, -8f), new Vector3(10, 0, 0), 60f, 10);
        var vcamYildiz = CreateVCam("VCam_YILDIZ", root.transform,
            new Vector3(3f, 1.5f, -3f), new Vector3(10, -30, 0), 40f, 0);
        var vcamKaya = CreateVCam("VCam_KAYA", root.transform,
            new Vector3(-2f, 0.8f, -2.5f), new Vector3(5, 20, 0), 35f, 0);
        var vcamFight = CreateVCam("VCam_Fight", root.transform,
            new Vector3(0f, 1.2f, -5f), new Vector3(8, 0, 0), 55f, 0);
        var vcamGhost = CreateVCam("VCam_Ghost", root.transform,
            new Vector3(0f, 2.5f, -10f), new Vector3(20, 0, 0), 70f, 0);
        var vcamRoster = CreateVCam("VCam_Roster", root.transform,
            new Vector3(0f, 4f, -8f), new Vector3(30, 0, 0), 65f, 0);
        var vcamLogo = CreateVCam("VCam_Logo", root.transform,
            new Vector3(0f, 5f, -15f), new Vector3(25, 0, 0), 50f, 0);

        // Fight camera shake
        var fightNoise = vcamFight.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (fightNoise == null)
            fightNoise = vcamFight.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        fightNoise.m_AmplitudeGain = 0.5f;
        fightNoise.m_FrequencyGain = 0.3f;

        // === TIMELINE ===

        if (!AssetDatabase.IsValidFolder(TimelineDir))
            AssetDatabase.CreateFolder("Assets", "Cinematic");

        var timelineAsset = TimelineAsset.CreateInstance<TimelineAsset>();
        string timelinePath = $"{TimelineDir}/VOLKTrailer.playable";
        AssetDatabase.CreateAsset(timelineAsset, timelinePath);

        var directorGO = new GameObject("CinematicDirector");
        directorGO.transform.SetParent(root.transform);
        var playableDir = directorGO.AddComponent<PlayableDirector>();
        playableDir.playableAsset = timelineAsset;
        playableDir.playOnAwake = true;
        playableDir.extrapolationMode = DirectorWrapMode.Hold;

        directorGO.AddComponent<Volk.Cinematic.CinematicDirector>();
        directorGO.AddComponent<Volk.Cinematic.CinematicVFXReceiver>();

        // Bind Cinemachine tracks to vcam objects
        int boundTrackCount = 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot1_Opening", vcamOpening, 0, 5) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot2_YILDIZ", vcamYildiz, 5, 5) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot3_KAYA", vcamKaya, 10, 5) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot4_Fight", vcamFight, 15, 20) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot5_Ghost", vcamGhost, 35, 10) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot6_Roster", vcamRoster, 45, 7) ? 1 : 0;
        boundTrackCount += BindCinemachineTrack(timelineAsset, playableDir, "Shot7_Logo", vcamLogo, 52, 8) ? 1 : 0;

        Debug.Log($"[Cinematic] Timeline bound to {boundTrackCount} tracks");

        // Animator Tracks
        AddDetailedAnimTrack(timelineAsset, "YILDIZ_Cinematic", new[]
        {
            ("Idle",      0f,  5f),
            ("Idle",      5f,  5f),
            ("HookPunch", 15f, 10f),
            ("HookPunch", 25f, 10f),
        });
        AddDetailedAnimTrack(timelineAsset, "KAYA_Cinematic", new[]
        {
            ("Idle",              10f, 5f),
            ("ReceivingUppercut", 25f, 10f),
        });

        // VFX Signal track
        var signalTrack = timelineAsset.CreateTrack<SignalTrack>(null, "VFX_Signals");
        CreateSignalAsset("HitEffect", 20f, signalTrack);
        CreateSignalAsset("ScreenFlash", 25f, signalTrack);
        CreateSignalAsset("SlowMotionKO", 33f, signalTrack);

        // Reset director time
        playableDir.time = 0;

        // === VFX WIRING ===

        var hitPoint = new GameObject("HitSpawnPoint");
        hitPoint.transform.SetParent(root.transform);
        hitPoint.transform.position = new Vector3(0, 1f, 0);

        // HitEffectManager
        var hitMgrGO = new GameObject("HitEffectManager");
        hitMgrGO.transform.SetParent(root.transform);
        var hitMgr = hitMgrGO.AddComponent<HitEffectManager>();

        // Load VFX prefabs (null-safe)
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

        // JuiceManager
        var juiceMgrGO = new GameObject("JuiceManager");
        juiceMgrGO.transform.SetParent(root.transform);
        var juiceMgr = juiceMgrGO.AddComponent<JuiceManager>();

        // Wire VFXReceiver
        var vfxReceiver = directorGO.GetComponent<Volk.Cinematic.CinematicVFXReceiver>();
        if (vfxReceiver != null)
        {
            vfxReceiver.hitEffectManager = hitMgr;
            vfxReceiver.juiceManager = juiceMgr;
            vfxReceiver.hitSpawnPoint = hitPoint.transform;
        }

        // === POST-PROCESSING ===

        var ppGO = new GameObject("PostProcessVolume");
        ppGO.transform.SetParent(root.transform);
        var volume = ppGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, $"{TimelineDir}/CinematicPostProcess.asset");

        var bloom = profile.Add<Bloom>();
        bloom.intensity.value = 1.2f;
        bloom.intensity.overrideState = true;
        bloom.threshold.value = 0.8f;
        bloom.threshold.overrideState = true;

        var vignette = profile.Add<Vignette>();
        vignette.intensity.value = 0.35f;
        vignette.intensity.overrideState = true;

        var colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.contrast.value = 15f;
        colorAdj.contrast.overrideState = true;
        colorAdj.saturation.value = 10f;
        colorAdj.saturation.overrideState = true;

        volume.profile = profile;

        // === SAVE ===

        string scenePath = "Assets/Scenes/Cinematic.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in buildScenes) if (s.path == scenePath) { found = true; break; }
        if (!found)
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Cinematic] Ready! Press Play.");
#endif
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
    static CinemachineVirtualCamera CreateVCam(string name, Transform parent,
        Vector3 position, Vector3 rotation, float fov, int priority)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(rotation);
        var vcam = go.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.FieldOfView = fov;
        vcam.Priority = priority;
        return vcam;
    }

    static bool BindCinemachineTrack(TimelineAsset timeline, PlayableDirector director,
        string shotName, CinemachineVirtualCamera vcam, float startSec, float durationSec)
    {
        var track = timeline.CreateTrack<CinemachineTrack>(null, shotName);
        var clip = track.CreateClip<CinemachineShot>();
        clip.start = startSec;
        clip.duration = durationSec;
        clip.displayName = shotName;

        if (vcam != null)
        {
            director.SetGenericBinding(track, vcam);
            return true;
        }
        return false;
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

    static void SpawnArenaMesh(string meshPath, Transform parent, Vector3 position, Vector3 scale)
    {
        var meshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
        if (meshPrefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
        }
        else
        {
            Debug.LogWarning($"[Cinematic] Arena mesh not found: {meshPath}");
        }
    }
}
