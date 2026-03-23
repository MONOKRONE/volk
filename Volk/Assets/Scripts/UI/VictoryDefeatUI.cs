using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Volk.Core;

namespace Volk.UI
{
    public class VictoryDefeatUI : MonoBehaviour
    {
        [Header("Main")]
        public GameObject panel;
        public CanvasGroup panelGroup;
        public TextMeshProUGUI resultTitle;
        public TextMeshProUGUI gradeText;

        [Header("Stats Grid")]
        public TextMeshProUGUI statHits;
        public TextMeshProUGUI statDamage;
        public TextMeshProUGUI statCombos;
        public TextMeshProUGUI statMaxChain;
        public TextMeshProUGUI statTime;
        public TextMeshProUGUI statParries;

        [Header("XP Section")]
        public Slider xpBar;
        public Image xpFill;
        public TextMeshProUGUI xpGainText;
        public TextMeshProUGUI levelText;
        public GameObject levelUpBanner;

        [Header("Coin Rain")]
        public TextMeshProUGUI coinRewardText;
        public RectTransform coinAnimTarget;
        public GameObject coinPrefab;
        public Transform coinSpawnArea;

        [Header("Buttons")]
        public Button continueButton;
        public Button rematchButton;
        public Button exitButton;

        private bool leveledUp;

        void Start()
        {
            if (panel) panel.SetActive(false);
            if (levelUpBanner) levelUpBanner.SetActive(false);
            if (xpFill) xpFill.color = VTheme.Blue;
        }

        public void ShowResult(MatchStats stats)
        {
            if (panel == null) return;
            panel.SetActive(true);
            StartCoroutine(AnimateResult(stats));
        }

        IEnumerator AnimateResult(MatchStats stats)
        {
            // Fade in
            if (panelGroup)
            {
                panelGroup.alpha = 0;
                float t = 0;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    panelGroup.alpha = t / 0.4f;
                    yield return null;
                }
            }

            // Title slam
            if (resultTitle)
            {
                resultTitle.text = stats.playerWon ? "ZAFER" : "YENILGI";
                resultTitle.color = stats.playerWon ? VTheme.Gold : VTheme.Red;
                resultTitle.transform.localScale = Vector3.one * 2f;
                float t = 0;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    float s = Mathf.Lerp(2f, 1f, Mathf.SmoothStep(0, 1, t / 0.3f));
                    resultTitle.transform.localScale = Vector3.one * s;
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // Grade
            if (gradeText)
            {
                gradeText.text = stats.Grade;
                gradeText.color = stats.Grade switch
                {
                    "S" => VTheme.Gold,
                    "A" => VTheme.Red,
                    "B" => VTheme.Blue,
                    "C" => VTheme.TextSecondary,
                    _ => VTheme.TextMuted
                };
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // Stats counter
            if (statHits) statHits.text = $"{stats.totalHitsLanded}";
            if (statDamage) statDamage.text = $"{stats.totalDamageDealt:F0}";
            if (statCombos) statCombos.text = $"{stats.combosLanded}";
            if (statMaxChain) statMaxChain.text = $"{stats.maxComboChain}";
            int min = (int)(stats.matchDuration / 60);
            int sec = (int)(stats.matchDuration % 60);
            if (statTime) statTime.text = $"{min:D2}:{sec:D2}";
            if (statParries) statParries.text = $"{stats.parriesSuccessful}";

            yield return new WaitForSecondsRealtime(0.3f);

            // XP bar fill
            int xpReward = stats.GetXPReward();
            if (xpGainText) xpGainText.text = $"+{xpReward} XP";

            if (xpBar && LevelSystem.Instance != null)
            {
                int oldLevel = LevelSystem.Instance.CurrentLevel;
                float startFill = LevelSystem.Instance.XPProgress;
                if (levelText) levelText.text = $"Lv.{oldLevel}";

                // Animate fill
                float targetFill = Mathf.Clamp01(startFill + (float)xpReward / LevelSystem.Instance.XPToNextLevel);
                float elapsed = 0;
                while (elapsed < 1.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    xpBar.value = Mathf.Lerp(startFill, targetFill, elapsed / 1.5f);
                    yield return null;
                }

                // Check level up
                if (LevelSystem.Instance.CurrentLevel > oldLevel && levelUpBanner)
                {
                    levelUpBanner.SetActive(true);
                    var bannerText = levelUpBanner.GetComponentInChildren<TextMeshProUGUI>();
                    if (bannerText) bannerText.text = $"SEVIYE {LevelSystem.Instance.CurrentLevel}!";
                    UIAudio.Instance?.PlayLevelUp();
                }
            }

            // Coin reward
            int coinReward = stats.GetCoinReward();
            if (coinRewardText)
            {
                coinRewardText.text = $"+{coinReward}";
                coinRewardText.color = VTheme.Gold;
            }
            UIAudio.Instance?.PlayCoin();

            // Spawn coin particles
            if (coinPrefab != null && coinSpawnArea != null)
            {
                for (int i = 0; i < Mathf.Min(coinReward / 10, 20); i++)
                {
                    var coin = Instantiate(coinPrefab, coinSpawnArea);
                    var rt = coin.GetComponent<RectTransform>();
                    if (rt)
                    {
                        rt.anchoredPosition = new Vector2(
                            Random.Range(-200f, 200f),
                            Random.Range(50f, 200f)
                        );
                    }
                    Destroy(coin, 2f);
                    if (i % 3 == 0) yield return new WaitForSecondsRealtime(0.05f);
                }
            }
        }
    }
}
