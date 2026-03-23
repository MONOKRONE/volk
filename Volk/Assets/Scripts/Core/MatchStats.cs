using UnityEngine;
using System;

namespace Volk.Core
{
    [Serializable]
    public class MatchStats
    {
        public int totalHitsLanded;
        public int totalHitsReceived;
        public float totalDamageDealt;
        public float totalDamageReceived;
        public int combosLanded;
        public int maxComboChain;
        public int skillsUsed;
        public int parriesSuccessful;
        public float matchDuration;
        public bool playerWon;
        public int roundsWon;
        public int roundsLost;
        public float remainingHPPercent;

        public string Grade
        {
            get
            {
                float score = CalculateScore();
                if (score >= 90f) return "S";
                if (score >= 75f) return "A";
                if (score >= 55f) return "B";
                if (score >= 35f) return "C";
                return "D";
            }
        }

        public float CalculateScore()
        {
            float score = 0;

            // Win bonus (40 pts max)
            if (playerWon) score += 40f;

            // HP remaining (20 pts max)
            score += remainingHPPercent * 20f;

            // Offense (20 pts max)
            float hitRatio = totalHitsReceived > 0
                ? (float)totalHitsLanded / (totalHitsLanded + totalHitsReceived)
                : 1f;
            score += hitRatio * 15f;

            // Combos (10 pts max)
            score += Mathf.Min(combosLanded * 2f, 10f);

            // Speed bonus (10 pts max) — under 60s = full, 180s+ = 0
            float speedBonus = Mathf.Clamp01(1f - (matchDuration - 60f) / 120f);
            score += speedBonus * 10f;

            // Parry bonus (5 pts)
            score += Mathf.Min(parriesSuccessful * 2.5f, 5f);

            return Mathf.Clamp(score, 0f, 100f);
        }

        public int GetCoinReward()
        {
            string grade = Grade;
            return grade switch
            {
                "S" => 200,
                "A" => 150,
                "B" => 100,
                "C" => 60,
                _ => 30
            };
        }

        public int GetXPReward()
        {
            string grade = Grade;
            return grade switch
            {
                "S" => 80,
                "A" => 60,
                "B" => 45,
                "C" => 30,
                _ => 15
            };
        }
    }

    public class MatchStatsTracker : MonoBehaviour
    {
        public static MatchStatsTracker Instance { get; private set; }

        public MatchStats Current { get; private set; } = new MatchStats();

        private float matchStartTime;
        private int currentComboChain;

        void Awake()
        {
            Instance = this;
            matchStartTime = Time.time;
        }

        public void RecordHitLanded(float damage)
        {
            Current.totalHitsLanded++;
            Current.totalDamageDealt += damage;
            currentComboChain++;
            if (currentComboChain > Current.maxComboChain)
                Current.maxComboChain = currentComboChain;
        }

        public void RecordHitReceived(float damage)
        {
            Current.totalHitsReceived++;
            Current.totalDamageReceived += damage;
            currentComboChain = 0; // combo broken
        }

        public void RecordCombo()
        {
            Current.combosLanded++;
        }

        public void RecordSkillUsed()
        {
            Current.skillsUsed++;
        }

        public void RecordParry()
        {
            Current.parriesSuccessful++;
        }

        public void RecordRoundResult(bool playerWon)
        {
            if (playerWon) Current.roundsWon++;
            else Current.roundsLost++;
        }

        public void FinalizeMatch(bool playerWon, float playerHPPercent)
        {
            Current.playerWon = playerWon;
            Current.remainingHPPercent = playerHPPercent;
            Current.matchDuration = Time.time - matchStartTime;
        }
    }

    // Adaptive AI difficulty adjuster
    public class AdaptiveDifficulty : MonoBehaviour
    {
        public static AdaptiveDifficulty Instance { get; private set; }

        [Header("Settings")]
        public float adjustmentSpeed = 0.1f;
        public float minScale = 0.5f;
        public float maxScale = 2.0f;

        public float DifficultyScale { get; private set; } = 1f;

        private int consecutiveWins;
        private int consecutiveLosses;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DifficultyScale = PlayerPrefs.GetFloat("adaptive_difficulty", 1f);
        }

        public void OnMatchResult(bool playerWon, float hpRemainingPercent)
        {
            if (playerWon)
            {
                consecutiveWins++;
                consecutiveLosses = 0;

                // Won easily? Increase difficulty
                if (hpRemainingPercent > 0.7f)
                    DifficultyScale += adjustmentSpeed * 1.5f;
                else if (hpRemainingPercent > 0.3f)
                    DifficultyScale += adjustmentSpeed * 0.5f;
                // Won barely? Small increase
                else
                    DifficultyScale += adjustmentSpeed * 0.2f;

                // Streak bonus
                if (consecutiveWins >= 3)
                    DifficultyScale += adjustmentSpeed;
            }
            else
            {
                consecutiveLosses++;
                consecutiveWins = 0;

                DifficultyScale -= adjustmentSpeed;

                // Losing streak? Ease up more
                if (consecutiveLosses >= 2)
                    DifficultyScale -= adjustmentSpeed;
            }

            DifficultyScale = Mathf.Clamp(DifficultyScale, minScale, maxScale);
            PlayerPrefs.SetFloat("adaptive_difficulty", DifficultyScale);
            PlayerPrefs.Save();

            Debug.Log($"[Adaptive] Scale: {DifficultyScale:F2} (W:{consecutiveWins} L:{consecutiveLosses})");
        }

        public void ApplyToFighter(Fighter enemy)
        {
            if (enemy == null) return;
            enemy.attackDamage *= DifficultyScale;
            enemy.maxHP *= DifficultyScale;
            enemy.currentHP = enemy.maxHP;
        }
    }
}
