using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Volk.Core
{
    public class SurvivalManager : MonoBehaviour
    {
        public static SurvivalManager Instance { get; private set; }

        [Header("Settings")]
        public float hpRecoveryPercent = 0.3f;
        public float difficultyScalePerRound = 0.08f;
        public int baseScorePerWin = 100;
        public float roundBreakDuration = 3f;

        [Header("References")]
        public Fighter playerFighter;
        public Fighter enemyFighter;

        public int CurrentRound { get; private set; }
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public bool IsActive { get; private set; }

        private float currentDifficultyScale = 1f;
        private TMPro.TextMeshProUGUI roundLabel;
        private TMPro.TextMeshProUGUI scoreLabel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (GameSettings.Instance == null || GameSettings.Instance.currentMode != GameSettings.GameMode.Survival)
                return;

            playerFighter = GameObject.Find("Player_Root")?.GetComponent<Fighter>();
            enemyFighter = GameObject.Find("Enemy_Root")?.GetComponent<Fighter>();

            HighScore = PlayerPrefs.GetInt("survival_highscore", 0);
            StartSurvival();
            BuildSurvivalHUD();
        }

        void Update()
        {
            if (roundLabel != null) roundLabel.text = $"ROUND {CurrentRound}";
            if (scoreLabel != null) scoreLabel.text = $"{Score} PTS";
        }

        public void StartSurvival()
        {
            IsActive = true;
            CurrentRound = 0;
            Score = 0;
            currentDifficultyScale = 1f;
            NextRound();
        }

        public void NextRound()
        {
            CurrentRound++;
            currentDifficultyScale = 1f + (CurrentRound - 1) * difficultyScalePerRound;

            // Partial HP recovery for player
            if (playerFighter != null && CurrentRound > 1)
            {
                float recovery = playerFighter.maxHP * hpRecoveryPercent;
                playerFighter.currentHP = Mathf.Min(playerFighter.currentHP + recovery, playerFighter.maxHP);
            }

            // Reset and scale enemy
            if (enemyFighter != null)
            {
                enemyFighter.ResetForRound();

                // Scale difficulty
                if (CurrentRound <= 3)
                    enemyFighter.difficulty = AIDifficulty.Easy;
                else if (CurrentRound <= 7)
                    enemyFighter.difficulty = AIDifficulty.Normal;
                else
                    enemyFighter.difficulty = AIDifficulty.Hard;

                enemyFighter.InitAIDifficulty();

                // Scale enemy stats
                enemyFighter.maxHP = 100f * currentDifficultyScale;
                enemyFighter.currentHP = enemyFighter.maxHP;
                enemyFighter.attackDamage = 15f * Mathf.Min(currentDifficultyScale, 2.5f);
            }
        }

        public void OnEnemyDefeated()
        {
            int roundScore = Mathf.RoundToInt(baseScorePerWin * currentDifficultyScale);
            Score += roundScore;
            Debug.Log($"[Survival] Round {CurrentRound} won! +{roundScore} pts. Total: {Score}");

            StartCoroutine(RoundBreak());
        }

        IEnumerator RoundBreak()
        {
            ShowRoundBreakUI();
            yield return new WaitForSeconds(roundBreakDuration);
            HideRoundBreakUI();
            NextRound();
            if (GameManager.Instance != null) GameManager.Instance.roundActive = true;
        }

        public void OnPlayerDefeated()
        {
            IsActive = false;

            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt("survival_highscore", HighScore);
                PlayerPrefs.Save();
                Debug.Log($"[Survival] NEW HIGH SCORE: {HighScore}!");
            }

            Debug.Log($"[Survival] Game Over! Round: {CurrentRound}, Score: {Score}, High: {HighScore}");

            ShowGameOverUI();

            // XP reward
            LevelSystem.Instance?.AddSurvivalXP(CurrentRound);

            // Save stats
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.totalMatches += CurrentRound;
                SaveManager.Instance.Save();
            }
        }

        void BuildSurvivalHUD()
        {
            var ui = Volk.UI.RuntimeUIBuilder.Instance;
            if (ui == null) return;
            ui.ShowCanvas();
            ui.EnsureCanvas();
            var canvas = ui.CanvasRect;

            // Top-center: ROUND display
            roundLabel = ui.CreateText(canvas, "ROUND 1", 28, Volk.UI.RuntimeUIBuilder.White, TMPro.TextAlignmentOptions.Center);
            var rlRect = roundLabel.GetComponent<RectTransform>();
            rlRect.anchorMin = new Vector2(0.35f, 0.90f);
            rlRect.anchorMax = new Vector2(0.65f, 1f);
            rlRect.offsetMin = rlRect.offsetMax = Vector2.zero;

            // Top-right: score
            scoreLabel = ui.CreateText(canvas, "0 PTS", 22, Volk.UI.RuntimeUIBuilder.Gold, TMPro.TextAlignmentOptions.MidlineRight);
            var slRect = scoreLabel.GetComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0.65f, 0.90f);
            slRect.anchorMax = new Vector2(0.98f, 1f);
            slRect.offsetMin = slRect.offsetMax = Vector2.zero;
        }

        void ShowRoundBreakUI()
        {
            var ui = Volk.UI.RuntimeUIBuilder.Instance;
            if (ui == null) return;
            var canvas = ui.CanvasRect;
            var overlay = ui.CreatePanel(canvas, new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f),
                new Color(0, 0, 0, 0.8f));
            overlay.gameObject.name = "RoundBreakOverlay";
            ui.CreateText(overlay.transform, $"ROUND {CurrentRound + 1}", 48, Volk.UI.RuntimeUIBuilder.White, TMPro.TextAlignmentOptions.Center);
            float recoveredHP = playerFighter != null ? playerFighter.maxHP * hpRecoveryPercent : 0;
            ui.CreateText(overlay.transform, $"+{recoveredHP:F0} HP recovered", 24,
                Volk.UI.RuntimeUIBuilder.Green, TMPro.TextAlignmentOptions.Center);
        }

        void HideRoundBreakUI()
        {
            var overlay = GameObject.Find("RoundBreakOverlay");
            if (overlay) Destroy(overlay);
        }

        void ShowGameOverUI()
        {
            var ui = Volk.UI.RuntimeUIBuilder.Instance;
            if (ui == null) return;
            ui.ShowCanvas();
            ui.EnsureCanvas();
            ui.ClearUI();
            var canvas = ui.CanvasRect;

            ui.CreatePanel(canvas, Vector2.zero, Vector2.one, Volk.UI.RuntimeUIBuilder.BG);

            var title = ui.CreateText(canvas, "GAME OVER", 72, Volk.UI.RuntimeUIBuilder.Accent, TMPro.TextAlignmentOptions.Center);
            title.fontStyle = TMPro.FontStyles.Bold;
            var tr = title.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0.65f); tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;

            var scoreTxt = ui.CreateText(canvas, $"Score: {Score}", 36, Volk.UI.RuntimeUIBuilder.Gold, TMPro.TextAlignmentOptions.Center);
            var sr = scoreTxt.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0, 0.52f); sr.anchorMax = new Vector2(1, 0.64f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;

            var roundTxt = ui.CreateText(canvas, $"Round Reached: {CurrentRound}", 28, Volk.UI.RuntimeUIBuilder.White, TMPro.TextAlignmentOptions.Center);
            var rr = roundTxt.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0, 0.44f); rr.anchorMax = new Vector2(1, 0.52f);
            rr.offsetMin = rr.offsetMax = Vector2.zero;

            bool newRecord = Score >= HighScore && Score > 0;
            if (newRecord)
            {
                var nrTxt = ui.CreateText(canvas, "NEW HIGH SCORE!", 32, Volk.UI.RuntimeUIBuilder.Neon, TMPro.TextAlignmentOptions.Center);
                var nr = nrTxt.GetComponent<RectTransform>();
                nr.anchorMin = new Vector2(0, 0.36f); nr.anchorMax = new Vector2(1, 0.44f);
                nr.offsetMin = nr.offsetMax = Vector2.zero;
            }

            ui.CreateButton(canvas, "RETRY", Volk.UI.RuntimeUIBuilder.Accent, Color.white,
                new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.30f),
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

            ui.CreateButton(canvas, "MAIN MENU", Volk.UI.RuntimeUIBuilder.Panel, Color.white,
                new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.17f),
                () => SceneManager.LoadScene("MainMenu"));
        }

        public string GetDifficultyLabel()
        {
            if (CurrentRound <= 3) return "Easy";
            if (CurrentRound <= 7) return "Normal";
            if (CurrentRound <= 12) return "Hard";
            return "Hell";
        }
    }
}
