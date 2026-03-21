using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SetupPauseMenu
{
    [MenuItem("Tools/Setup Pause Menu")]
    public static void Setup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        // Delete old
        var old = GameObject.Find("PauseCanvas");
        if (old != null) Object.DestroyImmediate(old);
        var oldVib = GameObject.Find("VibrationManager");
        if (oldVib != null) Object.DestroyImmediate(oldVib);

        // VibrationManager
        var vibGO = new GameObject("VibrationManager");
        vibGO.AddComponent<VibrationManager>();

        // PauseCanvas
        var canvasGO = new GameObject("PauseCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // PauseContainer (inactive)
        var container = new GameObject("PauseContainer", typeof(RectTransform));
        container.transform.SetParent(canvasGO.transform, false);
        var contRect = container.GetComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero; contRect.anchorMax = Vector2.one; contRect.sizeDelta = Vector2.zero;

        // Overlay
        var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(container.transform, false);
        var ovRect = overlay.GetComponent<RectTransform>();
        ovRect.anchorMin = Vector2.zero; ovRect.anchorMax = Vector2.one; ovRect.sizeDelta = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        overlay.GetComponent<Image>().raycastTarget = true;

        // Panel
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(container.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f); panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(320, 420);
        panel.GetComponent<Image>().color = HexColor("1A1A1A");
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(30, 30, 30, 30);
        vlg.spacing = 15;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // CanvasGroup on panel
        var pauseCG = panel.AddComponent<CanvasGroup>();

        // Title
        var title = CreateTMP(panel.transform, "TitleText", "DURAKLATILDI", 28);
        title.AddComponent<LayoutElement>().preferredHeight = 50;

        // Resume button
        var resumeBtn = CreateButton(panel.transform, "ResumeButton", "▶  DEVAM ET", Color.white, Color.black, 60);

        // Restart button
        var restartBtn = CreateButton(panel.transform, "RestartButton", "↺  YENİDEN BAŞLAT", Color.white, Color.black, 60);

        // Spacer
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(panel.transform, false);
        spacer.GetComponent<LayoutElement>().preferredHeight = 10;

        // Sound toggle
        var soundBtn = CreateButton(panel.transform, "SoundToggle", "SES AÇIK", HexColor("333333"), Color.white, 55);

        // Vibration toggle
        var vibBtn = CreateButton(panel.transform, "VibrationToggle", "TİTREŞİM AÇIK", HexColor("333333"), Color.white, 55);

        container.SetActive(false);

        // PauseMenu component
        var pm = canvasGO.AddComponent<PauseMenu>();
        pm.pausePanel = pauseCG;
        pm.pauseContainer = container;
        pm.resumeButton = resumeBtn.GetComponent<Button>();
        pm.restartButton = restartBtn.GetComponent<Button>();
        pm.soundToggleButton = soundBtn.GetComponent<Button>();
        pm.vibrationToggleButton = vibBtn.GetComponent<Button>();
        pm.soundLabel = soundBtn.GetComponentInChildren<TextMeshProUGUI>();
        pm.vibrationLabel = vibBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Pause button on TouchCanvas
        var touchCanvas = GameObject.Find("TouchCanvas");
        if (touchCanvas != null)
        {
            var oldPause = touchCanvas.transform.Find("PauseButton");
            if (oldPause != null) Object.DestroyImmediate(oldPause.gameObject);

            var pauseBtnGO = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            pauseBtnGO.transform.SetParent(touchCanvas.transform, false);
            var pbRect = pauseBtnGO.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(1, 1); pbRect.anchorMax = new Vector2(1, 1);
            pbRect.pivot = new Vector2(1, 1);
            pbRect.anchoredPosition = new Vector2(-15, -15);
            pbRect.sizeDelta = new Vector2(50, 50);
            pauseBtnGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);

            var pauseText = new GameObject("Text", typeof(RectTransform));
            pauseText.transform.SetParent(pauseBtnGO.transform, false);
            var ptmp = pauseText.AddComponent<TextMeshProUGUI>();
            ptmp.text = "⏸"; ptmp.fontSize = 24; ptmp.color = Color.white;
            ptmp.alignment = TextAlignmentOptions.Center; ptmp.raycastTarget = false;
            var ptRect = pauseText.GetComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero; ptRect.anchorMax = Vector2.one; ptRect.sizeDelta = Vector2.zero;

            // Wire pause button click
            var pauseButton = pauseBtnGO.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                pauseButton.onClick,
                new UnityEngine.Events.UnityAction(pm.TogglePause));
        }

        EditorUtility.SetDirty(canvasGO);
        EditorUtility.SetDirty(vibGO);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Pause menu setup complete!");
    }

    static GameObject CreateButton(Transform parent, string name, string text, Color bgColor, Color textColor, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;
        go.AddComponent<LayoutElement>().preferredHeight = height;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 22; tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        var tRect = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;

        return go;
    }

    static GameObject CreateTMP(Transform parent, string name, string text, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return go;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
