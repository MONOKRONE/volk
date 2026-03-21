using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SetupTouchUI
{
    [MenuItem("Tools/Setup Touch Controls UI")]
    public static void Setup()
    {
        // Find or create TouchCanvas
        var existing = GameObject.Find("TouchCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasGO = new GameObject("TouchCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Above HealthCanvas
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem exists
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // === LEFT SIDE: Joystick ===
        var joystickArea = CreatePanel(canvasGO.transform, "JoystickArea", new Color(0, 0, 0, 0));
        var jaRect = joystickArea.GetComponent<RectTransform>();
        jaRect.anchorMin = new Vector2(0, 0);
        jaRect.anchorMax = new Vector2(0.4f, 0.5f);
        jaRect.offsetMin = Vector2.zero;
        jaRect.offsetMax = Vector2.zero;

        var joystickBG = CreateCircle(joystickArea.transform, "JoystickBG", 120, new Color(1, 1, 1, 0.3f));
        var joystickKnob = CreateCircle(joystickBG.transform, "JoystickKnob", 60, new Color(1, 1, 1, 0.5f));
        joystickBG.SetActive(false); // Starts hidden, appears on touch

        var touchHandler = joystickArea.AddComponent<TouchInputHandler>();
        touchHandler.joystickBackground = joystickBG.GetComponent<RectTransform>();
        touchHandler.joystickKnob = joystickKnob.GetComponent<RectTransform>();

        // === RIGHT SIDE: Fight buttons ===
        // Row 1: PUNCH + KICK (bottom)
        var punchBtn = CreateFightButton(canvasGO.transform, "PunchButton", "PUNCH", "Punch",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 60), new Vector2(90, 90),
            new Color(0.9f, 0.2f, 0.2f, 0.5f));

        var kickBtn = CreateFightButton(canvasGO.transform, "KickButton", "KICK", "Kick",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 60), new Vector2(90, 90),
            new Color(0.2f, 0.5f, 0.9f, 0.5f));

        // Row 2: SK1, PARRY, SK2 (above)
        var sk1Btn = CreateFightButton(canvasGO.transform, "SK1Button", "SK1", "SK1",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-210, 170), new Vector2(70, 70),
            new Color(0.8f, 0.6f, 0.1f, 0.5f));

        var parryBtn = CreateFightButton(canvasGO.transform, "ParryButton", "PARRY", "Parry",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-125, 170), new Vector2(70, 70),
            new Color(0.2f, 0.8f, 0.3f, 0.5f));

        var sk2Btn = CreateFightButton(canvasGO.transform, "SK2Button", "SK2", "SK2",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 170), new Vector2(70, 70),
            new Color(0.8f, 0.6f, 0.1f, 0.5f));

        // === TouchCombatBridge ===
        var bridgeGO = new GameObject("TouchCombatBridge");
        var bridge = bridgeGO.AddComponent<TouchCombatBridge>();
        bridge.joystick = touchHandler;
        bridge.punchButton = punchBtn.GetComponent<FightButton>();
        bridge.kickButton = kickBtn.GetComponent<FightButton>();
        bridge.parryButton = parryBtn.GetComponent<FightButton>();
        bridge.sk1Button = sk1Btn.GetComponent<FightButton>();
        bridge.sk2Button = sk2Btn.GetComponent<FightButton>();

        // Try to find player Fighter
        var playerRoot = GameObject.Find("Player_Root");
        if (playerRoot != null)
        {
            bridge.fighter = playerRoot.GetComponent<Fighter>();
            Debug.Log("  TouchCombatBridge.fighter = Player_Root");
        }

        EditorUtility.SetDirty(canvasGO);
        EditorUtility.SetDirty(bridgeGO);

        Debug.Log("Touch controls UI setup complete!");
        Debug.Log("  Left: Floating joystick (appears on touch)");
        Debug.Log("  Right Row 1: PUNCH (90px), KICK (90px)");
        Debug.Log("  Right Row 2: SK1 (70px), PARRY (70px), SK2 (70px)");
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = true;
        return go;
    }

    static GameObject CreateCircle(Transform parent, string name, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        return go;
    }

    static GameObject CreateFightButton(Transform parent, string goName, string label, string buttonId,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        // Label
        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = size.x > 80 ? 18 : 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // FightButton component
        var fb = go.AddComponent<FightButton>();
        fb.buttonId = buttonId;

        return go;
    }
}
