using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateEquipmentSOs
{
    [MenuItem("VOLK/Create 20 Equipment Assets")]
    public static void Create()
    {
        string dir = "Assets/ScriptableObjects/Equipment";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Equipment");

        var slots = new[] { EquipmentSlot.Gloves, EquipmentSlot.Boots, EquipmentSlot.Chest, EquipmentSlot.Headband, EquipmentSlot.Accessory };
        var rarities = new[] { EquipmentRarity.Common, EquipmentRarity.Rare, EquipmentRarity.Epic, EquipmentRarity.Legendary };
        string[] slotNames = { "Gloves", "Boots", "Chest", "Headband", "Accessory" };
        string[] rarityNames = { "Common", "Rare", "Epic", "Legendary" };
        float[] baseStats = { 5f, 4f, 8f, 10f, 3f }; // per slot
        int[] upgradeCosts = { 50, 50, 75, 75, 100 };

        // Special effects for accessories
        var accessoryEffects = new[] {
            EquipmentSpecialEffect.None,
            EquipmentSpecialEffect.Lifesteal,
            EquipmentSpecialEffect.StunChance,
            EquipmentSpecialEffect.ExpBoost
        };

        int count = 0;
        for (int s = 0; s < slots.Length; s++)
        {
            for (int r = 0; r < rarities.Length; r++)
            {
                var eq = ScriptableObject.CreateInstance<EquipmentData>();
                eq.itemId = $"{slotNames[s]}_{rarityNames[r]}";
                eq.itemName = $"{rarityNames[r]} {slotNames[s]}";
                eq.description = $"{rarityNames[r]} tier {slotNames[s].ToLower()}";
                eq.slot = slots[s];
                eq.rarity = rarities[r];
                eq.baseStat = baseStats[s];
                eq.maxUpgradeLevel = 5;
                eq.upgradeCostBase = upgradeCosts[s] * (r + 1);

                if (slots[s] == EquipmentSlot.Accessory)
                    eq.specialEffect = accessoryEffects[r];

                string path = $"{dir}/{eq.itemId}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(eq, path);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VOLK] {count} equipment assets created (5 slots × 4 tiers)!");
    }
}
