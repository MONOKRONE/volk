using UnityEngine;
using System;
using Volk.Core;

namespace Volk.Meta
{
    [Serializable]
    public class WeeklyEventData
    {
        public string eventName;
        public string description;
        public int weekNumber;
        public float enemyHPMultiplier = 1.5f;
        public float enemyDamageMultiplier = 1.3f;
        public AIDifficulty difficulty = AIDifficulty.Hard;
        public int coinReward = 500;
        public int gemReward = 20;
        public LootBoxTier rewardTier = LootBoxTier.Gold;
    }

    public class WeeklyEventManager : MonoBehaviour
    {
        public static WeeklyEventManager Instance { get; private set; }

        public WeeklyEventData CurrentEvent { get; private set; }
        public bool IsEventActive { get; private set; }
        public bool IsEventCompleted { get; private set; }

        // Rotating weekly themes
        private static readonly string[] eventNames = {
            "Celik Yumruk Turnuvasi",
            "Gece Dovusu",
            "Buz Krali Meydan Okumasi",
            "Yangin Savasi",
            "Golge Avcilar Gecesi"
        };

        private static readonly string[] eventDescs = {
            "Rakip celik zirh giyiyor! Ekstra dayanikli.",
            "Karanlikta dovsun. Dikkatli ol.",
            "Buz gibi soguk bir rakip. Yavastir ama guclu.",
            "Atesi bol bir rakip. Hizi yuksek.",
            "Golgelerin icinden gelen gizemli dovuscu."
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CheckWeeklyEvent();
        }

        void CheckWeeklyEvent()
        {
            int currentWeek = GetWeekNumber();
            int lastCompletedWeek = PlayerPrefs.GetInt("weekly_completed", -1);

            IsEventCompleted = lastCompletedWeek == currentWeek;
            IsEventActive = true;

            int themeIndex = currentWeek % eventNames.Length;
            CurrentEvent = new WeeklyEventData
            {
                eventName = eventNames[themeIndex],
                description = eventDescs[themeIndex],
                weekNumber = currentWeek,
                enemyHPMultiplier = 1.5f + (themeIndex * 0.1f),
                enemyDamageMultiplier = 1.2f + (themeIndex * 0.05f),
                coinReward = 500 + (themeIndex * 100),
                gemReward = 20 + (themeIndex * 5)
            };
        }

        public void CompleteEvent()
        {
            if (IsEventCompleted) return;

            IsEventCompleted = true;
            PlayerPrefs.SetInt("weekly_completed", GetWeekNumber());
            PlayerPrefs.Save();

            // Rewards
            CurrencyManager.Instance?.AddCoins(CurrentEvent.coinReward);
            CurrencyManager.Instance?.AddGems(CurrentEvent.gemReward);
            LootBoxManager.Instance?.OpenBox(CurrentEvent.rewardTier);

            Debug.Log($"[Weekly] Event completed! +{CurrentEvent.coinReward}c +{CurrentEvent.gemReward}g + Gold box!");
        }

        public void ApplyToEnemy(Fighter enemy)
        {
            if (enemy == null || CurrentEvent == null) return;
            enemy.maxHP *= CurrentEvent.enemyHPMultiplier;
            enemy.currentHP = enemy.maxHP;
            enemy.attackDamage *= CurrentEvent.enemyDamageMultiplier;
            enemy.difficulty = CurrentEvent.difficulty;
            enemy.InitAIDifficulty();
        }

        int GetWeekNumber()
        {
            return (int)(DateTime.UtcNow - new DateTime(2026, 1, 1)).TotalDays / 7;
        }

        public string GetTimeRemaining()
        {
            int currentWeek = GetWeekNumber();
            DateTime weekEnd = new DateTime(2026, 1, 1).AddDays((currentWeek + 1) * 7);
            TimeSpan remaining = weekEnd - DateTime.UtcNow;
            if (remaining.TotalHours < 0) return "Bitti";
            return $"{remaining.Days}g {remaining.Hours}s";
        }
    }
}
