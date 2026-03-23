using UnityEngine;

namespace Volk.Core
{
    public class CharacterUnlockManager : MonoBehaviour
    {
        public static CharacterUnlockManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsUnlocked(CharacterData data)
        {
            if (data.unlockedByDefault) return true;
            return PlayerPrefs.GetInt(GetKey(data), 0) == 1;
        }

        public bool TryUnlock(CharacterData data)
        {
            if (IsUnlocked(data)) return true;

            switch (data.unlockType)
            {
                case UnlockCondition.WinCount:
                    int wins = PlayerPrefs.GetInt("total_wins", 0);
                    if (wins >= data.unlockValue) { Unlock(data); return true; }
                    break;

                case UnlockCondition.StoryProgress:
                    int chapter = PlayerPrefs.GetInt("completed_chapter", 0);
                    if (chapter >= data.unlockValue) { Unlock(data); return true; }
                    break;

                case UnlockCondition.Currency:
                    int coins = PlayerPrefs.GetInt("currency", 0);
                    if (coins >= data.unlockValue)
                    {
                        PlayerPrefs.SetInt("currency", coins - data.unlockValue);
                        Unlock(data);
                        return true;
                    }
                    break;
            }
            return false;
        }

        public void Unlock(CharacterData data)
        {
            PlayerPrefs.SetInt(GetKey(data), 1);
            PlayerPrefs.Save();
            Debug.Log($"[Unlock] {data.characterName} unlocked!");
        }

        public string GetUnlockDescription(CharacterData data)
        {
            return data.unlockType switch
            {
                UnlockCondition.StoryProgress => $"Chapter {data.unlockValue} tamamla",
                UnlockCondition.WinCount => $"{data.unlockValue} mac kazan",
                UnlockCondition.Currency => $"{data.unlockValue} coin gerekli",
                _ => "Kilitli"
            };
        }

        public float GetUnlockProgress(CharacterData data)
        {
            if (data.unlockValue <= 0) return 1f;
            return data.unlockType switch
            {
                UnlockCondition.WinCount => (float)PlayerPrefs.GetInt("total_wins", 0) / data.unlockValue,
                UnlockCondition.StoryProgress => (float)PlayerPrefs.GetInt("completed_chapter", 0) / data.unlockValue,
                UnlockCondition.Currency => (float)PlayerPrefs.GetInt("currency", 0) / data.unlockValue,
                _ => 0f
            };
        }

        string GetKey(CharacterData data) => $"char_unlocked_{data.characterName}";
    }
}
