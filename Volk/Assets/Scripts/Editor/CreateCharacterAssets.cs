using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateCharacterAssets
{
    [MenuItem("VOLK/Create Placeholder Character Assets")]
    static void Create()
    {
        // Maria - balanced fighter
        var maria = ScriptableObject.CreateInstance<CharacterData>();
        maria.characterName = "Maria";
        maria.speed = 6f;
        maria.power = 5f;
        maria.defense = 5f;
        maria.maxHP = 100f;
        maria.attackDamage = 15f;
        maria.attackRange = 1.2f;
        maria.walkSpeed = 4f;
        maria.runSpeed = 7f;
        maria.knockbackForce = 2f;
        maria.unlockedByDefault = true;
        AssetDatabase.CreateAsset(maria, "Assets/ScriptableObjects/Characters/Maria.asset");

        // Kachujin - heavy hitter
        var kachujin = ScriptableObject.CreateInstance<CharacterData>();
        kachujin.characterName = "Kachujin";
        kachujin.speed = 4f;
        kachujin.power = 8f;
        kachujin.defense = 6f;
        kachujin.maxHP = 120f;
        kachujin.attackDamage = 20f;
        kachujin.attackRange = 1.3f;
        kachujin.walkSpeed = 3.5f;
        kachujin.runSpeed = 6f;
        kachujin.knockbackForce = 3f;
        kachujin.unlockedByDefault = true;
        AssetDatabase.CreateAsset(kachujin, "Assets/ScriptableObjects/Characters/Kachujin.asset");

        // Placeholder skills
        var sk1 = ScriptableObject.CreateInstance<SkillData>();
        sk1.skillName = "Power Strike";
        sk1.damage = 30f;
        sk1.cooldown = 5f;
        sk1.animationTrigger = "HookPunch";
        AssetDatabase.CreateAsset(sk1, "Assets/ScriptableObjects/Skills/PowerStrike.asset");

        var sk2 = ScriptableObject.CreateInstance<SkillData>();
        sk2.skillName = "Spinning Kick";
        sk2.damage = 25f;
        sk2.cooldown = 4f;
        sk2.animationTrigger = "MMAKick";
        AssetDatabase.CreateAsset(sk2, "Assets/ScriptableObjects/Skills/SpinningKick.asset");

        // Link skills to characters
        maria.skill1 = sk1;
        maria.skill2 = sk2;
        EditorUtility.SetDirty(maria);

        kachujin.skill1 = sk1;
        kachujin.skill2 = sk2;
        EditorUtility.SetDirty(kachujin);

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] Placeholder character and skill assets created!");
    }
}
