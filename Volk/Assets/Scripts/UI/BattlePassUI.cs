using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class BattlePassUI : MonoBehaviour
    {
        [Header("Progress")]
        public Slider xpProgressBar;
        public TextMeshProUGUI tierText;
        public TextMeshProUGUI xpText;
        public TextMeshProUGUI seasonNameText;
        public TextMeshProUGUI timeRemainingText;

        [Header("Tier List")]
        public Transform tierListParent;
        public GameObject tierEntryPrefab;

        [Header("Purchase")]
        public VButton purchasePremiumButton;
        public TextMeshProUGUI premiumPriceText;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null || bp.currentSeason == null) return;

            if (seasonNameText) seasonNameText.text = bp.currentSeason.seasonName;
            if (tierText) tierText.text = $"Tier {bp.CurrentTier} / {bp.currentSeason.tiers.Length}";
            if (xpText) xpText.text = $"{bp.CurrentXP} XP";
            if (xpProgressBar) xpProgressBar.value = bp.GetTierProgress();
            if (premiumPriceText) premiumPriceText.text = bp.IsPremium ? "AKTIF" : "$9.99";

            if (timeRemainingText && bp.currentSeason.endDate != null)
            {
                if (System.DateTime.TryParse(bp.currentSeason.endDate, out var end))
                {
                    var remaining = end - System.DateTime.Now;
                    timeRemainingText.text = remaining.TotalDays > 0
                        ? $"{(int)remaining.TotalDays}g {remaining.Hours}s kaldi"
                        : "Sezon bitti";
                }
            }

            if (purchasePremiumButton != null)
                purchasePremiumButton.interactable = !bp.IsPremium;
        }

        public void OnPurchasePremium()
        {
            BattlePassManager.Instance?.ActivatePremium();
            Refresh();
        }
    }
}
