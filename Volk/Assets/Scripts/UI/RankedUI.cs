using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Volk.UI
{
    public class RankedUI : MonoBehaviour
    {
        [Header("League Display")]
        public TextMeshProUGUI leagueNameText;
        public Image leagueIcon;
        public TextMeshProUGUI eloText;
        public Slider promoProgress;

        [Header("Match History")]
        public Transform matchHistoryParent;
        public TextMeshProUGUI winRateText;

        static readonly string[] LeagueNames = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" };
        static readonly Color[] LeagueColors = {
            new Color(0.8f, 0.5f, 0.2f), // Bronze
            new Color(0.75f, 0.75f, 0.8f), // Silver
            new Color(1f, 0.84f, 0f), // Gold
            new Color(0.4f, 0.8f, 0.9f), // Platinum
            new Color(0.6f, 0.4f, 1f), // Diamond
            new Color(1f, 0.3f, 0.3f), // Master
        };

        void OnEnable() => Refresh();

        public void Refresh()
        {
            int elo = PlayerPrefs.GetInt("ranked_elo", 1000);
            int wins = PlayerPrefs.GetInt("ranked_wins", 0);
            int losses = PlayerPrefs.GetInt("ranked_losses", 0);

            int leagueIdx = Mathf.Clamp(elo / 300, 0, LeagueNames.Length - 1);

            if (leagueNameText)
            {
                leagueNameText.text = LeagueNames[leagueIdx];
                leagueNameText.color = LeagueColors[leagueIdx];
            }
            if (eloText) eloText.text = $"{elo} ELO";
            if (promoProgress)
            {
                float inLeague = (elo % 300) / 300f;
                promoProgress.value = inLeague;
            }

            int total = wins + losses;
            if (winRateText)
                winRateText.text = total > 0 ? $"Kazan: {wins}/{total} (%{wins * 100 / total})" : "Henuz mac yok";
        }
    }
}
