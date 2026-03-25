using UnityEngine;
using UnityEditor;
using Volk.Core;
using Volk.Story;

public class CreateChapterAssets
{
    static readonly (string title, string desc, string bossChar, string unlockChar,
        AIDifficulty diff, float hpMult, int reward)[] ChapterDefs = new[]
    {
        ("Sokak Dovusu",         "Istanbul sokaklarinda ilk meydan okuma",  "YILDIZ", null,     AIDifficulty.Easy,   0.8f, 50),
        ("Yeralti Turnuvasi",    "Gercek turnuva basliyor",                 "KAYA",   null,     AIDifficulty.Easy,   1.0f, 100),
        ("Saha Ustasi",          "Turnuvanin en guclu rakibi",              "RUZGAR", null,     AIDifficulty.Normal, 1.2f, 150),
        ("Kaya'nin Meydan Okumasi", "Kaya seni bekliyor",                   "KAYA",   "RUZGAR", AIDifficulty.Normal, 1.5f, 200),
        ("Celik'in Kalesi",      "Celik'in savunmasini kir",                "CELIK",  null,     AIDifficulty.Normal, 1.8f, 250),
        ("Gizli Usta",           "Celik seni sinav ediyor",                 "CELIK",  "CELIK",  AIDifficulty.Hard,   2.0f, 300),
        ("Sis'in Aldatmacasi",   "Sis'in tuzaklarindan kurtul",            "SIS",    null,     AIDifficulty.Hard,   2.2f, 400),
        ("Final — VOLK",         "Son savas. Kazanan her seyi alir.",       "SIS",    "SIS",    AIDifficulty.Hard,   2.5f, 500),
    };

    [MenuItem("VOLK/Create 8 Chapter Assets")]
    static void Create()
    {
        string chapterDir = "Assets/ScriptableObjects/Chapters";
        string bossDir = "Assets/ScriptableObjects/Bosses";

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Bosses"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Bosses");
        }
        if (!AssetDatabase.IsValidFolder(chapterDir))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Chapters");

        // Load dialogue content
        string[][][] intros = { StoryContent.CH1_INTRO, StoryContent.CH2_INTRO, StoryContent.CH3_INTRO, StoryContent.CH4_INTRO, null, null, null, null };
        string[][][] outros = { StoryContent.CH1_OUTRO, StoryContent.CH2_OUTRO, StoryContent.CH3_OUTRO, StoryContent.CH4_OUTRO, null, null, null, null };

        for (int i = 0; i < ChapterDefs.Length; i++)
        {
            var def = ChapterDefs[i];
            int num = i + 1;

            // Create BossData
            var boss = ScriptableObject.CreateInstance<BossData>();
            boss.bossCharacter = LoadChar(def.bossChar);
            boss.bossHPMultiplier = def.hpMult;
            boss.coinReward = def.reward;
            if (def.unlockChar != null)
                boss.rewardCharacterUnlock = LoadChar(def.unlockChar);

            string bossPath = $"{bossDir}/Ch{num}_Boss.asset";
            AssetDatabase.DeleteAsset(bossPath);
            AssetDatabase.CreateAsset(boss, bossPath);

            // Create ChapterData
            var ch = ScriptableObject.CreateInstance<ChapterData>();
            ch.chapterTitle = def.title;
            ch.description = def.desc;
            ch.chapterNumber = num;
            ch.difficulty = def.diff;
            ch.enemyHPMultiplier = def.hpMult;
            ch.coinReward = def.reward;
            ch.enemyCharacter = boss.bossCharacter;
            ch.boss = boss;
            ch.characterUnlockReward = boss.rewardCharacterUnlock;

            if (i < intros.Length && intros[i] != null)
                ch.introDialogue = ConvertDialogue(intros[i]);
            if (i < outros.Length && outros[i] != null)
                ch.outroDialogue = ConvertDialogue(outros[i]);

            string chPath = $"{chapterDir}/Chapter{num}.asset";
            AssetDatabase.DeleteAsset(chPath);
            AssetDatabase.CreateAsset(ch, chPath);
            EditorUtility.SetDirty(ch);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VOLK] 8 chapters + 8 boss assets created!");
    }

    static CharacterData LoadChar(string name)
    {
        return AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/ScriptableObjects/Characters/{name}.asset");
    }

    static DialogueEntry[] ConvertDialogue(string[][] lines)
    {
        if (lines == null) return new DialogueEntry[0];
        var entries = new DialogueEntry[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            entries[i] = new DialogueEntry
            {
                speakerName = lines[i][0],
                text = lines[i][1],
                isPlayerSpeaking = lines[i][0] == "Volk"
            };
        }
        return entries;
    }
}
