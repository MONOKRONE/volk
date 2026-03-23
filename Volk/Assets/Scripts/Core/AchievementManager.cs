using UnityEngine;
using System;
using System.Collections.Generic;

namespace Volk.Core
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("All Achievements")]
        public AchievementData[] allAchievements;

        private Dictionary<AchievementCondition, int> progressCounters = new Dictionary<AchievementCondition, int>();

        public event Action<AchievementData> OnAchievementUnlocked;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }

        void LoadProgress()
        {
            foreach (AchievementCondition cond in Enum.GetValues(typeof(AchievementCondition)))
                progressCounters[cond] = PlayerPrefs.GetInt($"ach_progress_{cond}", 0);
        }

        public void ReportProgress(AchievementCondition condition, int amount = 1)
        {
            if (!progressCounters.ContainsKey(condition))
                progressCounters[condition] = 0;

            progressCounters[condition] += amount;
            PlayerPrefs.SetInt($"ach_progress_{condition}", progressCounters[condition]);
            PlayerPrefs.Save();

            CheckAchievements(condition);
        }

        public void SetProgress(AchievementCondition condition, int value)
        {
            progressCounters[condition] = Mathf.Max(progressCounters.GetValueOrDefault(condition, 0), value);
            PlayerPrefs.SetInt($"ach_progress_{condition}", progressCounters[condition]);
            PlayerPrefs.Save();

            CheckAchievements(condition);
        }

        void CheckAchievements(AchievementCondition condition)
        {
            if (allAchievements == null) return;

            foreach (var ach in allAchievements)
            {
                if (ach.condition != condition) continue;
                if (IsCompleted(ach.achievementId)) continue;

                int progress = GetProgress(ach.condition);
                if (progress >= ach.targetValue)
                    UnlockAchievement(ach);
            }
        }

        void UnlockAchievement(AchievementData ach)
        {
            if (SaveManager.Instance != null)
            {
                if (SaveManager.Instance.Data.completedAchievements.Contains(ach.achievementId)) return;
                SaveManager.Instance.Data.completedAchievements.Add(ach.achievementId);
                SaveManager.Instance.Save();
            }
            else
            {
                PlayerPrefs.SetInt($"ach_done_{ach.achievementId}", 1);
                PlayerPrefs.Save();
            }

            // Give rewards
            if (ach.coinReward > 0) CurrencyManager.Instance?.AddCoins(ach.coinReward);
            if (ach.gemReward > 0) CurrencyManager.Instance?.AddGems(ach.gemReward);
            if (ach.xpReward > 0) LevelSystem.Instance?.AddXP(ach.xpReward);

            OnAchievementUnlocked?.Invoke(ach);
            Debug.Log($"[Achievement] Unlocked: {ach.title}! +{ach.coinReward}c +{ach.gemReward}g +{ach.xpReward}xp");
        }

        public bool IsCompleted(string achievementId)
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.Data.completedAchievements.Contains(achievementId);
            return PlayerPrefs.GetInt($"ach_done_{achievementId}", 0) == 1;
        }

        public int GetProgress(AchievementCondition condition)
        {
            return progressCounters.GetValueOrDefault(condition, 0);
        }

        public int CompletedCount()
        {
            int count = 0;
            if (allAchievements == null) return 0;
            foreach (var ach in allAchievements)
                if (IsCompleted(ach.achievementId)) count++;
            return count;
        }

        public int TotalCount()
        {
            return allAchievements?.Length ?? 0;
        }
    }
}
