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

            // Prefer SaveManager, fallback to PlayerPrefs
            if (SaveManager.Instance != null)
                return SaveManager.Instance.IsCharacterUnlocked(data.characterName);

            return PlayerPrefs.GetInt(GetKey(data), 0) == 1;
        }

        public bool TryUnlock(CharacterData data)
        {
            if (IsUnlocked(data)) return true;

            switch (data.unlockType)
            {
                case UnlockCondition.WinCount:
                    int wins = SaveManager.Instance != null ? SaveManager.Instance.Data.totalWins : PlayerPrefs.GetInt("total_wins", 0);
                    if (wins >= data.unlockValue) { Unlock(data); return true; }
                    break;

                case UnlockCondition.StoryProgress:
                    int chapter = SaveManager.Instance != null ? SaveManager.Instance.Data.completedChapter : PlayerPrefs.GetInt("completed_chapter", 0);
                    if (chapter >= data.unlockValue) { Unlock(data); return true; }
                    break;

                case UnlockCondition.Currency:
                    if (SaveManager.Instance != null)
                    {
                        if (SaveManager.Instance.SpendCurrency(data.unlockValue))
                        {
                            Unlock(data);
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public void Unlock(CharacterData data)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.UnlockCharacter(data.characterName);
            }
            else
            {
                PlayerPrefs.SetInt(GetKey(data), 1);
                PlayerPrefs.Save();
            }
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

            if (SaveManager.Instance != null)
            {
                return data.unlockType switch
                {
                    UnlockCondition.WinCount => (float)SaveManager.Instance.Data.totalWins / data.unlockValue,
                    UnlockCondition.StoryProgress => (float)SaveManager.Instance.Data.completedChapter / data.unlockValue,
                    UnlockCondition.Currency => (float)SaveManager.Instance.Data.currency / data.unlockValue,
                    _ => 0f
                };
            }

            return data.unlockType switch
            {
                UnlockCondition.WinCount => (float)PlayerPrefs.GetInt("total_wins", 0) / data.unlockValue,
                UnlockCondition.StoryProgress => (float)PlayerPrefs.GetInt("completed_chapter", 0) / data.unlockValue,
                UnlockCondition.Currency => (float)PlayerPrefs.GetInt("currency", 0) / data.unlockValue,
                _ => 0f
            };
        }

        /// <summary>
        /// Check all characters for story-based unlocks after completing a chapter.
        /// </summary>
        public void CheckStoryUnlocks(int completedChapter, CharacterData[] allCharacters)
        {
            if (allCharacters == null) return;
            foreach (var data in allCharacters)
            {
                if (data == null || data.unlockedByDefault || IsUnlocked(data)) continue;
                if (data.unlockType == UnlockCondition.StoryProgress && completedChapter >= data.unlockValue)
                {
                    Unlock(data);
                    Debug.Log($"[Unlock] Story progress: {data.characterName} unlocked at chapter {completedChapter}!");
                }
            }
        }

        string GetKey(CharacterData data) => $"char_unlocked_{data.characterName}";
    }
}
