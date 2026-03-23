using UnityEngine;
using System;

namespace Volk.Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public int Coins { get; private set; }
        public int Gems { get; private set; }

        public event Action<int, int> OnCoinsChanged; // newAmount, delta
        public event Action<int, int> OnGemsChanged;

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
                Coins = SaveManager.Instance.Data.currency;
                Gems = SaveManager.Instance.Data.gems;
            }
            else
            {
                Coins = PlayerPrefs.GetInt("currency", 0);
                Gems = PlayerPrefs.GetInt("gems", 0);
            }
        }

        public void AddCoins(int amount)
        {
            Coins += amount;
            Sync();
            OnCoinsChanged?.Invoke(Coins, amount);
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Sync();
            OnCoinsChanged?.Invoke(Coins, -amount);
            return true;
        }

        public void AddGems(int amount)
        {
            Gems += amount;
            Sync();
            OnGemsChanged?.Invoke(Gems, amount);
        }

        public bool SpendGems(int amount)
        {
            if (Gems < amount) return false;
            Gems -= amount;
            Sync();
            OnGemsChanged?.Invoke(Gems, -amount);
            return true;
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
