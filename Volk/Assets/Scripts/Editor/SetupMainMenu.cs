using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

public class SetupMainMenu
{
    [MenuItem("Tools/Setup Main Menu Scene")]
    public static void Setup()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Lighting ---
        var mainLight = new GameObject("Directional Light");
        var light1 = mainLight.AddComponent<Light>();
        light1.type = LightType.Directional;
        light1.intensity = 1.2f;
        light1.color = HexColor("FFF5E0");
        mainLight.transform.eulerAngles = new Vector3(35, -45, 0);

        var rimLight = new GameObject("Rim Light");
        var light2 = rimLight.AddComponent<Light>();
        light2.type = LightType.Directional;
        light2.intensity = 0.4f;
        light2.color = HexColor("C8D8FF");
        rimLight.transform.eulerAngles = new Vector3(-20, 135, 0);

        // --- Floor ---
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Arena_Floor";
        floor.transform.localScale = new Vector3(3, 1, 3);
        floor.isStatic = true;

        // --- Characters ---
        // Player display (Maria)
        var playerDisplay = new GameObject("Player_Display");
        playerDisplay.transform.position = new Vector3(-1.5f, 0, 0);

        var mariaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Maria.fbx");
        if (mariaPrefab != null)
        {
            var maria = (GameObject)PrefabUtility.InstantiatePrefab(mariaPrefab);
            maria.name = "Maria_Display";
            maria.transform.SetParent(playerDisplay.transform, false);
            maria.transform.localPosition = new Vector3(0, 0.08f, 0);
            maria.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var mariaAnim = maria.GetComponent<Animator>();
            if (mariaAnim != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAnimator.controller");
                mariaAnim.runtimeAnimatorController = controller;
                mariaAnim.applyRootMotion = false;
            }
        }

        // Enemy display (Kachujin)
        var enemyDisplay = new GameObject("Enemy_Display");
        enemyDisplay.transform.position = new Vector3(1.5f, 0, 0);

        var kachujinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Kachujin.fbx");
        if (kachujinPrefab != null)
        {
            var kachujin = (GameObject)PrefabUtility.InstantiatePrefab(kachujinPrefab);
            kachujin.name = "Kachujin_Display";
            kachujin.transform.SetParent(enemyDisplay.transform, false);
            kachujin.transform.localPosition = new Vector3(0, 0.12f, 0);
            kachujin.transform.localRotation = Quaternion.Euler(0, -90, 0);
            var kachujinAnim = kachujin.GetComponent<Animator>();
            if (kachujinAnim != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAnimator.controller");
                kachujinAnim.runtimeAnimatorController = controller;
                kachujinAnim.applyRootMotion = false;
            }
        }

        // --- Camera ---
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<UniversalAdditionalCameraData>();
        camGO.transform.position = new Vector3(0, 1.6f, -4.5f);
        camGO.transform.eulerAngles = new Vector3(8, 0, 0);
        camGO.AddComponent<CameraMenuDrift>();

        // --- UI Canvas ---
        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        var menuGroup = canvasGO.AddComponent<CanvasGroup>();

        // Logo
        var logoGO = CreateTMP(canvasGO.transform, "LogoText", "VOLK", 120,
            new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(600, 140));
        var logoTMP = logoGO.GetComponent<TextMeshProUGUI>();
        logoTMP.characterSpacing = 30;

        // Tagline
        var taglineGO = CreateTMP(canvasGO.transform, "TaglineText", "FIGHT.", 28,
            new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(400, 40));
        var taglineTMP = taglineGO.GetComponent<TextMeshProUGUI>();
        taglineTMP.color = new Color(1, 1, 1, 0.6f);
        taglineTMP.characterSpacing = 15;

        // Play Button
        var btnGO = new GameObject("PlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0); btnRect.anchorMax = new Vector2(0.5f, 0);
        btnRect.pivot = new Vector2(0.5f, 0);
        btnRect.anchoredPosition = new Vector2(0, 120);
        btnRect.sizeDelta = new Vector2(220, 70);
        btnGO.GetComponent<Image>().color = Color.white;

        var btnTextGO = CreateTMP(btnGO.transform, "ButtonText", "PLAY", 36,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 70));
        btnTextGO.GetComponent<TextMeshProUGUI>().color = Color.black;

        // Version
        var versionGO = CreateTMP(canvasGO.transform, "VersionText", "v0.1", 18,
            new Vector2(1, 0), new Vector2(-20, 15), new Vector2(100, 30));
        versionGO.GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.3f);
        versionGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // MainMenuController (runtime UI is built via GameFlowManager, no fields to wire)
        canvasGO.AddComponent<MainMenuController>();

        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        // Update build settings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/CombatTest.unity", true)
        };

        Debug.Log("MainMenu scene created and saved!");
        Debug.Log("Build settings: MainMenu=0, CombatTest=1");
    }

    static GameObject CreateTMP(Transform parent, string name, string text, int fontSize,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor; rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return go;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
