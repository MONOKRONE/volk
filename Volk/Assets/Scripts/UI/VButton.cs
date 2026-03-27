using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Volk.UI
{
    [RequireComponent(typeof(Button))]
    public class VButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Style")]
        public bool usePrimaryColor = true;
        public bool animateOnPress = true;

        private Button button;
        private Image image;
        private RectTransform rt;
        private Vector3 originalScale;
        private Coroutine animCoroutine;

        /// <summary>Pass-through to underlying Button.onClick for AddListener compatibility.</summary>
        public Button.ButtonClickedEvent onClick => button != null ? button.onClick : GetComponent<Button>()?.onClick;

        void Awake()
        {
            button = GetComponent<Button>();
            image = GetComponent<Image>();
            rt = GetComponent<RectTransform>();
            originalScale = rt.localScale;

            ApplyStyle();
        }

        void ApplyStyle()
        {
            if (image != null)
            {
                image.color = usePrimaryColor ? VTheme.ButtonPrimary : VTheme.ButtonSecondary;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable || !animateOnPress) return;
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PunchScale(VTheme.ButtonPunchScaleMin));

            // Hover color
            if (image != null && usePrimaryColor)
                image.color = VTheme.ButtonPrimaryHover;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!animateOnPress) return;
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PunchScaleReturn());

            // Play click sound
            UIAudio.Instance?.PlayClick();

            // Restore color
            if (image != null && usePrimaryColor)
                image.color = VTheme.ButtonPrimary;
        }

        IEnumerator PunchScale(float target)
        {
            float t = 0;
            Vector3 start = rt.localScale;
            Vector3 end = originalScale * target;
            while (t < VTheme.ButtonPunchScaleDuration)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(start, end, t / VTheme.ButtonPunchScaleDuration);
                yield return null;
            }
        }

        IEnumerator PunchScaleReturn()
        {
            // Overshoot then settle
            float t = 0;
            Vector3 start = rt.localScale;
            Vector3 overshoot = originalScale * VTheme.ButtonPunchScaleMax;
            float half = VTheme.ButtonPunchScaleDuration * 0.5f;

            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(start, overshoot, t / half);
                yield return null;
            }

            t = 0;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(overshoot, originalScale, t / half);
                yield return null;
            }
            rt.localScale = originalScale;
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            if (image != null)
                image.color = interactable
                    ? (usePrimaryColor ? VTheme.ButtonPrimary : VTheme.ButtonSecondary)
                    : VTheme.ButtonDisabled;
        }
    }
}
