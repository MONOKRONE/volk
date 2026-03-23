using UnityEngine;

namespace Volk.Core
{
    public enum EquipmentSlot { Gloves, Boots, Guard, Headgear }
    public enum EquipmentRarity { Common, Rare, Epic, Legendary }

    [CreateAssetMenu(fileName = "NewEquipment", menuName = "VOLK/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public EquipmentSlot slot;
        public EquipmentRarity rarity;
        public Sprite icon;

        [Header("Stats")]
        public float baseStat = 5f;
        public int maxUpgradeLevel = 5;
        public int upgradeCostBase = 50;

        public float GetStatAtLevel(int level)
        {
            float rarityMult = rarity switch
            {
                EquipmentRarity.Common => 1f,
                EquipmentRarity.Rare => 1.5f,
                EquipmentRarity.Epic => 2f,
                EquipmentRarity.Legendary => 3f,
                _ => 1f
            };
            return baseStat * rarityMult * (1f + level * 0.2f);
        }

        public int GetUpgradeCost(int currentLevel)
        {
            return upgradeCostBase * (currentLevel + 1);
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                EquipmentRarity.Common => new Color(0.6f, 0.6f, 0.6f),
                EquipmentRarity.Rare => new Color(0.2f, 0.5f, 1f),
                EquipmentRarity.Epic => new Color(0.6f, 0.2f, 0.9f),
                EquipmentRarity.Legendary => new Color(1f, 0.84f, 0f),
                _ => Color.white
            };
        }
    }
}
