using UnityEngine;
using System.Collections.Generic;

namespace Volk.Core
{
    public enum CosmeticType
    {
        HitEffect,
        VictoryAnim,
        Outfit,
        ProfileBorder,
        ArenaSkin
    }

    public enum CosmeticRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "NewCosmetic", menuName = "VOLK/Cosmetic Item")]
    public class CosmeticItemData : ScriptableObject
    {
        public string itemId;
        public string itemName;
        public CosmeticType itemType;
        public CosmeticRarity rarity;
        public int gemPrice;
        public Sprite icon;
        public GameObject previewPrefab;
    }

    [System.Serializable]
    public class CosmeticInventoryData
    {
        public List<string> ownedItemIds = new List<string>();
        public List<string> equippedKeys = new List<string>();   // "HitEffect=item_id"
    }

    public class CosmeticManager : MonoBehaviour
    {
        public static CosmeticManager Instance { get; private set; }

        [Header("All Available Items")]
        public CosmeticItemData[] allItems;

        private HashSet<string> ownedItems = new HashSet<string>();
        private Dictionary<CosmeticType, string> equippedItems = new Dictionary<CosmeticType, string>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadInventory();
        }

        public bool IsOwned(string itemId) => ownedItems.Contains(itemId);

        public string GetEquipped(CosmeticType type)
        {
            equippedItems.TryGetValue(type, out string id);
            return id;
        }

        public bool Purchase(string itemId)
        {
            if (IsOwned(itemId)) return false;

            var item = FindItem(itemId);
            if (item == null) return false;

            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendGems(item.gemPrice))
                return false;

            ownedItems.Add(itemId);
            SaveInventory();
            Debug.Log($"[Cosmetic] Purchased: {item.itemName} ({item.rarity})");
            return true;
        }

        public void GrantItem(string itemId)
        {
            if (IsOwned(itemId)) return;
            ownedItems.Add(itemId);
            SaveInventory();
            Debug.Log($"[Cosmetic] Granted: {itemId}");
        }

        public bool Equip(string itemId)
        {
            if (!IsOwned(itemId)) return false;
            var item = FindItem(itemId);
            if (item == null) return false;

            equippedItems[item.itemType] = itemId;
            SaveInventory();
            Debug.Log($"[Cosmetic] Equipped: {item.itemName} ({item.itemType})");
            return true;
        }

        public void Unequip(CosmeticType type)
        {
            equippedItems.Remove(type);
            SaveInventory();
        }

        public List<CosmeticItemData> GetOwnedItemsOfType(CosmeticType type)
        {
            var result = new List<CosmeticItemData>();
            if (allItems == null) return result;
            foreach (var item in allItems)
                if (item != null && item.itemType == type && IsOwned(item.itemId))
                    result.Add(item);
            return result;
        }

        CosmeticItemData FindItem(string itemId)
        {
            if (allItems == null) return null;
            foreach (var item in allItems)
                if (item != null && item.itemId == itemId) return item;
            return null;
        }

        void SaveInventory()
        {
            var data = new CosmeticInventoryData();
            data.ownedItemIds = new List<string>(ownedItems);
            foreach (var kvp in equippedItems)
                data.equippedKeys.Add($"{kvp.Key}={kvp.Value}");

            PlayerPrefs.SetString("cosmetic_inv", JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        void LoadInventory()
        {
            string json = PlayerPrefs.GetString("cosmetic_inv", "");
            if (string.IsNullOrEmpty(json)) return;

            var data = JsonUtility.FromJson<CosmeticInventoryData>(json);
            if (data.ownedItemIds != null)
                ownedItems = new HashSet<string>(data.ownedItemIds);
            if (data.equippedKeys != null)
            {
                foreach (string entry in data.equippedKeys)
                {
                    int sep = entry.IndexOf('=');
                    if (sep <= 0) continue;
                    if (System.Enum.TryParse(entry.Substring(0, sep), out CosmeticType type))
                        equippedItems[type] = entry.Substring(sep + 1);
                }
            }
        }
    }
}
