using UnityEngine;
using System;

namespace Volk.Core
{
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem Instance { get; private set; }

        [Header("XP Curve")]
        public int baseXPPerLevel = 100;
        public float xpScaleFactor = 1.5f;

        [Header("XP Rewards")]
        public int xpPerWin = 50;
        public int xpPerLoss = 15;
        public int xpPerChapter = 100;
        public int xpPerSurvivalRound = 20;

        [Header("Level Rewards")]
        public int coinsPerLevel = 50;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int XPToNextLevel => GetXPForLevel(CurrentLevel);

        public float XPProgress => XPToNextLevel > 0 ? (float)CurrentXP / XPToNextLevel : 0f;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXPGained;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromSave();
        }

        void LoadFromSave()
        {
            if (SaveManager.Instance != null)
            {
                CurrentLevel = Mathf.Max(1, PlayerPrefs.GetInt("player_level", 1));
                CurrentXP = PlayerPrefs.GetInt("player_xp", 0);
            }
        }

        public int GetXPForLevel(int level)
        {
            return Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(xpScaleFactor, level - 1));
        }

        public void AddXP(int amount)
        {
            CurrentXP += amount;
            OnXPGained?.Invoke(amount);
            Debug.Log($"[XP] +{amount} XP ({CurrentXP}/{XPToNextLevel})");

            while (CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                CurrentLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
                Debug.Log($"[XP] LEVEL UP! Now level {CurrentLevel}");

                // Level rewards
                if (SaveManager.Instance != null)
                    SaveManager.Instance.AddCurrency(coinsPerLevel + (CurrentLevel * 10));
            }

            SaveProgress();
        }

        public void AddMatchXP(bool won)
        {
            AddXP(won ? xpPerWin : xpPerLoss);
        }

        public void AddChapterXP()
        {
            AddXP(xpPerChapter);
        }

        public void AddSurvivalXP(int rounds)
        {
            AddXP(rounds * xpPerSurvivalRound);
        }

        void SaveProgress()
        {
            PlayerPrefs.SetInt("player_level", CurrentLevel);
            PlayerPrefs.SetInt("player_xp", CurrentXP);
            PlayerPrefs.Save();
        }

        public string GetLevelTitle()
        {
            if (CurrentLevel < 5) return "Cirak";
            if (CurrentLevel < 10) return "Dovuscu";
            if (CurrentLevel < 20) return "Savaşci";
            if (CurrentLevel < 35) return "Usta";
            if (CurrentLevel < 50) return "Efendi";
            return "Efsane";
        }
    }
}
