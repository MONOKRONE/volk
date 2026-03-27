using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.UI
{
    public class RankedUI : MonoBehaviour
    {
        [Header("Navigation")]
        public Button backButton;
        public VTopBar topBar;

        [Header("League Display")]
        public TextMeshProUGUI leagueNameText;
        public Image leagueIcon;
        public Image leagueBadgeBG;
        public TextMeshProUGUI eloText;
        public Slider promoProgress;
        public Image promoFill;
        public TextMeshProUGUI promoLabel;

        [Header("Stats")]
        public TextMeshProUGUI winRateText;
        public TextMeshProUGUI winsText;
        public TextMeshProUGUI lossesText;
        public TextMeshProUGUI streakText;

        [Header("Match History")]
        public Transform matchHistoryParent;
        public GameObject matchEntryPrefab;

        [Header("Actions")]
        public Button findMatchButton;
        public TextMeshProUGUI findMatchLabel;

        [Header("Season")]
        public TextMeshProUGUI seasonText;
        public TextMeshProUGUI seasonTimerText;

        static readonly string[] LeagueNames = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master" };
        static readonly Color[] LeagueColors = {
            new Color(0.80f, 0.50f, 0.20f), // Bronze
            new Color(0.75f, 0.75f, 0.80f), // Silver
            new Color(1.00f, 0.84f, 0.00f), // Gold
            new Color(0.40f, 0.80f, 0.90f), // Platinum
            new Color(0.60f, 0.40f, 1.00f), // Diamond
            new Color(1.00f, 0.30f, 0.30f), // Master
        };
        static readonly int[] LeagueThresholds = { 0, 300, 600, 900, 1200, 1500 };

        void Start()
        {
            if (backButton)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            if (findMatchButton)
                findMatchButton.onClick.AddListener(OnFindMatch);

            Refresh();
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            int elo = PlayerPrefs.GetInt("ranked_elo", 1000);
            int wins = PlayerPrefs.GetInt("ranked_wins", 0);
            int losses = PlayerPrefs.GetInt("ranked_losses", 0);
            int streak = PlayerPrefs.GetInt("ranked_streak", 0);

            int leagueIdx = GetLeagueIndex(elo);
            Color leagueColor = LeagueColors[leagueIdx];

            // League display
            if (leagueNameText)
            {
                leagueNameText.text = LeagueNames[leagueIdx];
                leagueNameText.color = leagueColor;
            }
            if (eloText)
            {
                eloText.text = $"{elo} ELO";
                eloText.color = VTheme.TextPrimary;
            }
            if (leagueIcon)
                leagueIcon.color = leagueColor;
            if (leagueBadgeBG)
                leagueBadgeBG.color = new Color(leagueColor.r, leagueColor.g, leagueColor.b, 0.15f);

            // Promotion progress
            if (promoProgress)
            {
                int leagueFloor = LeagueThresholds[leagueIdx];
                int nextThreshold = leagueIdx < LeagueThresholds.Length - 1
                    ? LeagueThresholds[leagueIdx + 1] : leagueFloor + 300;
                float progress = (float)(elo - leagueFloor) / (nextThreshold - leagueFloor);
                promoProgress.value = Mathf.Clamp01(progress);
            }
            if (promoFill)
                promoFill.color = leagueColor;
            if (promoLabel)
            {
                int nextThreshold = leagueIdx < LeagueThresholds.Length - 1
                    ? LeagueThresholds[leagueIdx + 1] : elo;
                int remaining = Mathf.Max(0, nextThreshold - elo);
                promoLabel.text = remaining > 0 ? $"Terfi icin {remaining} ELO" : "MAX LIGA";
                promoLabel.color = VTheme.TextSecondary;
            }

            // Stats
            int total = wins + losses;
            if (winRateText)
            {
                float rate = total > 0 ? (float)wins / total * 100f : 0;
                winRateText.text = $"%{rate:F0}";
                winRateText.color = rate >= 50 ? VTheme.Green : VTheme.Red;
            }
            if (winsText)
            {
                winsText.text = $"{wins} G";
                winsText.color = VTheme.Green;
            }
            if (lossesText)
            {
                lossesText.text = $"{losses} M";
                lossesText.color = VTheme.Red;
            }
            if (streakText)
            {
                if (streak > 0)
                {
                    streakText.text = $"{streak} seri galibiyet";
                    streakText.color = VTheme.Gold;
                }
                else if (streak < 0)
                {
                    streakText.text = $"{-streak} seri maglub";
                    streakText.color = VTheme.Red;
                }
                else
                {
                    streakText.text = "";
                }
            }

            // Season
            UpdateSeason();

            // Match history
            PopulateMatchHistory();

            // Find match button
            if (findMatchLabel)
                findMatchLabel.text = "MAC BUL";
        }

        int GetLeagueIndex(int elo)
        {
            for (int i = LeagueThresholds.Length - 1; i >= 0; i--)
            {
                if (elo >= LeagueThresholds[i]) return i;
            }
            return 0;
        }

        void UpdateSeason()
        {
            int seasonNum = PlayerPrefs.GetInt("ranked_season", 1);
            if (seasonText) seasonText.text = $"Sezon {seasonNum}";

            if (seasonTimerText)
            {
                string endDate = PlayerPrefs.GetString("ranked_season_end", "2026-05-01");
                if (System.DateTime.TryParse(endDate, out var end))
                {
                    var remaining = end - System.DateTime.Now;
                    if (remaining.TotalDays > 0)
                    {
                        seasonTimerText.text = $"{(int)remaining.TotalDays}g {remaining.Hours}s kaldi";
                        seasonTimerText.color = remaining.TotalDays <= 7 ? VTheme.Red : VTheme.TextSecondary;
                    }
                    else
                    {
                        seasonTimerText.text = "Sezon bitti";
                        seasonTimerText.color = VTheme.Red;
                    }
                }
            }
        }

        // --- Match History ---

        void PopulateMatchHistory()
        {
            if (matchHistoryParent == null) return;

            foreach (Transform child in matchHistoryParent)
                Destroy(child.gameObject);

            var history = LoadMatchHistory();
            foreach (var match in history)
            {
                if (matchEntryPrefab != null)
                {
                    var entry = Instantiate(matchEntryPrefab, matchHistoryParent);
                    SetupMatchEntry(entry, match);
                }
                else
                {
                    CreateMatchEntryRuntime(match);
                }
            }
        }

        void SetupMatchEntry(GameObject entry, MatchRecord match)
        {
            var texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                texts[0].text = match.won ? "G" : "M";
                texts[0].color = match.won ? VTheme.Green : VTheme.Red;
            }
            if (texts.Length > 1) texts[1].text = match.opponent;
            if (texts.Length > 2)
            {
                string sign = match.eloDelta >= 0 ? "+" : "";
                texts[2].text = $"{sign}{match.eloDelta}";
                texts[2].color = match.eloDelta >= 0 ? VTheme.Green : VTheme.Red;
            }

            var bg = entry.GetComponent<Image>();
            if (bg != null)
            {
                Color c = match.won ? VTheme.Green : VTheme.Red;
                bg.color = new Color(c.r, c.g, c.b, 0.06f);
            }
        }

        void CreateMatchEntryRuntime(MatchRecord match)
        {
            var go = new GameObject("Match", typeof(RectTransform));
            go.transform.SetParent(matchHistoryParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            Color resultColor = match.won ? VTheme.Green : VTheme.Red;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(resultColor.r, resultColor.g, resultColor.b, 0.06f);
            bg.raycastTarget = false;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Result
            AddCell(go.transform, match.won ? "GAL" : "MAG", 60f, resultColor, FontStyles.Bold);

            // Opponent
            AddCell(go.transform, $"vs {match.opponent}", 0f, VTheme.TextPrimary, FontStyles.Normal);

            // Elo delta
            string sign = match.eloDelta >= 0 ? "+" : "";
            AddCell(go.transform, $"{sign}{match.eloDelta}", 80f,
                match.eloDelta >= 0 ? VTheme.Green : VTheme.Red, FontStyles.Bold);
        }

        void AddCell(Transform parent, string text, float width, Color color, FontStyles style)
        {
            var go = new GameObject("Cell", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            if (width > 0) { le.preferredWidth = width; le.minWidth = width; }
            else le.flexibleWidth = 1;
        }

        // --- Actions ---

        void OnFindMatch()
        {
            UIAudio.Instance?.PlayClick();
            GameSettings.Instance.currentMode = GameSettings.GameMode.Online;
            SceneManager.LoadScene("CombatTest");
        }

        // --- Match History Data ---

        struct MatchRecord
        {
            public bool won;
            public string opponent;
            public int eloDelta;
        }

        List<MatchRecord> LoadMatchHistory()
        {
            var list = new List<MatchRecord>();
            int count = PlayerPrefs.GetInt("ranked_history_count", 0);

            if (count == 0)
            {
                // Generate placeholder history for display
                return GeneratePlaceholderHistory();
            }

            for (int i = 0; i < Mathf.Min(count, 10); i++)
            {
                list.Add(new MatchRecord
                {
                    won = PlayerPrefs.GetInt($"ranked_h_{i}_won", 0) == 1,
                    opponent = PlayerPrefs.GetString($"ranked_h_{i}_opp", "???"),
                    eloDelta = PlayerPrefs.GetInt($"ranked_h_{i}_delta", 0)
                });
            }
            return list;
        }

        List<MatchRecord> GeneratePlaceholderHistory()
        {
            var list = new List<MatchRecord>();
            string[] names = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };
            for (int i = 0; i < 10; i++)
            {
                bool won = Random.value > 0.45f;
                list.Add(new MatchRecord
                {
                    won = won,
                    opponent = names[Random.Range(0, names.Length)],
                    eloDelta = won ? Random.Range(15, 35) : -Random.Range(12, 28)
                });
            }
            return list;
        }

        // Called externally after a ranked match to record result
        public static void RecordMatch(string opponent, bool won, int eloDelta)
        {
            int count = PlayerPrefs.GetInt("ranked_history_count", 0);

            // Shift history down
            for (int i = Mathf.Min(count, 9); i > 0; i--)
            {
                PlayerPrefs.SetInt($"ranked_h_{i}_won", PlayerPrefs.GetInt($"ranked_h_{i - 1}_won", 0));
                PlayerPrefs.SetString($"ranked_h_{i}_opp", PlayerPrefs.GetString($"ranked_h_{i - 1}_opp", ""));
                PlayerPrefs.SetInt($"ranked_h_{i}_delta", PlayerPrefs.GetInt($"ranked_h_{i - 1}_delta", 0));
            }

            // Insert new at index 0
            PlayerPrefs.SetInt("ranked_h_0_won", won ? 1 : 0);
            PlayerPrefs.SetString("ranked_h_0_opp", opponent);
            PlayerPrefs.SetInt("ranked_h_0_delta", eloDelta);
            PlayerPrefs.SetInt("ranked_history_count", Mathf.Min(count + 1, 10));

            // Update elo and stats
            int elo = PlayerPrefs.GetInt("ranked_elo", 1000) + eloDelta;
            PlayerPrefs.SetInt("ranked_elo", Mathf.Max(0, elo));
            if (won)
            {
                PlayerPrefs.SetInt("ranked_wins", PlayerPrefs.GetInt("ranked_wins", 0) + 1);
                int streak = PlayerPrefs.GetInt("ranked_streak", 0);
                PlayerPrefs.SetInt("ranked_streak", streak >= 0 ? streak + 1 : 1);
            }
            else
            {
                PlayerPrefs.SetInt("ranked_losses", PlayerPrefs.GetInt("ranked_losses", 0) + 1);
                int streak = PlayerPrefs.GetInt("ranked_streak", 0);
                PlayerPrefs.SetInt("ranked_streak", streak <= 0 ? streak - 1 : -1);
            }
            PlayerPrefs.Save();
        }
    }
}
