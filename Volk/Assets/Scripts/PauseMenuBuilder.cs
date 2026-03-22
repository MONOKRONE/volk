using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuBuilder : MonoBehaviour
{
    public PauseMenu pauseMenu;

    void Awake()
    {
        BuildUI();
    }

    void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null || pauseMenu == null) return;

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // Overlay
        GameObject overlay = CreateImage(gameObject, "Overlay", new Color(0, 0, 0, 0.75f));
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        overlay.SetActive(false);
        pauseMenu.overlayObj = overlay;

        // Panel
        GameObject panel = CreateImage(overlay, "Panel", new Color(0.1f, 0.1f, 0.1f, 1f));
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(560, 280);
        prt.anchoredPosition = Vector2.zero;

        // Title
        GameObject title = CreateTMP(panel, "Title", "DURAKLATILDI", 11, new Color(0.53f, 0.53f, 0.53f));
        RectTransform trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.offsetMin = new Vector2(0, -40); trt.offsetMax = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TextMeshProUGUI>().characterSpacing = 4;

        // Grid
        GameObject grid = new GameObject("Grid");
        grid.transform.SetParent(panel.transform, false);
        RectTransform grt = grid.AddComponent<RectTransform>();
        grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
        grt.offsetMin = new Vector2(20, 16); grt.offsetMax = new Vector2(-20, -44);
        HorizontalLayoutGroup hlg = grid.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // LEFT COLUMN
        GameObject left = new GameObject("LeftColumn");
        left.transform.SetParent(grid.transform, false);
        left.AddComponent<RectTransform>();
        LayoutElement leL = left.AddComponent<LayoutElement>();
        leL.preferredWidth = 260;
        VerticalLayoutGroup vlgL = left.AddComponent<VerticalLayoutGroup>();
        vlgL.spacing = 8;
        vlgL.childForceExpandWidth = true;
        vlgL.childForceExpandHeight = false;

        GameObject resumeBtn = CreateButton(left, "ResumeBtn", "\u25B6  DEVAM ET",
            Color.white, new Color(0.08f, 0.08f, 0.08f), 13, 44);
        pauseMenu.resumeButton = resumeBtn.GetComponent<Button>();

        GameObject restartBtn = CreateButton(left, "RestartBtn", "\u21BA  YEN\u0130DEN BA\u015eLA",
            new Color(0.9f, 0.9f, 0.9f), new Color(0.16f, 0.16f, 0.16f), 13, 44);
        pauseMenu.restartButton = restartBtn.GetComponent<Button>();

        CreateDivider(left, new Color(0.2f, 0.2f, 0.2f));

        GameObject soundBtn = CreateToggleButton(left, "SoundBtn", "Ses", "A\u00c7IK", out TextMeshProUGUI soundBadge);
        pauseMenu.soundBadge = soundBadge;
        pauseMenu.soundToggleButton = soundBtn.GetComponent<Button>();
        pauseMenu.soundLabel = soundBtn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

        GameObject vibBtn = CreateToggleButton(left, "VibBtn", "Titre\u015fim", "A\u00c7IK", out TextMeshProUGUI vibBadge);
        pauseMenu.vibrationBadge = vibBadge;
        pauseMenu.vibrationToggleButton = vibBtn.GetComponent<Button>();
        pauseMenu.vibrationLabel = vibBtn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();

        // RIGHT COLUMN
        GameObject right = new GameObject("RightColumn");
        right.transform.SetParent(grid.transform, false);
        right.AddComponent<RectTransform>();
        LayoutElement leR = right.AddComponent<LayoutElement>();
        leR.preferredWidth = 220;
        VerticalLayoutGroup vlgR = right.AddComponent<VerticalLayoutGroup>();
        vlgR.spacing = 8;
        vlgR.childForceExpandWidth = true;
        vlgR.childForceExpandHeight = false;
        vlgR.padding = new RectOffset(12, 0, 0, 0);

        GameObject diffLabel = CreateTMP(right, "DiffLabel", "ZORLUK SEV\u0130YES\u0130", 10, new Color(0.4f, 0.4f, 0.4f));
        diffLabel.GetComponent<TextMeshProUGUI>().characterSpacing = 2;
        diffLabel.AddComponent<LayoutElement>().preferredHeight = 22;

        GameObject easyBtn = CreateButton(right, "EasyBtn", "Kolay",
            new Color(0.67f, 0.67f, 0.67f), new Color(0.16f, 0.16f, 0.16f), 14, 50);
        pauseMenu.easyButton = easyBtn.GetComponent<Button>();

        GameObject normalBtn = CreateButton(right, "NormalBtn", "Normal",
            new Color(0.08f, 0.08f, 0.08f), new Color(0.9f, 0.72f, 0f), 14, 50);
        pauseMenu.normalButton = normalBtn.GetComponent<Button>();

        GameObject hardBtn = CreateButton(right, "HardBtn", "Zor",
            new Color(0.67f, 0.67f, 0.67f), new Color(0.16f, 0.16f, 0.16f), 14, 50);
        pauseMenu.hardButton = hardBtn.GetComponent<Button>();
    }

    GameObject CreateImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    GameObject CreateTMP(GameObject parent, string name, string text, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        return go;
    }

    GameObject CreateButton(GameObject parent, string name, string label, Color textColor, Color bgColor, float fontSize, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(bgColor.r * 1.2f, bgColor.g * 1.2f, bgColor.b * 1.2f);
        cb.pressedColor = new Color(bgColor.r * 0.85f, bgColor.g * 0.85f, bgColor.b * 0.85f);
        btn.colors = cb;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return go;
    }

    GameObject CreateToggleButton(GameObject parent, string name, string labelText, string badgeText, out TextMeshProUGUI badgeTMP)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f);
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().preferredHeight = 42;

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(0.6f, 1);
        lrt.offsetMin = new Vector2(12, 0); lrt.offsetMax = Vector2.zero;
        TextMeshProUGUI lTMP = labelGo.AddComponent<TextMeshProUGUI>();
        lTMP.text = labelText; lTMP.fontSize = 13; lTMP.color = Color.white;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft; lTMP.raycastTarget = false;

        GameObject badgeGo = new GameObject("Badge");
        badgeGo.transform.SetParent(go.transform, false);
        RectTransform brt = badgeGo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.62f, 0.2f); brt.anchorMax = new Vector2(0.98f, 0.8f);
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        badgeGo.AddComponent<Image>().color = new Color(0.23f, 0.23f, 0.23f);

        GameObject btGo = new GameObject("BadgeText");
        btGo.transform.SetParent(badgeGo.transform, false);
        RectTransform btrt = btGo.AddComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero; btrt.offsetMax = Vector2.zero;
        badgeTMP = btGo.AddComponent<TextMeshProUGUI>();
        badgeTMP.text = badgeText; badgeTMP.fontSize = 11;
        badgeTMP.color = new Color(0.67f, 0.67f, 0.67f);
        badgeTMP.alignment = TextAlignmentOptions.Center; badgeTMP.raycastTarget = false;

        return go;
    }

    void CreateDivider(GameObject parent, Color color)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        go.AddComponent<LayoutElement>().preferredHeight = 1;
    }
}
