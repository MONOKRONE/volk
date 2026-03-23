using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.UI
{
    public class AchievementPopup : MonoBehaviour
    {
        public static AchievementPopup Instance { get; private set; }

        [Header("Popup")]
        public RectTransform popupRect;
        public CanvasGroup popupGroup;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI rewardText;
        public Image iconImage;
        public Image borderGlow;

        [Header("Animation")]
        public float slideDistance = 120f;
        public float displayDuration = 3f;

        private Queue<AchievementData> pendingPopups = new Queue<AchievementData>();
        private bool isShowing;

        void Awake()
        {
            Instance = this;
            if (popupGroup) popupGroup.alpha = 0;
        }

        void Start()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += QueuePopup;
        }

        void OnDestroy()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= QueuePopup;
        }

        void QueuePopup(AchievementData ach)
        {
            pendingPopups.Enqueue(ach);
            if (!isShowing)
                StartCoroutine(ShowNext());
        }

        IEnumerator ShowNext()
        {
            while (pendingPopups.Count > 0)
            {
                isShowing = true;
                var ach = pendingPopups.Dequeue();
                yield return ShowPopup(ach);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            isShowing = false;
        }

        IEnumerator ShowPopup(AchievementData ach)
        {
            if (titleText) titleText.text = ach.title;

            string rewards = "";
            if (ach.coinReward > 0) rewards += $"+{ach.coinReward}c ";
            if (ach.gemReward > 0) rewards += $"+{ach.gemReward}g ";
            if (ach.xpReward > 0) rewards += $"+{ach.xpReward}xp";
            if (rewardText) rewardText.text = rewards;

            if (iconImage && ach.icon) iconImage.sprite = ach.icon;
            if (borderGlow) borderGlow.color = VTheme.Gold;

            UIAudio.Instance?.PlayLevelUp();

            // Slide down
            if (popupRect != null)
            {
                Vector2 hiddenPos = popupRect.anchoredPosition + new Vector2(0, slideDistance);
                Vector2 showPos = popupRect.anchoredPosition;
                popupRect.anchoredPosition = hiddenPos;

                float t = 0;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    float ease = 1f - Mathf.Pow(1f - t / 0.4f, 3f);
                    popupRect.anchoredPosition = Vector2.Lerp(hiddenPos, showPos, ease);
                    if (popupGroup) popupGroup.alpha = ease;
                    yield return null;
                }
                popupRect.anchoredPosition = showPos;
            }
            if (popupGroup) popupGroup.alpha = 1;

            yield return new WaitForSecondsRealtime(displayDuration);

            // Slide up
            if (popupRect != null && popupGroup != null)
            {
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    popupGroup.alpha = 1 - (t / 0.3f);
                    yield return null;
                }
                popupGroup.alpha = 0;
            }
        }
    }
}
