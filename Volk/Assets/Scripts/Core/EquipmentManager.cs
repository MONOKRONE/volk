using UnityEngine;
using System;
using System.Collections.Generic;

namespace Volk.Core
{
    [Serializable]
    public class OwnedEquipment
    {
        public string itemId;
        public int upgradeLevel;
    }

    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        [Header("Equipment Database")]
        public EquipmentData[] allEquipment;

        public List<OwnedEquipment> Inventory { get; private set; } = new List<OwnedEquipment>();
        public Dictionary<EquipmentSlot, string> EquippedSlots { get; private set; } = new Dictionary<EquipmentSlot, string>();

        public event Action OnInventoryChanged;
        public event Action<EquipmentSlot> OnEquipmentChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory();
        }

        void LoadInventory()
        {
            string json = PlayerPrefs.GetString("equipment_inventory", "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
                if (wrapper != null)
                {
                    Inventory = wrapper.items ?? new List<OwnedEquipment>();
                    if (wrapper.equipped != null)
                    {
                        foreach (var e in wrapper.equipped)
                        {
                            var parts = e.Split(':');
                            if (parts.Length == 2 && Enum.TryParse<EquipmentSlot>(parts[0], out var slot))
                                EquippedSlots[slot] = parts[1];
                        }
                    }
                }
            }

            // Give starter equipment if empty
            if (Inventory.Count == 0)
                GiveStarterEquipment();
        }

        void GiveStarterEquipment()
        {
            foreach (var eq in allEquipment)
            {
                if (eq.rarity == EquipmentRarity.Common)
                    AddToInventory(eq.itemId);
            }
        }

        public void AddToInventory(string itemId)
        {
            if (Inventory.Exists(i => i.itemId == itemId)) return;
            Inventory.Add(new OwnedEquipment { itemId = itemId, upgradeLevel = 0 });
            SaveInventory();
            OnInventoryChanged?.Invoke();
        }

        public bool Equip(string itemId)
        {
            var data = GetEquipmentData(itemId);
            if (data == null) return false;
            if (!Inventory.Exists(i => i.itemId == itemId)) return false;

            EquippedSlots[data.slot] = itemId;
            SaveInventory();
            OnEquipmentChanged?.Invoke(data.slot);
            return true;
        }

        public void Unequip(EquipmentSlot slot)
        {
            EquippedSlots.Remove(slot);
            SaveInventory();
            OnEquipmentChanged?.Invoke(slot);
        }

        public bool Upgrade(string itemId)
        {
            var owned = Inventory.Find(i => i.itemId == itemId);
            if (owned == null) return false;

            var data = GetEquipmentData(itemId);
            if (data == null || owned.upgradeLevel >= data.maxUpgradeLevel) return false;

            int cost = data.GetUpgradeCost(owned.upgradeLevel);
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendCoins(cost)) return false;

            owned.upgradeLevel++;
            SaveInventory();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public EquipmentData GetEquipmentData(string itemId)
        {
            foreach (var eq in allEquipment)
                if (eq.itemId == itemId) return eq;
            return null;
        }

        public OwnedEquipment GetOwned(string itemId)
        {
            return Inventory.Find(i => i.itemId == itemId);
        }

        public string GetEquippedInSlot(EquipmentSlot slot)
        {
            return EquippedSlots.TryGetValue(slot, out var id) ? id : null;
        }

        /// <summary>
        /// Fuse 3 items of the same tier into the next tier.
        /// Returns the new item ID or null if fusion failed.
        /// </summary>
        public string FuseItems(string itemId1, string itemId2, string itemId3, int fusionCoinCost = 200)
        {
            var d1 = GetEquipmentData(itemId1);
            var d2 = GetEquipmentData(itemId2);
            var d3 = GetEquipmentData(itemId3);
            if (d1 == null || d2 == null || d3 == null) return null;
            if (d1.rarity != d2.rarity || d2.rarity != d3.rarity) return null;
            if (d1.rarity == EquipmentRarity.Legendary) return null;

            EquipmentRarity nextTier = d1.rarity + 1;

            // Find result BEFORE spending coins
            EquipmentData resultEquip = null;
            foreach (var eq in allEquipment)
            {
                if (eq.slot == d1.slot && eq.rarity == nextTier)
                { resultEquip = eq; break; }
            }
            if (resultEquip == null) return null; // No valid fusion target

            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendCoins(fusionCoinCost))
                return null;

            Inventory.RemoveAll(i => i.itemId == itemId1 || i.itemId == itemId2 || i.itemId == itemId3);
            AddToInventory(resultEquip.itemId);
            Debug.Log($"[Equipment] Fused 3x {d1.rarity} → {resultEquip.itemName} ({nextTier})");
            return resultEquip.itemId;
        }

        /// <summary>
        /// Get the equipped accessory special effect, if any.
        /// </summary>
        public EquipmentSpecialEffect GetActiveSpecialEffect()
        {
            string accessoryId = GetEquippedInSlot(EquipmentSlot.Accessory);
            if (accessoryId == null) return EquipmentSpecialEffect.None;
            var data = GetEquipmentData(accessoryId);
            return data?.specialEffect ?? EquipmentSpecialEffect.None;
        }

        /// <summary>
        /// Get headband cooldown reduction multiplier (0-1 range, e.g. 0.8 = 20% reduction).
        /// </summary>
        public float GetCooldownMultiplier()
        {
            string headbandId = GetEquippedInSlot(EquipmentSlot.Headband);
            if (headbandId == null) return 1f;
            var data = GetEquipmentData(headbandId);
            var owned = GetOwned(headbandId);
            if (data == null || owned == null) return 1f;
            float stat = data.GetStatAtLevel(owned.upgradeLevel);
            return Mathf.Clamp(1f - stat * 0.01f, 0.5f, 1f);
        }

        // Apply equipment bonuses to a fighter (PvE only — skip in PvP)
        public void ApplyBonuses(Fighter fighter, bool isPvP = false)
        {
            if (fighter == null || isPvP) return;

            foreach (var kvp in EquippedSlots)
            {
                var data = GetEquipmentData(kvp.Value);
                var owned = GetOwned(kvp.Value);
                if (data == null || owned == null) continue;

                float stat = data.GetStatAtLevel(owned.upgradeLevel);

                switch (data.slot)
                {
                    case EquipmentSlot.Gloves:
                        fighter.attackDamage += stat;
                        break;
                    case EquipmentSlot.Boots:
                        fighter.walkSpeed += stat * 0.1f;
                        fighter.attackDamage += stat * 0.5f; // kick bonus
                        break;
                    case EquipmentSlot.Chest:
                        fighter.maxHP += stat * 2f;
                        fighter.defense += stat * 0.1f;
                        break;
                    case EquipmentSlot.Headband:
                        // Skill cooldown reduction applied via multiplier
                        break;
                    case EquipmentSlot.Accessory:
                        // Special effects handled separately
                        break;
                }
            }

            fighter.currentHP = fighter.maxHP;
        }

        void SaveInventory()
        {
            var wrapper = new InventoryWrapper();
            wrapper.items = Inventory;
            wrapper.equipped = new List<string>();
            foreach (var kvp in EquippedSlots)
                wrapper.equipped.Add($"{kvp.Key}:{kvp.Value}");

            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString("equipment_inventory", json);
            PlayerPrefs.Save();
        }

        [Serializable]
        class InventoryWrapper
        {
            public List<OwnedEquipment> items = new List<OwnedEquipment>();
            public List<string> equipped = new List<string>();
        }
    }
}
