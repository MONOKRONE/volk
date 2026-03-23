using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateEquipmentAssets
{
    [MenuItem("VOLK/Create Equipment Assets")]
    static void Create()
    {
        // Starter Common items
        CreateItem("starter_gloves", "Antrenman Eldiveni", EquipmentSlot.Gloves, EquipmentRarity.Common, 3f, "Basit ama ise yarar.");
        CreateItem("starter_boots", "Sokak Ayakkabisi", EquipmentSlot.Boots, EquipmentRarity.Common, 2f, "Hafif ve rahat.");
        CreateItem("starter_guard", "Bez Bandaj", EquipmentSlot.Guard, EquipmentRarity.Common, 3f, "Temel koruma.");
        CreateItem("starter_headgear", "Pamuk Bant", EquipmentSlot.Headgear, EquipmentRarity.Common, 2f, "Alin koruyucu.");

        // Rare items
        CreateItem("rare_gloves", "Celik Yumruk", EquipmentSlot.Gloves, EquipmentRarity.Rare, 5f, "Metal takviyeli eldivenler.");
        CreateItem("rare_boots", "Hiz Botlari", EquipmentSlot.Boots, EquipmentRarity.Rare, 4f, "Cok hafif malzeme.");
        CreateItem("rare_guard", "Deri Yelek", EquipmentSlot.Guard, EquipmentRarity.Rare, 5f, "Dayanikli deri koruma.");
        CreateItem("rare_headgear", "Boks Kaskı", EquipmentSlot.Headgear, EquipmentRarity.Rare, 4f, "Profesyonel koruma.");

        // Epic items
        CreateItem("epic_gloves", "Ejderha Pençesi", EquipmentSlot.Gloves, EquipmentRarity.Epic, 7f, "Efsanevi guc.");
        CreateItem("epic_boots", "Ruzgar Adimi", EquipmentSlot.Boots, EquipmentRarity.Epic, 6f, "Gorulmez hiz.");
        CreateItem("epic_guard", "Titanyum Zirhı", EquipmentSlot.Guard, EquipmentRarity.Epic, 7f, "Kirilmaz savunma.");
        CreateItem("epic_headgear", "Kurt Maskesi", EquipmentSlot.Headgear, EquipmentRarity.Epic, 6f, "VOLK'un sembolü.");

        // Legendary items
        CreateItem("legend_gloves", "Sultanin Yumrugu", EquipmentSlot.Gloves, EquipmentRarity.Legendary, 10f, "Istanbul'un en guclu silahi.");
        CreateItem("legend_guard", "Bogazin Kalkani", EquipmentSlot.Guard, EquipmentRarity.Legendary, 10f, "Yenilmez savunma.");

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
