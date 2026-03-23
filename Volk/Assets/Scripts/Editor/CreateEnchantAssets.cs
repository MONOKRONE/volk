using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateEnchantAssets
{
    [MenuItem("VOLK/Create Enchant Assets")]
    static void Create()
    {
        E("enchant_lifesteal", "Can Emici", "Vuruslarin %5'i kadar HP geri kazan",
            EnchantType.Lifesteal, 0.05f, new Color(0.8f, 0.1f, 0.2f));

        E("enchant_frenzy", "Cinnet", "Hareket hizin %10 artar",
            EnchantType.Frenzy, 0.10f, new Color(1f, 0.5f, 0f));

        E("enchant_shield", "Kalkan", "Savunman %15 artar (ekstra HP)",
            EnchantType.Shield, 0.15f, new Color(0.2f, 0.6f, 1f));

        E("enchant_vampir", "Vampir", "KO aninda %20 HP geri kazan",
            EnchantType.Vampir, 0.20f, new Color(0.5f, 0f, 0.5f));

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 4 enchant assets created!");
    }

    static void E(string id, string name, string desc, EnchantType type, float value, Color glow)
    {
        var e = ScriptableObject.CreateInstance<EnchantData>();
        e.enchantId = id;
        e.enchantName = name;
        e.description = desc;
        e.type = type;
        e.effectValue = value;
        e.glowColor = glow;
        AssetDatabase.CreateAsset(e, $"Assets/ScriptableObjects/Skills/Enchant_{id}.asset");
    }
}
