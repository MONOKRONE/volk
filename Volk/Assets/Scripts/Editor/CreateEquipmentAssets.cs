using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateEquipmentAssets
{
    [MenuItem("VOLK/Create Equipment Assets")]
    static void Create()
    {
        // Starter Common items
        CreateItem("starter_gloves", "Training Gloves", EquipmentSlot.Gloves, EquipmentRarity.Common, 3f, "Simple but effective.");
        CreateItem("starter_boots", "Street Shoes", EquipmentSlot.Boots, EquipmentRarity.Common, 2f, "Light and comfortable.");
        CreateItem("starter_guard", "Cloth Bandage", EquipmentSlot.Guard, EquipmentRarity.Common, 3f, "Basic protection.");
        CreateItem("starter_headgear", "Cotton Tape", EquipmentSlot.Headgear, EquipmentRarity.Common, 2f, "Forehead guard.");

        // Rare items
        CreateItem("rare_gloves", "Steel Fist", EquipmentSlot.Gloves, EquipmentRarity.Rare, 5f, "Metal-reinforced gloves.");
        CreateItem("rare_boots", "Speed Boots", EquipmentSlot.Boots, EquipmentRarity.Rare, 4f, "Ultra-light material.");
        CreateItem("rare_guard", "Leather Vest", EquipmentSlot.Guard, EquipmentRarity.Rare, 5f, "Durable leather protection.");
        CreateItem("rare_headgear", "Boxing Helmet", EquipmentSlot.Headgear, EquipmentRarity.Rare, 4f, "Professional protection.");

        // Epic items
        CreateItem("epic_gloves", "Dragon Claw", EquipmentSlot.Gloves, EquipmentRarity.Epic, 7f, "Legendary power.");
        CreateItem("epic_boots", "Wind Step", EquipmentSlot.Boots, EquipmentRarity.Epic, 6f, "Invisible speed.");
        CreateItem("epic_guard", "Titanium Armor", EquipmentSlot.Guard, EquipmentRarity.Epic, 7f, "Unbreakable defense.");
        CreateItem("epic_headgear", "Wolf Mask", EquipmentSlot.Headgear, EquipmentRarity.Epic, 6f, "Symbol of VOLK.");

        // Legendary items
        CreateItem("legend_gloves", "Sultan's Fist", EquipmentSlot.Gloves, EquipmentRarity.Legendary, 10f, "Istanbul's most powerful weapon.");
        CreateItem("legend_guard", "Bosphorus Shield", EquipmentSlot.Guard, EquipmentRarity.Legendary, 10f, "Invincible defense.");

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 14 equipment assets created!");
    }

    static void CreateItem(string id, string name, EquipmentSlot slot, EquipmentRarity rarity, float stat, string desc)
    {
        var item = ScriptableObject.CreateInstance<EquipmentData>();
        item.itemId = id;
        item.itemName = name;
        item.description = desc;
        item.slot = slot;
        item.rarity = rarity;
        item.baseStat = stat;
        item.maxUpgradeLevel = rarity == EquipmentRarity.Legendary ? 10 : 5;
        item.upgradeCostBase = rarity switch
        {
            EquipmentRarity.Common => 30,
            EquipmentRarity.Rare => 75,
            EquipmentRarity.Epic => 150,
            EquipmentRarity.Legendary => 300,
            _ => 50
        };
        string folder = "Assets/ScriptableObjects/Skills"; // reuse existing folder
        AssetDatabase.CreateAsset(item, $"{folder}/Equip_{id}.asset");
    }
}
