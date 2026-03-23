using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateComboAssets
{
    [MenuItem("VOLK/Create Placeholder Combo Assets")]
    static void Create()
    {
        var combo1 = ScriptableObject.CreateInstance<ComboData>();
        combo1.comboName = "Istanbul Combo";
        combo1.description = "Punch Punch Kick — klasik sokak kombini";
        combo1.inputSequence = new[] { AttackType.Punch, AttackType.Punch, AttackType.Kick };
        combo1.damageMultiplier = 1.5f;
        AssetDatabase.CreateAsset(combo1, "Assets/ScriptableObjects/Skills/Combo_Istanbul.asset");

        var combo2 = ScriptableObject.CreateInstance<ComboData>();
        combo2.comboName = "Bosphorus Rush";
        combo2.description = "Kick Punch Punch — hizli saldiri dizisi";
        combo2.inputSequence = new[] { AttackType.Kick, AttackType.Punch, AttackType.Punch };
        combo2.damageMultiplier = 1.3f;
        AssetDatabase.CreateAsset(combo2, "Assets/ScriptableObjects/Skills/Combo_Bosphorus.asset");

        var combo3 = ScriptableObject.CreateInstance<ComboData>();
        combo3.comboName = "Galata Fury";
        combo3.description = "Kick Kick Punch — guclu bitirici";
        combo3.inputSequence = new[] { AttackType.Kick, AttackType.Kick, AttackType.Punch };
        combo3.damageMultiplier = 1.8f;
        AssetDatabase.CreateAsset(combo3, "Assets/ScriptableObjects/Skills/Combo_Galata.asset");

        var combo4 = ScriptableObject.CreateInstance<ComboData>();
        combo4.comboName = "Sultan Strike";
        combo4.description = "Punch Kick Punch Kick — uzun kombo";
        combo4.inputSequence = new[] { AttackType.Punch, AttackType.Kick, AttackType.Punch, AttackType.Kick };
        combo4.damageMultiplier = 2.0f;
        AssetDatabase.CreateAsset(combo4, "Assets/ScriptableObjects/Skills/Combo_Sultan.asset");

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 4 combo assets created!");
    }
}
