using UnityEngine;
using System;

namespace Volk.Core
{
    [System.Serializable]
    public class BattlePassTier
    {
        public int tierNumber;
        public int xpRequired;       // Cumulative XP to reach this tier
        public string freeReward;     // e.g. "500 Coin", "50 Gem"
        public string premiumReward;  // e.g. "EpicSkin_YILDIZ", "HitEffect_Fire"
    }

    [CreateAssetMenu(fileName = "NewSeason", menuName = "VOLK/Battle Pass Season")]
    public class SeasonData : ScriptableObject
    {
        public string seasonName;
        public string startDate;  // "2026-04-01"
        public string endDate;    // "2026-05-27" (8 weeks)
        public BattlePassTier[] tiers;
    }

    public class BattlePassManager : MonoBehaviour
    {
        public static BattlePassManager Instance { get; private set; }

        [Header("Season")]
        public SeasonData currentSeason;

        public int CurrentXP { get; private set; }
        public int CurrentTier { get; private set; }
        public bool IsPremium { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadState();
        }

        void LoadState()
        {
            CurrentXP = PlayerPrefs.GetInt("bp_xp", 0);
            IsPremium = PlayerPrefs.GetInt("bp_premium", 0) == 1;
            RecalculateTier();
        }

        public void AddXP(int amount)
        {
            CurrentXP += amount;
            PlayerPrefs.SetInt("bp_xp", CurrentXP);

            int prevTier = CurrentTier;
            RecalculateTier();

            // Check new tier rewards
            if (CurrentTier > prevTier)
            {
                for (int t = prevTier + 1; t <= CurrentTier; t++)
                    ClaimTierReward(t);
            }

            PlayerPrefs.Save();
        }

        void RecalculateTier()
        {
            if (currentSeason == null || currentSeason.tiers == null)
            {
                CurrentTier = 0;
                return;
            }

            CurrentTier = 0;
            for (int i = 0; i < currentSeason.tiers.Length; i++)
            {
                if (CurrentXP >= currentSeason.tiers[i].xpRequired)
                    CurrentTier = currentSeason.tiers[i].tierNumber;
                else
                    break;
            }
        }

        void ClaimTierReward(int tier)
        {
            if (currentSeason == null || currentSeason.tiers == null) return;
            if (tier <= 0 || tier > currentSeason.tiers.Length) return;

            var t = currentSeason.tiers[tier - 1];
            if (!string.IsNullOrEmpty(t.freeReward))
                Debug.Log($"[BattlePass] Tier {tier} free reward: {t.freeReward}");
            if (IsPremium && !string.IsNullOrEmpty(t.premiumReward))
                Debug.Log($"[BattlePass] Tier {tier} premium reward: {t.premiumReward}");
        }

        public void ActivatePremium()
        {
            IsPremium = true;
            PlayerPrefs.SetInt("bp_premium", 1);
            PlayerPrefs.Save();
            Debug.Log("[BattlePass] Premium activated!");

            // Retroactively claim premium rewards for already-passed tiers
            for (int t = 1; t <= CurrentTier; t++)
            {
                var tier = currentSeason.tiers[t - 1];
                if (!string.IsNullOrEmpty(tier.premiumReward))
                    Debug.Log($"[BattlePass] Retroactive premium: Tier {t} → {tier.premiumReward}");
            }
        }

        public float GetTierProgress()
        {
            if (currentSeason == null || currentSeason.tiers == null || CurrentTier >= currentSeason.tiers.Length)
                return 1f;

            int currentReq = CurrentTier > 0 ? currentSeason.tiers[CurrentTier - 1].xpRequired : 0;
            int nextReq = currentSeason.tiers[CurrentTier].xpRequired;
            int range = nextReq - currentReq;
            if (range <= 0) return 1f;
            return (float)(CurrentXP - currentReq) / range;
        }

        public bool IsSeasonActive()
        {
            if (currentSeason == null) return false;
            if (DateTime.TryParse(currentSeason.startDate, out DateTime start) &&
                DateTime.TryParse(currentSeason.endDate, out DateTime end))
            {
                return DateTime.Now >= start && DateTime.Now <= end;
            }
            return true; // If no dates, always active
        }

        public void ResetSeason()
        {
            CurrentXP = 0;
            CurrentTier = 0;
            IsPremium = false;
            PlayerPrefs.SetInt("bp_xp", 0);
            PlayerPrefs.SetInt("bp_premium", 0);
            PlayerPrefs.Save();
        }

        // XP amounts for various activities
        public const int XP_STAGE_CLEAR = 100;
        public const int XP_TOWER_FLOOR = 50;
        public const int XP_DAILY_CHALLENGE = 200;
        public const int XP_PVP_WIN = 150;
        public const int XP_PVP_LOSS = 50;
    }
}
