using UnityEngine;
using System.Collections.Generic;
using Volk.Core;

namespace Volk.Meta
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        public ShopItemData[] shopItems;

        public event System.Action<ShopItemData> OnItemPurchased;
        public event System.Action<string> OnPurchaseFailed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool CanAfford(ShopItemData item)
        {
            return SaveManager.Instance != null && SaveManager.Instance.Data.currency >= item.price;
        }

        public bool IsOwned(ShopItemData item)
        {
            if (SaveManager.Instance == null) return false;
            var data = SaveManager.Instance.Data;

            if (item.itemType == ShopItemType.Character && item.linkedCharacter != null)
                return data.unlockedCharacters.Contains(item.linkedCharacter.characterName);

            return PlayerPrefs.GetInt($"shop_owned_{item.itemId}", 0) == 1;
        }

        public bool TryPurchase(ShopItemData item)
        {
            if (IsOwned(item))
            {
                OnPurchaseFailed?.Invoke("Zaten sahipsiniz");
                return false;
            }

            if (!CanAfford(item))
            {
                OnPurchaseFailed?.Invoke("Yetersiz bakiye");
                return false;
            }

            SaveManager.Instance.SpendCurrency(item.price);

            // Apply purchase
            switch (item.itemType)
            {
                case ShopItemType.Character:
                    if (item.linkedCharacter != null)
                        SaveManager.Instance.UnlockCharacter(item.linkedCharacter.characterName);
                    break;
                default:
                    PlayerPrefs.SetInt($"shop_owned_{item.itemId}", 1);
                    PlayerPrefs.Save();
                    break;
            }

            OnItemPurchased?.Invoke(item);
            Debug.Log($"[Shop] Purchased: {item.itemName} for {item.price} coins");
            return true;
        }

        public int GetPlayerCurrency()
        {
            return SaveManager.Instance?.Data.currency ?? 0;
        }
    }
}
