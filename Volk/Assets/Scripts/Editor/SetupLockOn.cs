using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SetupLockOn
{
    [MenuItem("Tools/Setup Lock-On Button")]
    public static void Setup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        var playerRoot = GameObject.Find("Player_Root");
        Fighter playerFighter = playerRoot != null ? playerRoot.GetComponent<Fighter>() : null;

        // Wire CameraFollow
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            var cf = cam.GetComponent<CameraFollow>();
            if (cf != null && playerRoot != null)
            {
                cf.player = playerRoot.transform;
                cf.playerFighter = playerFighter;
                EditorUtility.SetDirty(cf);
                Debug.Log("CameraFollow: player + playerFighter wired");
            }
        }

        // Add LockOn button to TouchCanvas
        var touchCanvas = GameObject.Find("TouchCanvas");
        if (touchCanvas == null) { Debug.LogError("TouchCanvas not found!"); return; }

        // Remove old
        var old = touchCanvas.transform.Find("LockOnButton");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var btnGO = new GameObject("LockOnButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(touchCanvas.transform, false);
        var rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.pivot = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(-15, 0);
        rect.sizeDelta = new Vector2(70, 40);
        btnGO.GetComponent<Image>().color = new Color(0.9f, 0.72f, 0f);

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "LOCK"; tmp.fontSize = 12; tmp.color = new Color(0.08f, 0.08f, 0.08f);
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        var lockOn = btnGO.AddComponent<LockOnButton>();
        lockOn.playerFighter = playerFighter;
        lockOn.buttonImage = btnGO.GetComponent<Image>();
        lockOn.buttonText = tmp;

        EditorUtility.SetDirty(btnGO);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("LockOn button added to TouchCanvas!");
    }
}
