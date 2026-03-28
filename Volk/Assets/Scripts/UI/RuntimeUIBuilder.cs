using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

namespace Volk.UI
{
    public class RuntimeUIBuilder : MonoBehaviour
    {
        public static RuntimeUIBuilder Instance { get; private set; }

        // Color palette
        public static readonly Color BG = new Color(0.039f, 0.039f, 0.078f, 1f);         // #0A0A14
        public static readonly Color Panel = new Color(0.102f, 0.102f, 0.180f, 1f);       // #1A1A2E
        public static readonly Color Accent = new Color(0.914f, 0.271f, 0.376f, 1f);      // #E94560
        public static readonly Color Gold = new Color(1f, 0.843f, 0f, 1f);                // #FFD700
        public static readonly Color Neon = new Color(0f, 0.831f, 1f, 1f);                // #00D4FF
        public static readonly Color White = Color.white;
        public static readonly Color Gray = new Color(0.533f, 0.533f, 0.667f, 1f);        // #8888AA
        public static readonly Color Purple = new Color(0.608f, 0.349f, 0.714f, 1f);      // #9B59B6
        public static readonly Color Green = new Color(0.180f, 0.800f, 0.443f, 1f);       // #2ECC71

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private RectTransform _canvasRect;

        public Canvas UICanvas => _canvas;
        public RectTransform CanvasRect => _canvasRect;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void EnsureCanvas()
        {
            if (_canvas != null) return;

            var canvasGO = new GameObject("RuntimeCanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            _scaler = canvasGO.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            _canvasRect = canvasGO.GetComponent<RectTransform>();

            // EventSystem - check for existing
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.transform.SetParent(transform);
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }
        }

        public Image CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false; // Panels are decorative — don't block button clicks
            return img;
        }

        public TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Color color, TextAlignmentOptions anchor = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = anchor;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false; // Text should never block button clicks
            return tmp;
        }

        public Button CreateButton(Transform parent, string text, Color bgColor, Color textColor,
            Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true; // Button Image MUST receive raycasts

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button text — raycastTarget MUST be false so clicks pass through to button Image
            var txtGO = new GameObject("BtnText", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10, 5);
            txtRect.offsetMax = new Vector2(-10, -5);

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false; // CRITICAL: text must not block button clicks

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        public Image CreateImage(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Image", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public ScrollRect CreateScrollRect(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(parent, false);
            var viewRect = viewportGO.GetComponent<RectTransform>();
            viewRect.anchorMin = anchorMin;
            viewRect.anchorMax = anchorMax;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            var viewImg = viewportGO.AddComponent<Image>();
            viewImg.color = Color.clear;
            viewImg.raycastTarget = true; // Viewport MUST receive raycasts for scroll/touch
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect
            var scrollRect = viewportGO.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 30;

            return scrollRect;
        }

        public CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        // --- Animation Coroutines ---

        public IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            cg.alpha = 0;
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1;
        }

        public IEnumerator FadeOut(CanvasGroup cg, float duration)
        {
            float start = cg.alpha;
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(start, 0, t / duration);
                yield return null;
            }
            cg.alpha = 0;
        }

        public IEnumerator PunchScale(Transform target)
        {
            float duration = 0.15f;
            float t = 0;
            while (t < duration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, 1.15f, t / (duration * 0.5f));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            t = 0;
            while (t < duration * 0.5f)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.15f, 1f, t / (duration * 0.5f));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        public IEnumerator SlideIn(RectTransform rect, Vector2 from, float duration)
        {
            Vector2 to = rect.anchoredPosition;
            rect.anchoredPosition = from;
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f); // ease out cubic
                rect.anchoredPosition = Vector2.Lerp(from, to, ease);
                yield return null;
            }
            rect.anchoredPosition = to;
        }

        public IEnumerator ScaleOvershoot(Transform target, float from, float overshoot, float final_, float duration)
        {
            target.localScale = Vector3.one * from;
            float half = duration * 0.6f;
            float t = 0;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(from, overshoot, Mathf.Clamp01(t / half));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            t = 0;
            float remain = duration - half;
            while (t < remain)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(overshoot, final_, Mathf.Clamp01(t / remain));
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one * final_;
        }

        public void ClearUI()
        {
            if (_canvas == null) return;
            for (int i = _canvas.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_canvas.transform.GetChild(i).gameObject);
            }
        }

        public void HideCanvas()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void ShowCanvas()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(true);
        }

        // --- Utility ---

        public static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
