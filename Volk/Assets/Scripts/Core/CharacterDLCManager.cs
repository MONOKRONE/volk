using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewCharacterDLC", menuName = "VOLK/Character DLC")]
    public class CharacterDLCData : ScriptableObject
    {
        public string characterId;
        public string iapProductId; // e.g. "com.volk.char_dlc_storm"
        public float grindHoursRequired = 30f;
        public int grindMatchesRequired = 50;
    }

    public class CharacterDLCManager : MonoBehaviour
    {
        public static CharacterDLCManager Instance { get; private set; }

        [Header("DLC Characters")]
        public CharacterDLCData[] dlcCharacters;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsDLCCharacter(string characterId)
        {
            return FindDLC(characterId) != null;
        }

        public bool IsUnlocked(string characterId)
        {
            return PlayerPrefs.GetInt($"dlc_unlocked_{characterId}", 0) == 1;
        }

        public float GetGrindProgress(string characterId)
        {
            var dlc = FindDLC(characterId);
            if (dlc == null) return 0f;

            float hoursProg = dlc.grindHoursRequired > 0
                ? PlayerPrefs.GetFloat($"dlc_hours_{characterId}", 0f) / dlc.grindHoursRequired : 0f;
            float matchProg = dlc.grindMatchesRequired > 0
                ? (float)PlayerPrefs.GetInt($"dlc_matches_{characterId}", 0) / dlc.grindMatchesRequired : 0f;

            return Mathf.Max(hoursProg, matchProg);
        }

        public void RecordMatchPlayed(string characterId, float matchDurationMinutes)
        {
            if (IsUnlocked(characterId)) return;
            var dlc = FindDLC(characterId);
            if (dlc == null) return;

            // Track hours
            float hours = PlayerPrefs.GetFloat($"dlc_hours_{characterId}", 0f);
            hours += matchDurationMinutes / 60f;
            PlayerPrefs.SetFloat($"dlc_hours_{characterId}", hours);

            // Track matches
            int matches = PlayerPrefs.GetInt($"dlc_matches_{characterId}", 0) + 1;
            PlayerPrefs.SetInt($"dlc_matches_{characterId}", matches);
            PlayerPrefs.Save();

            CheckGrindUnlock(characterId);
        }

        public void CheckGrindUnlock(string characterId)
        {
            if (IsUnlocked(characterId)) return;
            if (GetGrindProgress(characterId) >= 1f)
                Unlock(characterId);
        }

        /// <summary>
        /// IAP purchase placeholder. In production, verify receipt server-side.
        /// </summary>
        public void PurchaseIAP(string characterId)
        {
            var dlc = FindDLC(characterId);
            if (dlc == null) return;

            Debug.Log($"[DLC] IAP purchase initiated: {dlc.iapProductId} (${3.99} placeholder)");
            // In production: call Unity IAP, verify receipt, then:
            Unlock(characterId);
        }

        void Unlock(string characterId)
        {
            PlayerPrefs.SetInt($"dlc_unlocked_{characterId}", 1);
            PlayerPrefs.Save();

            // Also unlock in CharacterUnlockManager if available
            if (CharacterUnlockManager.Instance != null && GameSettings.Instance?.allCharacters != null)
            {
                foreach (var charData in GameSettings.Instance.allCharacters)
                {
                    if (charData != null && charData.characterName == characterId)
                    {
                        CharacterUnlockManager.Instance.Unlock(charData);
                        break;
                    }
                }
            }

            Debug.Log($"[DLC] Character unlocked: {characterId}");
        }

        CharacterDLCData FindDLC(string characterId)
        {
            if (dlcCharacters == null) return null;
            foreach (var dlc in dlcCharacters)
                if (dlc != null && dlc.characterId == characterId) return dlc;
            return null;
        }
    }
}
