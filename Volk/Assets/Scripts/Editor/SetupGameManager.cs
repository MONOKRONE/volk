using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class SetupGameManager
{
    [MenuItem("Tools/Setup Game Manager")]
    public static void Setup()
    {
        // Add scene to build settings
        var scenePath = "Assets/Scenes/CombatTest.unity";
        var scenes = EditorBuildSettings.scenes;
        bool found = false;
        foreach (var s in scenes)
            if (s.path == scenePath) { found = true; break; }
        if (!found)
        {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("Added CombatTest to Build Settings");
        }

        // Find HealthCanvas
        var canvas = GameObject.Find("HealthCanvas");
        if (canvas == null) { Debug.LogError("HealthCanvas not found!"); return; }

        // Create GameOverPanel
        var existing = canvas.transform.Find("GameOverPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        // ResultText
        var resultGO = new GameObject("ResultText", typeof(RectTransform));
        resultGO.transform.SetParent(panel.transform, false);
        var resultTMP = resultGO.AddComponent<TextMeshProUGUI>();
        resultTMP.text = "YOU WIN";
        resultTMP.fontSize = 60;
        resultTMP.color = Color.white;
        resultTMP.alignment = TextAlignmentOptions.Center;
        resultTMP.fontStyle = FontStyles.Bold;
        var resultRect = resultGO.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(0, 30);
        resultRect.sizeDelta = new Vector2(600, 80);

        // RestartHintText
        var hintGO = new GameObject("RestartHintText", typeof(RectTransform));
        hintGO.transform.SetParent(panel.transform, false);
        var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
        hintTMP.text = "Press R to restart";
        hintTMP.fontSize = 24;
        hintTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        hintTMP.alignment = TextAlignmentOptions.Center;
        var hintRect = hintGO.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0, -30);
        hintRect.sizeDelta = new Vector2(400, 40);

        panel.SetActive(false);

        // Create GameManager GameObject
        var gmExisting = GameObject.Find("GameManager");
        if (gmExisting != null) Object.DestroyImmediate(gmExisting);

        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.gameOverPanel = panel;
        gm.resultText = resultTMP;
        gm.restartHintText = hintTMP;

        EditorUtility.SetDirty(gmGO);
        EditorUtility.SetDirty(canvas);

        Debug.Log("GameManager setup complete!");
        Debug.Log($"  GameOverPanel: {panel.name}");
        Debug.Log($"  ResultText: {resultTMP.text}");
        Debug.Log($"  RestartHintText: {hintTMP.text}");
    }
}
