using UnityEngine;

namespace Volk.Core
{
    public enum ShopItemType { Character, Skin, Cosmetic }

    [CreateAssetMenu(fileName = "NewShopItem", menuName = "VOLK/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        public string itemName;
        [TextArea] public string description;
        public ShopItemType itemType;
        public int price;
        public Sprite icon;
        public CharacterData linkedCharacter; // for character unlocks
        public string itemId; // unique identifier for save
    }
}
