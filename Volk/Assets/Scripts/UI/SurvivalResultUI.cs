using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class SurvivalResultUI : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public CanvasGroup panelGroup;

        [Header("Result")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;
        public GameObject newRecordBanner;

        [Header("XP")]
        public Slider xpBar;
        public TextMeshProUGUI xpGainText;
        public TextMeshProUGUI levelText;

        [Header("Buttons")]
        public Button retryButton;
        public Button exitButton;

        void Start()
        {
            if (panel) panel.SetActive(false);
            if (newRecordBanner) newRecordBanner.SetActive(false);
            if (retryButton) retryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            if (exitButton) exitButton.onClick.AddListener(() => SceneManager.LoadScene("MainHub"));
        }

        public void Show(int round, int score, int highScore, bool isNewRecord)
        {
            if (panel == null) return;
            panel.SetActive(true);
            StartCoroutine(Animate(round, score, highScore, isNewRecord));
        }

        IEnumerator Animate(int round, int score, int highScore, bool isNewRecord)
        {
            if (panelGroup) panelGroup.alpha = 0;

            float t = 0;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                if (panelGroup) panelGroup.alpha = t / 0.4f;
                yield return null;
            }

            if (titleText) { titleText.text = "HAYATTA KALAMADINIZ"; titleText.color = VTheme.Red; }
            if (roundText) roundText.text = $"Round: {round}";
            if (scoreText) scoreText.text = $"Skor: {score}";
            if (highScoreText) highScoreText.text = $"En Yuksek: {highScore}";

            if (isNewRecord && newRecordBanner)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                newRecordBanner.SetActive(true);
                var bannerText = newRecordBanner.GetComponentInChildren<TextMeshProUGUI>();
                if (bannerText) { bannerText.text = "YENI REKOR!"; bannerText.color = VTheme.Gold; }
                UIAudio.Instance?.PlayLevelUp();
            }

            // XP
            if (LevelSystem.Instance != null && xpBar != null)
            {
                int xp = round * 20;
                if (xpGainText) xpGainText.text = $"+{xp} XP";
                if (levelText) levelText.text = $"Lv.{LevelSystem.Instance.CurrentLevel}";
                xpBar.value = LevelSystem.Instance.XPProgress;
            }
        }
    }
}
