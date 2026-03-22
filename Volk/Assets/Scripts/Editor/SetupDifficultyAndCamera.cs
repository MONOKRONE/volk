using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SetupDifficultyAndCamera
{
    [MenuItem("Tools/Setup Difficulty Buttons And Camera")]
    public static void Setup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        // Find PauseCanvas and its panel
        var pauseCanvas = GameObject.Find("PauseCanvas");
        if (pauseCanvas == null) { Debug.LogError("PauseCanvas not found!"); return; }

        var pm = pauseCanvas.GetComponent<PauseMenu>();
        if (pm == null) { Debug.LogError("PauseMenu not found!"); return; }

        // Find Panel inside PauseContainer
        var container = pm.pauseContainer;
        if (container == null) { Debug.LogError("PauseContainer null!"); return; }
        var panel = container.transform.Find("Panel");
        if (panel == null)
        {
            // Try to find any panel with VerticalLayoutGroup
            foreach (Transform child in container.transform)
                if (child.GetComponent<VerticalLayoutGroup>() != null) { panel = child; break; }
        }
        if (panel == null) { Debug.LogError("Panel not found in PauseContainer!"); return; }

        // Remove old difficulty section if exists
        var oldDiff = panel.Find("DifficultySection");
        if (oldDiff != null) Object.DestroyImmediate(oldDiff.gameObject);

        // Create DifficultySection
        var diffSection = new GameObject("DifficultySection", typeof(RectTransform), typeof(VerticalLayoutGroup));
        diffSection.transform.SetParent(panel, false);
        var dsVLG = diffSection.GetComponent<VerticalLayoutGroup>();
        dsVLG.spacing = 6;
        dsVLG.childAlignment = TextAnchor.UpperCenter;
        dsVLG.childForceExpandWidth = true;
        dsVLG.childForceExpandHeight = false;
        diffSection.AddComponent<LayoutElement>().preferredHeight = 90;

        // Label
        var label = new GameObject("DifficultyLabel", typeof(RectTransform));
        label.transform.SetParent(diffSection.transform, false);
        var labelTMP = label.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "ZORLUK"; labelTMP.fontSize = 20; labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.Center; labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.raycastTarget = false;
        label.AddComponent<LayoutElement>().preferredHeight = 28;

        // Button row
        var btnRow = new GameObject("DifficultyButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnRow.transform.SetParent(diffSection.transform, false);
        var hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        btnRow.AddComponent<LayoutElement>().preferredHeight = 45;

        var easyBtn = CreateDiffButton(btnRow.transform, "EasyButton", "KOLAY", 80, 45);
        var normalBtn = CreateDiffButton(btnRow.transform, "NormalButton", "NORMAL", 80, 45);
        var hardBtn = CreateDiffButton(btnRow.transform, "HardButton", "ZOR", 80, 45);

        // Wire to PauseMenu
        pm.easyButton = easyBtn.GetComponent<Button>();
        pm.normalButton = normalBtn.GetComponent<Button>();
        pm.hardButton = hardBtn.GetComponent<Button>();
        EditorUtility.SetDirty(pm);

        // Wire CameraFollow to Player_Root
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            var cf = cam.GetComponent<CameraFollow>();
            if (cf != null)
            {
                var playerRoot = GameObject.Find("Player_Root");
                if (playerRoot != null)
                {
                    cf.player = playerRoot.transform;
                    EditorUtility.SetDirty(cf);
                    Debug.Log("CameraFollow.player = Player_Root");
                }
            }
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Difficulty buttons and camera setup complete!");
    }

    static GameObject CreateDiffButton(Transform parent, string name, string text, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w; le.preferredHeight = h;

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 16; tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        var tRect = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;

        return go;
    }
}
