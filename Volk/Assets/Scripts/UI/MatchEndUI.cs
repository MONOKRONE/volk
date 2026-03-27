using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class MatchEndUI : MonoBehaviour
    {
        [Header("Result")]
        public GameObject panel;
        public TextMeshProUGUI resultTitle; // ZAFER / YENILGI
        public TextMeshProUGUI gradeText;   // S, A, B, C, D
        public Image gradeGlow;

        [Header("Stats")]
        public TextMeshProUGUI hitsText;
        public TextMeshProUGUI damageText;
        public TextMeshProUGUI combosText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI scoreText;

        [Header("Rewards")]
        public TextMeshProUGUI coinRewardText;
        public TextMeshProUGUI xpRewardText;
        public Slider xpBar;
        public TextMeshProUGUI levelText;

        [Header("Buttons")]
        public Button rematchButton;
        public Button exitButton;
        public Button nextButton; // story mode only

        [Header("Grade Colors")]
        public Color gradeS = new Color(1f, 0.84f, 0f);
        public Color gradeA = new Color(0.91f, 0.27f, 0.38f);
        public Color gradeB = new Color(0f, 0.83f, 1f);
        public Color gradeC = new Color(0.5f, 0.5f, 0.5f);
        public Color gradeD = new Color(0.3f, 0.2f, 0.2f);

        void Start()
        {
            if (panel) panel.SetActive(false);
            if (rematchButton) rematchButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            if (exitButton) exitButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

        public void Show(MatchStats stats)
        {
            if (panel == null) return;
            panel.SetActive(true);
            StartCoroutine(AnimateResults(stats));
        }

        IEnumerator AnimateResults(MatchStats stats)
        {
            // Title
            if (resultTitle)
            {
                resultTitle.text = stats.playerWon ? "VICTORY" : "DEFEAT";
                resultTitle.color = stats.playerWon ? gradeS : gradeD;
            }

            yield return new WaitForSecondsRealtime(0.5f);

            // Grade reveal
            if (gradeText)
            {
                gradeText.text = stats.Grade;
                gradeText.color = GetGradeColor(stats.Grade);
                gradeText.transform.localScale = Vector3.zero;
                float t = 0;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    float scale = Mathf.Sin(t / 0.4f * Mathf.PI * 0.5f) * 1.2f;
                    gradeText.transform.localScale = Vector3.one * Mathf.Min(scale, 1f);
                    yield return null;
                }
                gradeText.transform.localScale = Vector3.one;
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // Stats
            if (hitsText) hitsText.text = $"Vurus: {stats.totalHitsLanded}";
            if (damageText) damageText.text = $"Damage: {stats.totalDamageDealt:F0}";
            if (combosText) combosText.text = $"Kombo: {stats.combosLanded} (Max: {stats.maxComboChain})";
            int min = (int)(stats.matchDuration / 60);
            int sec = (int)(stats.matchDuration % 60);
            if (timeText) timeText.text = $"Time: {min:D2}:{sec:D2}";
            if (scoreText) scoreText.text = $"Puan: {stats.CalculateScore():F0}/100";

            yield return new WaitForSecondsRealtime(0.3f);

            // Rewards
            int coins = stats.GetCoinReward();
            int xp = stats.GetXPReward();
            if (coinRewardText) coinRewardText.text = $"+{coins}";
            if (xpRewardText) xpRewardText.text = $"+{xp} XP";

            // XP bar fill animation
            if (xpBar && LevelSystem.Instance != null)
            {
                float startFill = LevelSystem.Instance.XPProgress;
                if (levelText) levelText.text = $"Lv.{LevelSystem.Instance.CurrentLevel}";

                float elapsed = 0;
                float targetFill = Mathf.Clamp01(startFill + (float)xp / LevelSystem.Instance.XPToNextLevel);
                while (elapsed < 1f)
                {
                    elapsed += Time.unscaledDeltaTime * 2f;
                    xpBar.value = Mathf.Lerp(startFill, targetFill, elapsed);
                    yield return null;
                }
            }
        }

        Color GetGradeColor(string grade)
        {
            return grade switch
            {
                "S" => gradeS,
                "A" => gradeA,
                "B" => gradeB,
                "C" => gradeC,
                _ => gradeD
            };
        }
    }
}
