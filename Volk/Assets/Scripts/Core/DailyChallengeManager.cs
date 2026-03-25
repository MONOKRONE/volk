using UnityEngine;
using System;

namespace Volk.Core
{
    public enum ChallengeType
    {
        StyleMastery,
        CharacterRoulette,
        Survivalist
    }

    [System.Serializable]
    public class DailyChallenge
    {
        public ChallengeType type;
        public string description;
        public string targetValue; // e.g. character name, count, etc.
        public bool completed;
    }

    public class DailyChallengeManager : MonoBehaviour
    {
        public static DailyChallengeManager Instance { get; private set; }

        public DailyChallenge[] TodayChallenges { get; private set; }
        public int Streak { get; private set; }

        static readonly string[] StyleDescriptions = {
            "10 Parry yap",
            "5 Skill kullan",
            "Cansiz (no heal) vurus yapma"
        };

        static readonly string[] CharacterNames = {
            "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK"
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOrGenerate();
        }

        void LoadOrGenerate()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString("daily_date", "");
            Streak = PlayerPrefs.GetInt("daily_streak", 0);

            if (lastDate == today)
            {
                // Load saved state
                TodayChallenges = new DailyChallenge[3];
                for (int i = 0; i < 3; i++)
                {
                    TodayChallenges[i] = new DailyChallenge
                    {
                        type = (ChallengeType)PlayerPrefs.GetInt($"daily_ch{i}_type", i),
                        description = PlayerPrefs.GetString($"daily_ch{i}_desc", ""),
                        targetValue = PlayerPrefs.GetString($"daily_ch{i}_target", ""),
                        completed = PlayerPrefs.GetInt($"daily_ch{i}_done", 0) == 1
                    };
                }
            }
            else
            {
                // Check streak
                if (lastDate == DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"))
                    Streak++;
                else
                    Streak = 0;

                GenerateToday();
                PlayerPrefs.SetString("daily_date", today);
                PlayerPrefs.SetInt("daily_streak", Streak);
                SaveChallenges();
            }
        }

        void GenerateToday()
        {
            int seed = DateTime.Today.GetHashCode() ^ SystemInfo.deviceUniqueIdentifier.GetHashCode();
            System.Random rng = new System.Random(seed);

            TodayChallenges = new DailyChallenge[3];

            // Challenge 1: StyleMastery
            int styleIdx = rng.Next(StyleDescriptions.Length);
            TodayChallenges[0] = new DailyChallenge
            {
                type = ChallengeType.StyleMastery,
                description = StyleDescriptions[styleIdx],
                targetValue = styleIdx.ToString(),
                completed = false
            };

            // Challenge 2: CharacterRoulette
            int charIdx = rng.Next(CharacterNames.Length);
            TodayChallenges[1] = new DailyChallenge
            {
                type = ChallengeType.CharacterRoulette,
                description = $"{CharacterNames[charIdx]} ile 3 kat gec",
                targetValue = CharacterNames[charIdx],
                completed = false
            };

            // Challenge 3: Survivalist
            TodayChallenges[2] = new DailyChallenge
            {
                type = ChallengeType.Survivalist,
                description = "10 tower kat iyilestirme kullanmadan",
                targetValue = "10",
                completed = false
            };
        }

        public void CompleteChallenge(int index)
        {
            if (index < 0 || index >= TodayChallenges.Length) return;
            if (TodayChallenges[index].completed) return;

            TodayChallenges[index].completed = true;
            CurrencyManager.Instance?.OnDailyChallengeComplete();

            // Check if all completed for streak bonus
            bool allDone = true;
            foreach (var ch in TodayChallenges)
                if (!ch.completed) { allDone = false; break; }

            if (allDone && Streak >= 3)
            {
                CurrencyManager.Instance?.AddDailyTokens(50);
                Debug.Log("[Daily] 3-day streak bonus: +50 tokens!");
            }

            SaveChallenges();
            Debug.Log($"[Daily] Challenge {index} completed!");
        }

        void SaveChallenges()
        {
            for (int i = 0; i < TodayChallenges.Length; i++)
            {
                PlayerPrefs.SetInt($"daily_ch{i}_type", (int)TodayChallenges[i].type);
                PlayerPrefs.SetString($"daily_ch{i}_desc", TodayChallenges[i].description);
                PlayerPrefs.SetString($"daily_ch{i}_target", TodayChallenges[i].targetValue);
                PlayerPrefs.SetInt($"daily_ch{i}_done", TodayChallenges[i].completed ? 1 : 0);
            }
            PlayerPrefs.Save();
        }
    }
}
