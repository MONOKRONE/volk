using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateVOLKCharacters
{
    [MenuItem("VOLK/Create 6 Character Assets")]
    public static void Create()
    {
        // ── SKILL DATA ──────────────────────────────────────────────

        // YILDIZ
        var yildiz_sk1 = MakeSkill("AlevYumruk", "Alev Yumruk", 35f, 6f);
        var yildiz_sk2 = MakeSkill("GirdapTekme", "Girdap Tekme", 28f, 5f);

        // KAYA
        var kaya_sk1 = MakeSkill("DepremDarbesi", "Deprem Darbesi", 45f, 8f);
        var kaya_sk2 = MakeSkill("AyiKucagi", "Ayi Kucagi", 30f, 7f);

        // RUZGAR
        var ruzgar_sk1 = MakeSkill("GolgeAdim", "Golge Adim", 20f, 4f);
        var ruzgar_sk2 = MakeSkill("FirtinaSerisi", "Firtina Serisi", 25f, 5f);

        // CELIK
        var celik_sk1 = MakeSkill("AynaKalkan", "Ayna Kalkan", 15f, 6f);
        var celik_sk2 = MakeSkill("HassasVurus", "Hassas Vurus", 40f, 9f);

        // SIS
        var sis_sk1 = MakeSkill("SisPerdesi", "Sis Perdesi", 18f, 5f);
        var sis_sk2 = MakeSkill("GolgeKlon", "Golge Klon", 22f, 7f);

        // TOPRAK
        var toprak_sk1 = MakeSkill("TasFirlatma", "Tas Firlatma", 32f, 5f);
        var toprak_sk2 = MakeSkill("DuvarYukselt", "Duvar Yukselt", 20f, 8f);

        AssetDatabase.SaveAssets();

        // ── CHARACTER DATA ──────────────────────────────────────────

        MakeCharacter("YILDIZ",
            hp: 100f, spd: 6f, pow: 6f, def: 6f,
            walk: 4.0f, run: 7.0f, kb: 2.0f,
            unlocked: true,
            sk1: yildiz_sk1, sk2: yildiz_sk2);

        MakeCharacter("KAYA",
            hp: 130f, spd: 3f, pow: 9f, def: 8f,
            walk: 3.0f, run: 5.5f, kb: 3.0f,
            unlocked: true,
            sk1: kaya_sk1, sk2: kaya_sk2);

        MakeCharacter("RUZGAR",
            hp: 85f, spd: 10f, pow: 5f, def: 3f,
            walk: 5.5f, run: 9.0f, kb: 1.5f,
            unlocked: false, unlockType: UnlockCondition.WinCount, unlockVal: 10,
            sk1: ruzgar_sk1, sk2: ruzgar_sk2);

        MakeCharacter("CELIK",
            hp: 100f, spd: 5f, pow: 7f, def: 9f,
            walk: 4.0f, run: 6.5f, kb: 1.8f,
            unlocked: false, unlockType: UnlockCondition.WinCount, unlockVal: 10,
            sk1: celik_sk1, sk2: celik_sk2);

        MakeCharacter("SIS",
            hp: 80f, spd: 8f, pow: 6f, def: 4f,
            walk: 5.0f, run: 8.5f, kb: 1.5f,
            unlocked: false, unlockType: UnlockCondition.WinCount, unlockVal: 10,
            sk1: sis_sk1, sk2: sis_sk2);

        MakeCharacter("TOPRAK",
            hp: 110f, spd: 4f, pow: 7f, def: 7f,
            walk: 3.5f, run: 6.0f, kb: 2.5f,
            unlocked: false, unlockType: UnlockCondition.WinCount, unlockVal: 10,
            sk1: toprak_sk1, sk2: toprak_sk2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[VOLK] 6 karakter + 12 skill asset olusturuldu!");
    }

    static SkillData MakeSkill(string fileName, string displayName, float damage, float cooldown)
    {
        string path = $"Assets/ScriptableObjects/Skills/{fileName}.asset";

        // Varsa sil, yeniden olustur
        AssetDatabase.DeleteAsset(path);

        var sk = ScriptableObject.CreateInstance<SkillData>();
        sk.skillName = displayName;
        sk.damage = damage;
        sk.cooldown = cooldown;
        sk.animationTrigger = ""; // Sonra doldurulacak
        AssetDatabase.CreateAsset(sk, path);
        return sk;
    }

    static void MakeCharacter(string name,
        float hp, float spd, float pow, float def,
        float walk, float run, float kb,
        bool unlocked,
        SkillData sk1, SkillData sk2,
        UnlockCondition unlockType = UnlockCondition.None,
        int unlockVal = 0)
    {
        string path = $"Assets/ScriptableObjects/Characters/{name}.asset";
        AssetDatabase.DeleteAsset(path);

        var c = ScriptableObject.CreateInstance<CharacterData>();
        c.characterName = name;
        c.maxHP = hp;
        c.speed = spd;
        c.power = pow;
        c.defense = def;
        c.attackDamage = pow * 1.67f; // power'a oranli
        c.attackRange = 1.2f;
        c.walkSpeed = walk;
        c.runSpeed = run;
        c.knockbackForce = kb;
        c.unlockedByDefault = unlocked;
        c.unlockType = unlockType;
        c.unlockValue = unlockVal;
        c.skill1 = sk1;
        c.skill2 = sk2;

        // Link animator controller
        var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/PlayerAnimator.controller");
        if (animCtrl != null)
            c.animController = animCtrl;

        AssetDatabase.CreateAsset(c, path);
        EditorUtility.SetDirty(c);
    }
}
