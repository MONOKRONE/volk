using UnityEngine;
using System;
using Volk.Meta;

namespace Volk.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "volk_save_data";
        public SaveData Data { get; private set; }

        public event Action OnSaveLoaded;
        public event Action OnSaveUpdated;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Save()
        {
            Data.lastSaveTime = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            OnSaveUpdated?.Invoke();
            SyncToCloud();
        }

        void SyncToCloud()
        {
            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsAuthenticated)
            {
                SupabaseManager.Instance.SaveToCloud(Data);
            }
        }

        public void LoadFromCloud(Action<bool> onComplete = null)
        {
            if (SupabaseManager.Instance == null || !SupabaseManager.Instance.IsAuthenticated)
            {
                onComplete?.Invoke(false);
                return;
            }

            SupabaseManager.Instance.LoadFromCloud(cloudData =>
            {
                if (cloudData != null)
                {
                    // Conflict resolution: latest timestamp wins
                    if (!string.IsNullOrEmpty(cloudData.lastSaveTime) &&
                        !string.IsNullOrEmpty(Data.lastSaveTime))
                    {
                        if (!DateTime.TryParse(cloudData.lastSaveTime, out var cloudTime) ||
                            !DateTime.TryParse(Data.lastSaveTime, out var localTime))
                        {
                            onComplete?.Invoke(false);
                            return;
                        }
                        if (cloudTime > localTime)
                        {
                            Data = cloudData;
                            string json = JsonUtility.ToJson(Data);
                            PlayerPrefs.SetString(SAVE_KEY, json);
                            PlayerPrefs.Save();
                            OnSaveLoaded?.Invoke();
                        }
                    }
                }
                onComplete?.Invoke(true);
            }, error => onComplete?.Invoke(false));
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                Data = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                Data = new SaveData();
                MigrateLegacyPlayerPrefs();
            }
            OnSaveLoaded?.Invoke();
        }

        public void ResetSave()
        {
            Data = new SaveData();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            OnSaveUpdated?.Invoke();
        }

        // One-time migration from scattered PlayerPrefs
        void MigrateLegacyPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("SoundEnabled"))
                Data.soundOn = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            if (PlayerPrefs.HasKey("VibrationEnabled"))
                Data.vibrationOn = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            if (PlayerPrefs.HasKey("AIDifficulty"))
                Data.difficulty = PlayerPrefs.GetInt("AIDifficulty", 1);
            if (PlayerPrefs.HasKey("total_wins"))
                Data.totalWins = PlayerPrefs.GetInt("total_wins", 0);
            if (PlayerPrefs.HasKey("completed_chapter"))
                Data.completedChapter = PlayerPrefs.GetInt("completed_chapter", 0);
            if (PlayerPrefs.HasKey("currency"))
                Data.currency = PlayerPrefs.GetInt("currency", 0);
            Save();
        }

        // Convenience methods
        public void AddWin()
        {
            Data.totalWins++;
            Data.totalMatches++;
            Save();
        }

        public void AddLoss()
        {
            Data.totalMatches++;
            Save();
        }

        public void AddCurrency(int amount)
        {
            Data.currency += amount;
            Save();
        }

        public bool SpendCurrency(int amount)
        {
            if (Data.currency < amount) return false;
            Data.currency -= amount;
            Save();
            return true;
        }

        public void CompleteChapter(int chapter)
        {
            if (chapter > Data.completedChapter)
            {
                Data.completedChapter = chapter;
                Save();
            }
        }

        public void UnlockCharacter(string characterName)
        {
            if (!Data.unlockedCharacters.Contains(characterName))
            {
                Data.unlockedCharacters.Add(characterName);
                Save();
            }
        }

        public bool IsCharacterUnlocked(string characterName)
        {
            return Data.unlockedCharacters.Contains(characterName);
        }
    }
}
