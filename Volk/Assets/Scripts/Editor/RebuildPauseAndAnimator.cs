using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.UI;
using TMPro;

public class RebuildPauseAndAnimator
{
    [MenuItem("Tools/Rebuild Pause Menu And Animator")]
    public static void Rebuild()
    {
        // Add WalkSpeed parameter to animator
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
        if (controller != null)
        {
            bool hasWalkSpeed = false;
            foreach (var p in controller.parameters)
                if (p.name == "WalkSpeed") { hasWalkSpeed = true; break; }
            if (!hasWalkSpeed)
            {
                controller.AddParameter("WalkSpeed", AnimatorControllerParameterType.Float);
                // Set default to 1
                var parms = controller.parameters;
                parms[parms.Length - 1].defaultFloat = 1f;
                controller.parameters = parms;
            }

            // Set Walk state to use WalkSpeed multiplier
            var sm = controller.layers[0].stateMachine;
            foreach (var cs in sm.states)
            {
                if (cs.state.name == "Walk")
                {
                    cs.state.speedParameterActive = true;
                    cs.state.speedParameter = "WalkSpeed";
                    Debug.Log("Walk state: speedParameter = WalkSpeed");
                }
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        // Load scene
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        // Add ScreenOrientationManager to GameManager
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null && gmGO.GetComponent<ScreenOrientationManager>() == null)
        {
            gmGO.AddComponent<ScreenOrientationManager>();
            EditorUtility.SetDirty(gmGO);
            Debug.Log("Added ScreenOrientationManager to GameManager");
        }

        // Rebuild pause menu
        var pauseCanvas = GameObject.Find("PauseCanvas");
        if (pauseCanvas == null) { Debug.LogError("PauseCanvas not found!"); return; }

        var pm = pauseCanvas.GetComponent<PauseMenu>();
        if (pm == null) { Debug.LogError("PauseMenu not found!"); return; }

        // Delete old container contents
        if (pm.pauseContainer != null)
            Object.DestroyImmediate(pm.pauseContainer);

        // New container
        var container = new GameObject("PauseContainer", typeof(RectTransform));
        container.transform.SetParent(pauseCanvas.transform, false);
        var contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero; contRect.anchorMax = Vector2.one; contRect.sizeDelta = Vector2.zero;

        // Overlay
        var overlay = CreateImage(container.transform, "Overlay", new Color(0, 0, 0, 0.7f));
        var ovRect = overlay.GetComponent<RectTransform>();
        ovRect.anchorMin = Vector2.zero; ovRect.anchorMax = Vector2.one; ovRect.sizeDelta = Vector2.zero;

        // Panel
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(container.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560, 320);
        panel.GetComponent<Image>().color = Hex("1A1A1A");
        var pvlg = panel.GetComponent<VerticalLayoutGroup>();
        pvlg.padding = new RectOffset(20, 20, 16, 16);
        pvlg.spacing = 12;
        pvlg.childAlignment = TextAnchor.UpperCenter;
        pvlg.childForceExpandWidth = true;
        pvlg.childForceExpandHeight = false;

        var pauseCG = panel.AddComponent<CanvasGroup>();

        // Title
        var title = MakeTMP(panel.transform, "TitleText", "DURAKLATILDI", 11, Hex("888888"));
        title.GetComponent<TextMeshProUGUI>().characterSpacing = 3;
        title.AddComponent<LayoutElement>().preferredHeight = 24;

        // Two column grid
        var grid = new GameObject("TwoColumnGrid", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        var hlg = grid.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16; hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        grid.AddComponent<LayoutElement>().flexibleHeight = 1;

        // Left column
        var leftCol = new GameObject("LeftColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
        leftCol.transform.SetParent(grid.transform, false);
        var leftVLG = leftCol.GetComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 8; leftVLG.childForceExpandWidth = true; leftVLG.childForceExpandHeight = false;
        leftCol.AddComponent<LayoutElement>().preferredWidth = 240;

        var resumeBtn = MakeButton(leftCol.transform, "ResumeButton", "▶  DEVAM ET", Color.white, Color.black, 44, 13);
        var restartBtn = MakeButton(leftCol.transform, "RestartButton", "↺  YENİDEN BAŞLAT", Hex("2A2A2A"), Color.white, 44, 13);
        MakeDivider(leftCol.transform);

        // Sound toggle with badge
        var soundBtn = MakeToggleButton(leftCol.transform, "SoundToggle", "Ses", "AÇIK", 42);
        var vibBtn = MakeToggleButton(leftCol.transform, "VibrationToggle", "Titreşim", "AÇIK", 42);

        // Right column
        var rightCol = new GameObject("RightColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rightCol.transform.SetParent(grid.transform, false);
        var rightVLG = rightCol.GetComponent<VerticalLayoutGroup>();
        rightVLG.spacing = 10; rightVLG.childForceExpandWidth = true; rightVLG.childForceExpandHeight = false;
        rightVLG.padding = new RectOffset(12, 0, 0, 0);
        rightCol.AddComponent<LayoutElement>().preferredWidth = 200;

        var diffLabel = MakeTMP(rightCol.transform, "DifficultyLabel", "ZORLUK SEVİYESİ", 11, Hex("666666"));
        diffLabel.GetComponent<TextMeshProUGUI>().characterSpacing = 2;
        diffLabel.AddComponent<LayoutElement>().preferredHeight = 24;

        var easyBtn = MakeButton(rightCol.transform, "EasyButton", "Kolay", Hex("2A2A2A"), Hex("AAAAAA"), 48, 14);
        var normalBtn = MakeButton(rightCol.transform, "NormalButton", "Normal", Hex("E6B800"), Hex("111111"), 48, 14);
        var hardBtn = MakeButton(rightCol.transform, "HardButton", "Zor", Hex("2A2A2A"), Hex("AAAAAA"), 48, 14);

        container.SetActive(false);

        // Wire PauseMenu
        pm.pauseContainer = container;
        pm.pausePanel = pauseCG;
        pm.resumeButton = resumeBtn.GetComponent<Button>();
        pm.restartButton = restartBtn.GetComponent<Button>();
        pm.soundToggleButton = soundBtn.GetComponent<Button>();
        pm.vibrationToggleButton = vibBtn.GetComponent<Button>();
        pm.soundLabel = soundBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        pm.vibrationLabel = vibBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        pm.soundBadge = soundBtn.transform.Find("Badge").GetComponent<TextMeshProUGUI>();
        pm.vibrationBadge = vibBtn.transform.Find("Badge").GetComponent<TextMeshProUGUI>();
        pm.easyButton = easyBtn.GetComponent<Button>();
        pm.normalButton = normalBtn.GetComponent<Button>();
        pm.hardButton = hardBtn.GetComponent<Button>();

        EditorUtility.SetDirty(pauseCanvas);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Pause menu rebuilt with 2-column layout!");
    }

    static GameObject MakeButton(Transform parent, string name, string text, Color bg, Color textColor, float h, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        go.AddComponent<LayoutElement>().preferredHeight = h;
        var tGO = new GameObject("Text", typeof(RectTransform));
        tGO.transform.SetParent(go.transform, false);
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        var r = tGO.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;
        return go;
    }

    static GameObject MakeToggleButton(Transform parent, string name, string labelText, string badgeText, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = Hex("2A2A2A");
        go.AddComponent<LayoutElement>().preferredHeight = h;
        var hlg2 = go.GetComponent<HorizontalLayoutGroup>();
        hlg2.padding = new RectOffset(12, 12, 0, 0);
        hlg2.childAlignment = TextAnchor.MiddleCenter;
        hlg2.childForceExpandWidth = true; hlg2.childForceExpandHeight = false;

        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var lTMP = label.AddComponent<TextMeshProUGUI>();
        lTMP.text = labelText; lTMP.fontSize = 13; lTMP.color = Color.white;
        lTMP.alignment = TextAlignmentOptions.Left; lTMP.raycastTarget = false;

        var badge = new GameObject("Badge", typeof(RectTransform));
        badge.transform.SetParent(go.transform, false);
        var bTMP = badge.AddComponent<TextMeshProUGUI>();
        bTMP.text = badgeText; bTMP.fontSize = 11; bTMP.color = Hex("88CC88");
        bTMP.alignment = TextAlignmentOptions.Right; bTMP.fontStyle = FontStyles.Bold; bTMP.raycastTarget = false;

        return go;
    }

    static void MakeDivider(Transform parent)
    {
        var d = CreateImage(parent, "Divider", Hex("333333"));
        d.AddComponent<LayoutElement>().preferredHeight = 1;
    }

    static GameObject CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = (name == "Overlay");
        return go;
    }

    static GameObject MakeTMP(Transform parent, string name, string text, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        return go;
    }

    static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
}
