using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class PlayerProfileUI : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI titleText;
        public Slider xpBar;
        public Image xpFill;

        [Header("Stats")]
        public TextMeshProUGUI totalMatchesText;
        public TextMeshProUGUI totalWinsText;
        public TextMeshProUGUI winRateText;
        public TextMeshProUGUI favCharacterText;
        public TextMeshProUGUI survivalRecordText;
        public TextMeshProUGUI totalStarsText;

        [Header("Achievements")]
        public TextMeshProUGUI achievementCountText;
        public Slider achievementBar;

        [Header("Currency")]
        public TextMeshProUGUI coinsText;
        public TextMeshProUGUI gemsText;

        [Header("Navigation")]
        public Button backButton;
        public Button achievementsButton;
        public Button equipmentButton;
        public Image backgroundImage;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        void Start()
        {
            if (backgroundImage) backgroundImage.color = VTheme.Background;
            if (xpFill) xpFill.color = VTheme.Blue;
            if (backButton) backButton.onClick.AddListener(() => SceneManager.LoadScene("MainHub"));
            if (achievementsButton) achievementsButton.onClick.AddListener(() => SceneManager.LoadScene("Achievements"));
            if (equipmentButton) equipmentButton.onClick.AddListener(() => SceneManager.LoadScene("Equipment"));

            Refresh();
        }

        void Refresh()
        {
            // Level info
            if (LevelSystem.Instance != null)
            {
                if (levelText) levelText.text = $"Seviye {LevelSystem.Instance.CurrentLevel}";
                if (titleText) { titleText.text = LevelSystem.Instance.GetLevelTitle(); titleText.color = VTheme.Gold; }
                if (xpBar) xpBar.value = LevelSystem.Instance.XPProgress;
            }

            if (playerNameText) playerNameText.text = "VOLK";

            // Match stats
            if (SaveManager.Instance != null)
            {
                var data = SaveManager.Instance.Data;
                if (totalMatchesText) totalMatchesText.text = $"{data.totalMatches}";
                if (totalWinsText) totalWinsText.text = $"{data.totalWins}";
                float winRate = data.totalMatches > 0 ? (float)data.totalWins / data.totalMatches * 100f : 0;
                if (winRateText) winRateText.text = $"%{winRate:F0}";
                if (totalStarsText) totalStarsText.text = $"{data.totalStars}";
            }

            // Survival record
            int survivalRecord = PlayerPrefs.GetInt("survival_highscore", 0);
            if (survivalRecordText) survivalRecordText.text = $"{survivalRecord}";

            // Achievements
            if (AchievementManager.Instance != null)
            {
                int completed = AchievementManager.Instance.CompletedCount();
                int total = AchievementManager.Instance.TotalCount();
                if (achievementCountText) achievementCountText.text = $"{completed}/{total}";
                if (achievementBar && total > 0) achievementBar.value = (float)completed / total;
            }

            // Currency
            if (CurrencyManager.Instance != null)
            {
                if (coinsText) coinsText.text = $"{CurrencyManager.Instance.Coins}";
                if (gemsText) gemsText.text = $"{CurrencyManager.Instance.Gems}";
            }
        }
    }
}
