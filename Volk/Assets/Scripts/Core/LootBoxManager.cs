using UnityEngine;
using System;
using System.Collections.Generic;

namespace Volk.Core
{
    public enum LootBoxTier { Bronze, Silver, Gold }

    [Serializable]
    public class LootBoxResult
    {
        public string itemId;
        public EquipmentRarity rarity;
        public bool isNew;
    }

    public class LootBoxManager : MonoBehaviour
    {
        public static LootBoxManager Instance { get; private set; }

        [Header("Equipment Pool")]
        public EquipmentData[] equipmentPool;

        public event Action<LootBoxTier, LootBoxResult> OnBoxOpened;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public LootBoxResult OpenBox(LootBoxTier tier)
        {
            EquipmentRarity rarity = RollRarity(tier);
            var candidates = GetCandidatesByRarity(rarity);

            // Fallback to Common if no items of rolled rarity
            if (candidates.Count == 0)
                candidates = GetCandidatesByRarity(EquipmentRarity.Common);
            if (candidates.Count == 0) return null;

            var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            bool isNew = !EquipmentManager.Instance.Inventory.Exists(i => i.itemId == chosen.itemId);

            if (isNew)
                EquipmentManager.Instance?.AddToInventory(chosen.itemId);
            else
                CurrencyManager.Instance?.AddCoins(GetDuplicateCoins(chosen.rarity));

            var result = new LootBoxResult
            {
                itemId = chosen.itemId,
                rarity = chosen.rarity,
                isNew = isNew
            };

            OnBoxOpened?.Invoke(tier, result);
            Debug.Log($"[Loot] {tier} box: {chosen.itemName} ({chosen.rarity}) {(isNew ? "NEW" : "DUPLICATE")}");
            return result;
        }

        EquipmentRarity RollRarity(LootBoxTier tier)
        {
            float roll = UnityEngine.Random.value * 100f;

            return tier switch
            {
                LootBoxTier.Bronze => roll < 80 ? EquipmentRarity.Common
                    : roll < 98 ? EquipmentRarity.Rare
                    : EquipmentRarity.Epic,

                LootBoxTier.Silver => roll < 50 ? EquipmentRarity.Common
                    : roll < 90 ? EquipmentRarity.Rare
                    : roll < 99 ? EquipmentRarity.Epic
                    : EquipmentRarity.Legendary,

                LootBoxTier.Gold => roll < 20 ? EquipmentRarity.Common
                    : roll < 60 ? EquipmentRarity.Rare
                    : roll < 90 ? EquipmentRarity.Epic
                    : EquipmentRarity.Legendary,

                _ => EquipmentRarity.Common
            };
        }

        List<EquipmentData> GetCandidatesByRarity(EquipmentRarity rarity)
        {
            var list = new List<EquipmentData>();
            if (equipmentPool == null) return list;
            foreach (var eq in equipmentPool)
                if (eq.rarity == rarity) list.Add(eq);
            return list;
        }

        int GetDuplicateCoins(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 20,
                EquipmentRarity.Rare => 50,
                EquipmentRarity.Epic => 100,
                EquipmentRarity.Legendary => 250,
                _ => 20
            };
        }

        // Determine box reward based on context
        public LootBoxTier? GetMatchRewardTier(bool won, int stars, bool isBoss)
        {
            if (!won) return null;
            if (isBoss) return LootBoxTier.Gold;
            if (stars >= 3) return LootBoxTier.Silver;
            return LootBoxTier.Bronze;
        }

        public LootBoxTier? GetSurvivalRewardTier(int round)
        {
            if (round > 0 && round % 5 == 0) return LootBoxTier.Bronze;
            return null;
        }
    }
}
