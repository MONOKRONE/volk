using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class BattlePassUI : MonoBehaviour
    {
        [Header("Season Info")]
        public TextMeshProUGUI seasonNameText;
        public TextMeshProUGUI timeRemainingText;

        [Header("Progress")]
        public Slider xpProgressBar;
        public Image xpProgressFill;
        public TextMeshProUGUI tierText;
        public TextMeshProUGUI xpText;

        [Header("Tier Scroll View")]
        public ScrollRect tierScrollRect;
        public Transform tierListParent;
        public GameObject tierEntryPrefab;

        [Header("Premium")]
        public VButton purchasePremiumButton;
        public TextMeshProUGUI premiumPriceText;
        public GameObject premiumBadge;

        [Header("Colors")]
        public Color completedColor = new Color(0.18f, 0.80f, 0.44f, 1f);   // green
        public Color currentColor = new Color(1f, 0.84f, 0f, 1f);            // gold
        public Color lockedColor = new Color(0.37f, 0.37f, 0.50f, 1f);       // muted
        public Color premiumColor = new Color(0.61f, 0.35f, 0.71f, 1f);      // purple
        public Color freeColor = new Color(0f, 0.83f, 1f, 1f);               // blue

        void OnEnable() => Refresh();

        public void Refresh()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null || bp.currentSeason == null) return;

            // Season info
            if (seasonNameText) seasonNameText.text = bp.currentSeason.seasonName;
            UpdateTimeRemaining(bp);

            // Progress bar
            if (tierText) tierText.text = $"Tier {bp.CurrentTier} / {bp.currentSeason.tiers.Length}";
            if (xpText) xpText.text = $"{bp.CurrentXP} XP";
            if (xpProgressBar) xpProgressBar.value = bp.GetTierProgress();
            if (xpProgressFill) xpProgressFill.color = VTheme.Blue;

            // Premium
            UpdatePremiumState(bp);

            // Tier list
            PopulateTiers(bp);
        }

        void UpdateTimeRemaining(BattlePassManager bp)
        {
            if (timeRemainingText == null || bp.currentSeason.endDate == null) return;
            if (System.DateTime.TryParse(bp.currentSeason.endDate, out var end))
            {
                var remaining = end - System.DateTime.Now;
                timeRemainingText.text = remaining.TotalDays > 0
                    ? $"{(int)remaining.TotalDays}g {remaining.Hours}s kaldi"
                    : "Sezon bitti";
                timeRemainingText.color = remaining.TotalDays <= 7 ? VTheme.Red : VTheme.TextSecondary;
            }
        }

        void UpdatePremiumState(BattlePassManager bp)
        {
            if (premiumPriceText)
                premiumPriceText.text = bp.IsPremium ? "AKTIF" : "PREMIUM AL";

            if (purchasePremiumButton)
                purchasePremiumButton.SetInteractable(!bp.IsPremium);

            if (premiumBadge)
                premiumBadge.SetActive(bp.IsPremium);
        }

        void PopulateTiers(BattlePassManager bp)
        {
            if (tierListParent == null) return;

            // Clear existing
            foreach (Transform child in tierListParent)
                Destroy(child.gameObject);

            if (bp.currentSeason.tiers == null) return;

            for (int i = 0; i < bp.currentSeason.tiers.Length; i++)
            {
                var tier = bp.currentSeason.tiers[i];
                bool completed = bp.CurrentTier >= tier.tierNumber;
                bool isCurrent = bp.CurrentTier == tier.tierNumber - 1;

                if (tierEntryPrefab != null)
                {
                    var entry = Instantiate(tierEntryPrefab, tierListParent);
                    SetupTierEntry(entry, tier, completed, isCurrent, bp.IsPremium);
                }
                else
                {
                    CreateTierEntryRuntime(tier, completed, isCurrent, bp.IsPremium);
                }
            }

            // Scroll to current tier
            if (tierScrollRect != null && bp.currentSeason.tiers.Length > 0)
            {
                StartCoroutine(ScrollToCurrentTier(bp.CurrentTier, bp.currentSeason.tiers.Length));
            }
        }

        void SetupTierEntry(GameObject entry, BattlePassTier tier, bool completed, bool isCurrent, bool isPremium)
        {
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();

            // Tier number
            if (texts.Length > 0)
            {
                texts[0].text = tier.tierNumber.ToString();
                texts[0].color = completed ? completedColor : isCurrent ? currentColor : lockedColor;
            }

            // Free reward
            if (texts.Length > 1)
            {
                texts[1].text = tier.freeReward ?? "-";
                texts[1].color = completed ? VTheme.TextSecondary : freeColor;
            }

            // Premium reward
            if (texts.Length > 2)
            {
                texts[2].text = tier.premiumReward ?? "-";
                texts[2].color = completed && isPremium ? VTheme.TextSecondary
                    : isPremium ? premiumColor : lockedColor;
            }

            // Background highlight
            var bg = entry.GetComponent<Image>();
            if (bg != null)
            {
                if (isCurrent)
                    bg.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.15f);
                else if (completed)
                    bg.color = new Color(completedColor.r, completedColor.g, completedColor.b, 0.08f);
                else
                    bg.color = VTheme.Card;
            }

            // Completed checkmark
            var check = entry.transform.Find("Check");
            if (check != null)
                check.gameObject.SetActive(completed);

            // Lock icon for premium
            var lockIcon = entry.transform.Find("Lock");
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!isPremium && !string.IsNullOrEmpty(tier.premiumReward));
        }

        void CreateTierEntryRuntime(BattlePassTier tier, bool completed, bool isCurrent, bool isPremium)
        {
            var ui = RuntimeUIBuilder.Instance;
            if (ui == null) return;

            // Create row container
            var rowGO = new GameObject($"Tier_{tier.tierNumber}", typeof(RectTransform));
            rowGO.transform.SetParent(tierListParent, false);
            var rowRect = rowGO.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 70);

            var rowImg = rowGO.AddComponent<Image>();
            if (isCurrent)
                rowImg.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.15f);
            else if (completed)
                rowImg.color = new Color(completedColor.r, completedColor.g, completedColor.b, 0.08f);
            else
                rowImg.color = VTheme.Card;
            rowImg.raycastTarget = false;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Tier number
            var tierColor = completed ? completedColor : isCurrent ? currentColor : lockedColor;
            var numText = CreateCellText(rowGO.transform, tier.tierNumber.ToString(), tierColor, 80f, FontStyles.Bold);
            if (completed)
            {
                numText.text = "\u2713"; // checkmark
                numText.color = completedColor;
            }

            // Free reward
            CreateCellText(rowGO.transform, tier.freeReward ?? "-",
                completed ? VTheme.TextSecondary : freeColor, 0f, FontStyles.Normal);

            // Divider
            CreateDivider(rowGO.transform);

            // Premium reward
            Color premColor = !isPremium ? lockedColor : completed ? VTheme.TextSecondary : premiumColor;
            var premText = CreateCellText(rowGO.transform, tier.premiumReward ?? "-", premColor, 0f, FontStyles.Normal);
            if (!isPremium)
                premText.fontStyle = FontStyles.Italic;
        }

        TextMeshProUGUI CreateCellText(Transform parent, string text, Color color, float preferredWidth, FontStyles style)
        {
            var go = new GameObject("Cell", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;

            if (preferredWidth > 0)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = preferredWidth;
                le.minWidth = preferredWidth;
            }
            else
            {
                var le = go.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
            }

            return tmp;
        }

        void CreateDivider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = VTheme.TextMuted;
            img.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 2;
            le.minWidth = 2;
        }

        IEnumerator ScrollToCurrentTier(int currentTier, int totalTiers)
        {
            yield return null; // Wait for layout to build
            if (tierScrollRect == null || totalTiers <= 0) yield break;

            float targetNorm = 1f - ((float)currentTier / totalTiers);
            targetNorm = Mathf.Clamp01(targetNorm);
            tierScrollRect.verticalNormalizedPosition = targetNorm;
        }

        // Called by premium purchase button
        public void OnPurchasePremium()
        {
            BattlePassManager.Instance?.ActivatePremium();
            UIAudio.Instance?.PlayClick();
            Refresh();
        }
    }
}
