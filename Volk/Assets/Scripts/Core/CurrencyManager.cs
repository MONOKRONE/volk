using UnityEngine;
using System;

namespace Volk.Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public int Coins { get; private set; }
        public int Gems { get; private set; }
        public int DailyTokens { get; private set; }

        public event Action<int, int> OnCoinsChanged; // newAmount, delta
        public event Action<int, int> OnGemsChanged;
        public event Action<int, int> OnDailyTokensChanged;

        // Earn rates
        public const int STAGE_CLEAR_COINS = 50;
        public const int CHAPTER_CLEAR_GEMS = 15;
        public const int BOSS_KILL_GEMS = 50;
        public const int DAILY_CHALLENGE_TOKENS = 25;
        public const int TOWER_MILESTONE_GEMS = 10;

        private bool _initialized;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Loads currency from SaveManager (preferred) or PlayerPrefs fallback.
        /// Safe to call multiple times — only the first call loads data.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (SaveManager.Instance != null)
            {
                Coins = SaveManager.Instance.Data.currency;
                Gems = SaveManager.Instance.Data.gems;
            }
            else
            {
                Debug.LogWarning("[CurrencyManager] SaveManager not ready, falling back to PlayerPrefs");
                Coins = PlayerPrefs.GetInt("currency", 0);
                Gems = PlayerPrefs.GetInt("gems", 0);
            }
            DailyTokens = PlayerPrefs.GetInt("daily_tokens", 0);
        }

        public void AddCoins(int amount)
        {
            EnsureInitialized();
            Coins += amount;
            Sync();
            OnCoinsChanged?.Invoke(Coins, amount);
        }

        public bool SpendCoins(int amount)
        {
            EnsureInitialized();
            if (Coins < amount) return false;
            Coins -= amount;
            Sync();
            OnCoinsChanged?.Invoke(Coins, -amount);
            return true;
        }

        public void AddGems(int amount)
        {
            EnsureInitialized();
            Gems += amount;
            Sync();
            OnGemsChanged?.Invoke(Gems, amount);
        }

        public bool SpendGems(int amount)
        {
            EnsureInitialized();
            if (Gems < amount) return false;
            Gems -= amount;
            Sync();
            OnGemsChanged?.Invoke(Gems, -amount);
            return true;
        }

        public void AddDailyTokens(int amount)
        {
            EnsureInitialized();
            DailyTokens += amount;
            PlayerPrefs.SetInt("daily_tokens", DailyTokens);
            PlayerPrefs.Save();
            OnDailyTokensChanged?.Invoke(DailyTokens, amount);
        }

        public bool SpendDailyTokens(int amount)
        {
            EnsureInitialized();
            if (DailyTokens < amount) return false;
            DailyTokens -= amount;
            PlayerPrefs.SetInt("daily_tokens", DailyTokens);
            PlayerPrefs.Save();
            OnDailyTokensChanged?.Invoke(DailyTokens, -amount);
            return true;
        }

        // --- Game Event Handlers ---

        public void OnStageClear()
        {
            AddCoins(STAGE_CLEAR_COINS);
            BattlePassManager.Instance?.AddXP(BattlePassManager.XP_STAGE_CLEAR);
            Debug.Log($"[Currency] Stage clear: +{STAGE_CLEAR_COINS} coins");
        }

        public void OnChapterClear()
        {
            AddGems(CHAPTER_CLEAR_GEMS);
            Debug.Log($"[Currency] Chapter clear: +{CHAPTER_CLEAR_GEMS} gems");
        }

        public void OnBossKill()
        {
            AddGems(BOSS_KILL_GEMS);
            Debug.Log($"[Currency] Boss kill: +{BOSS_KILL_GEMS} gems");
        }

        public void OnDailyChallengeComplete()
        {
            AddDailyTokens(DAILY_CHALLENGE_TOKENS);
            BattlePassManager.Instance?.AddXP(BattlePassManager.XP_DAILY_CHALLENGE);
            Debug.Log($"[Currency] Daily challenge: +{DAILY_CHALLENGE_TOKENS} tokens");
        }

        public void OnTowerMilestone()
        {
            AddGems(TOWER_MILESTONE_GEMS);
            Debug.Log($"[Currency] Tower milestone: +{TOWER_MILESTONE_GEMS} gems");
        }

        void Sync()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.currency = Coins;
                SaveManager.Instance.Data.gems = Gems;
                SaveManager.Instance.Save();
            }
            else
            {
                PlayerPrefs.SetInt("currency", Coins);
                PlayerPrefs.SetInt("gems", Gems);
                PlayerPrefs.Save();
            }
        }
    }
}
