using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Volk.Core;

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

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (GameSettings.Instance == null || GameSettings.Instance.currentMode != GameSettings.GameMode.Survival)
                return;

            HighScore = PlayerPrefs.GetInt("survival_highscore", 0);
            StartSurvival();
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
            yield return new WaitForSeconds(roundBreakDuration);
            NextRound();
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

            // Save stats
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.totalMatches += CurrentRound;
                SaveManager.Instance.Save();
            }
        }

        public string GetDifficultyLabel()
        {
            if (CurrentRound <= 3) return "Kolay";
            if (CurrentRound <= 7) return "Normal";
            if (CurrentRound <= 12) return "Zor";
            return "Cehennem";
        }
    }
}
