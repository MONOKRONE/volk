using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Volk.Core;

namespace Volk.UI
{
    public class SurvivalHUD : MonoBehaviour
    {
        [Header("HUD Elements")]
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI difficultyText;
        public TextMeshProUGUI highScoreText;

        [Header("Game Over Panel")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI finalScoreText;
        public TextMeshProUGUI finalRoundText;
        public TextMeshProUGUI newHighScoreLabel;
        public Button retryButton;
        public Button exitButton;

        [Header("Round Break")]
        public GameObject roundBreakPanel;
        public TextMeshProUGUI nextRoundText;
        public TextMeshProUGUI hpRecoveryText;

        void Start()
        {
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (roundBreakPanel) roundBreakPanel.SetActive(false);

            if (retryButton) retryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            if (exitButton) exitButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

        void Update()
        {
            if (SurvivalManager.Instance == null || !SurvivalManager.Instance.IsActive) return;

            if (roundText) roundText.text = $"ROUND {SurvivalManager.Instance.CurrentRound}";
            if (scoreText) scoreText.text = $"{SurvivalManager.Instance.Score}";
            if (difficultyText) difficultyText.text = SurvivalManager.Instance.GetDifficultyLabel();
            if (highScoreText) highScoreText.text = $"BEST: {SurvivalManager.Instance.HighScore}";
        }

        public void ShowGameOver()
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);

            if (finalScoreText) finalScoreText.text = $"SCORE: {SurvivalManager.Instance.Score}";
            if (finalRoundText) finalRoundText.text = $"ROUND: {SurvivalManager.Instance.CurrentRound}";
            if (newHighScoreLabel) newHighScoreLabel.gameObject.SetActive(
                SurvivalManager.Instance.Score >= SurvivalManager.Instance.HighScore);
        }

        public void ShowRoundBreak(int nextRound, float recoveredHP)
        {
            if (roundBreakPanel == null) return;
            roundBreakPanel.SetActive(true);
            if (nextRoundText) nextRoundText.text = $"ROUND {nextRound}";
            if (hpRecoveryText) hpRecoveryText.text = $"+{recoveredHP:F0} HP";
            Invoke(nameof(HideRoundBreak), 2.5f);
        }

        void HideRoundBreak()
        {
            if (roundBreakPanel) roundBreakPanel.SetActive(false);
        }
    }
}
