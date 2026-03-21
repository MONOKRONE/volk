using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SetupRoundUI
{
    [MenuItem("Tools/Setup Round UI")]
    public static void Setup()
    {
        // Delete old GameOverPanel from HealthCanvas
        var healthCanvas = GameObject.Find("HealthCanvas");
        if (healthCanvas != null)
        {
            var gop = healthCanvas.transform.Find("GameOverPanel");
            if (gop != null) Object.DestroyImmediate(gop.gameObject);
        }

        // Delete old RoundCanvas
        var existing = GameObject.Find("RoundCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        // Create RoundCanvas
        var canvasGO = new GameObject("RoundCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // IntroGroup
        var introGO = new GameObject("IntroGroup", typeof(RectTransform), typeof(CanvasGroup));
        introGO.transform.SetParent(canvasGO.transform, false);
        var introRect = introGO.GetComponent<RectTransform>();
        introRect.anchorMin = Vector2.zero; introRect.anchorMax = Vector2.one; introRect.sizeDelta = Vector2.zero;
        var introGroup = introGO.GetComponent<CanvasGroup>();
        introGO.SetActive(false);

        var roundText = CreateTMP(introGO.transform, "RoundText", "ROUND 1", 80,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(800, 100));
        roundText.GetComponent<TextMeshProUGUI>().characterSpacing = 20;

        var fightText = CreateTMP(introGO.transform, "FightText", "FIGHT!", 100,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(600, 120));
        fightText.GetComponent<TextMeshProUGUI>().color = Color.yellow;
        fightText.SetActive(false);

        // HUD
        var hudGO = new GameObject("HUD", typeof(RectTransform));
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hudRect = hudGO.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one; hudRect.sizeDelta = Vector2.zero;

        var timerText = CreateTMP(hudGO.transform, "TimerText", "99", 60,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(120, 70));

        // Player round dots (left of timer)
        var pDotsGO = new GameObject("PlayerRoundDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        pDotsGO.transform.SetParent(hudGO.transform, false);
        var pDotsRect = pDotsGO.GetComponent<RectTransform>();
        pDotsRect.anchorMin = new Vector2(0.5f, 1); pDotsRect.anchorMax = new Vector2(0.5f, 1);
        pDotsRect.pivot = new Vector2(1, 1);
        pDotsRect.anchoredPosition = new Vector2(-80, -25); pDotsRect.sizeDelta = new Vector2(60, 20);
        pDotsGO.GetComponent<HorizontalLayoutGroup>().spacing = 8;

        var pDot1 = CreateDot(pDotsGO.transform, "Dot1");
        var pDot2 = CreateDot(pDotsGO.transform, "Dot2");

        // Enemy round dots (right of timer)
        var eDotsGO = new GameObject("EnemyRoundDots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        eDotsGO.transform.SetParent(hudGO.transform, false);
        var eDotsRect = eDotsGO.GetComponent<RectTransform>();
        eDotsRect.anchorMin = new Vector2(0.5f, 1); eDotsRect.anchorMax = new Vector2(0.5f, 1);
        eDotsRect.pivot = new Vector2(0, 1);
        eDotsRect.anchoredPosition = new Vector2(80, -25); eDotsRect.sizeDelta = new Vector2(60, 20);
        eDotsGO.GetComponent<HorizontalLayoutGroup>().spacing = 8;

        var eDot1 = CreateDot(eDotsGO.transform, "Dot1");
        var eDot2 = CreateDot(eDotsGO.transform, "Dot2");

        // ResultGroup
        var resultGO = new GameObject("ResultGroup", typeof(RectTransform), typeof(CanvasGroup));
        resultGO.transform.SetParent(canvasGO.transform, false);
        var resultRect = resultGO.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero; resultRect.anchorMax = Vector2.one; resultRect.sizeDelta = Vector2.zero;
        resultGO.SetActive(false);

        var resultText = CreateTMP(resultGO.transform, "ResultText", "K.O.", 120,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 150));

        // MatchResultPanel
        var matchPanel = new GameObject("MatchResultPanel", typeof(RectTransform), typeof(Image));
        matchPanel.transform.SetParent(canvasGO.transform, false);
        var mpRect = matchPanel.GetComponent<RectTransform>();
        mpRect.anchorMin = Vector2.zero; mpRect.anchorMax = Vector2.one; mpRect.sizeDelta = Vector2.zero;
        matchPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        matchPanel.SetActive(false);

        var matchResultText = CreateTMP(matchPanel.transform, "MatchResultText", "YOU WIN", 90,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 110));

        var restartText = CreateTMP(matchPanel.transform, "RestartText", "TAP TO RESTART", 36,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(500, 50));
        restartText.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.8f, 0.8f);

        // Add RoundUI component
        var roundUI = canvasGO.AddComponent<RoundUI>();
        roundUI.roundText = roundText.GetComponent<TextMeshProUGUI>();
        roundUI.fightText = fightText.GetComponent<TextMeshProUGUI>();
        roundUI.introGroup = introGroup;
        roundUI.timerText = timerText.GetComponent<TextMeshProUGUI>();
        roundUI.playerRoundDots = new Image[] { pDot1.GetComponent<Image>(), pDot2.GetComponent<Image>() };
        roundUI.enemyRoundDots = new Image[] { eDot1.GetComponent<Image>(), eDot2.GetComponent<Image>() };
        roundUI.resultText = resultText.GetComponent<TextMeshProUGUI>();
        roundUI.resultGroup = resultGO.GetComponent<CanvasGroup>();
        roundUI.matchResultPanel = matchPanel;
        roundUI.matchResultText = matchResultText.GetComponent<TextMeshProUGUI>();
        roundUI.restartText = restartText.GetComponent<TextMeshProUGUI>();

        // Wire GameManager
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            // Remove old GameManager component and re-add
            var oldGM = gmGO.GetComponent<GameManager>();
            if (oldGM != null) Object.DestroyImmediate(oldGM);
            var gm = gmGO.AddComponent<GameManager>();

            var playerRoot = GameObject.Find("Player_Root");
            var enemyRoot = GameObject.Find("Enemy_Root");
            if (playerRoot != null) gm.playerFighter = playerRoot.GetComponent<Fighter>();
            if (enemyRoot != null) gm.enemyFighter = enemyRoot.GetComponent<Fighter>();
            gm.roundUI = roundUI;
            EditorUtility.SetDirty(gmGO);
        }

        EditorUtility.SetDirty(canvasGO);
        Debug.Log("Round UI setup complete!");
    }

    static GameObject CreateTMP(Transform parent, string name, string text, int fontSize,
        Vector2 anchorMinMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMinMax; rect.anchorMax = anchorMinMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    static GameObject CreateDot(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.2f);
        img.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 20; le.preferredHeight = 20;
        return go;
    }
}
