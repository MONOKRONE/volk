using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateChapterStages
{
    static readonly string[] CharacterNames = { "YILDIZ", "KAYA", "RUZGAR", "CELIK", "SIS", "TOPRAK" };

    // Chapter definitions: (chapterNum, bossCharName, unlockCharName, specialStageTypes)
    static readonly (int num, string boss, string unlock, StageType s8, StageType s9)[] Chapters = new[]
    {
        (1, "YILDIZ", null,     StageType.Handicap, StageType.Handicap),
        (2, "KAYA",   null,     StageType.Timed,    StageType.Timed),
        (3, "RUZGAR", null,     StageType.Survival,  StageType.Survival),
        (4, "KAYA",   "KAYA",   StageType.Handicap, StageType.Timed),
        (5, "CELIK",  null,     StageType.Handicap, StageType.Handicap),
        (6, "CELIK",  "CELIK",  StageType.Timed,    StageType.Survival),
        (7, "SIS",    null,     StageType.Survival,  StageType.Survival),
        (8, "SIS",    "SIS",    StageType.Handicap, StageType.Timed),
    };

    [MenuItem("VOLK/Create 80 Stage Structure")]
    public static void Create()
    {
        string stageDir = "Assets/ScriptableObjects/Stages";
        string chapterDir = "Assets/ScriptableObjects/Chapters";
        string bossDir = "Assets/ScriptableObjects/Bosses";

        EnsureFolder("Assets/ScriptableObjects", "Stages");
        EnsureFolder("Assets/ScriptableObjects", "Bosses");
        EnsureFolder("Assets/ScriptableObjects", "Chapters");

        int totalStages = 0;

        foreach (var ch in Chapters)
        {
            var stages = new StageData[9]; // 7 standard + 2 special

            AIDifficulty baseDiff = ch.num <= 3 ? AIDifficulty.Easy :
                                     ch.num <= 6 ? AIDifficulty.Normal : AIDifficulty.Hard;

            // Stages 1-7: Standard
            for (int i = 0; i < 7; i++)
            {
                var stage = ScriptableObject.CreateInstance<StageData>();
                stage.stageName = $"Ch{ch.num} Stage {i + 1}";
                stage.stageIndex = i;
                stage.stageType = StageType.Standard;
                stage.opponentCharacter = LoadCharacter(CharacterNames[(ch.num + i) % CharacterNames.Length]);
                stage.difficulty = baseDiff;
                stage.hpMultiplier = 1f + (i * 0.05f);
                stage.coinReward = 30 + (ch.num * 5) + (i * 3);

                string path = $"{stageDir}/Ch{ch.num}_Stage{i + 1}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(stage, path);
                stages[i] = stage;
                totalStages++;
            }

            // Stage 8-9: Special types
            for (int i = 7; i < 9; i++)
            {
                var stage = ScriptableObject.CreateInstance<StageData>();
                stage.stageName = $"Ch{ch.num} Stage {i + 1}";
                stage.stageIndex = i;
                stage.stageType = i == 7 ? ch.s8 : ch.s9;
                stage.opponentCharacter = LoadCharacter(CharacterNames[(ch.num + i + 2) % CharacterNames.Length]);
                stage.difficulty = baseDiff == AIDifficulty.Easy ? AIDifficulty.Normal : AIDifficulty.Hard;
                stage.hpMultiplier = 1.2f;
                stage.coinReward = 60 + (ch.num * 10);

                if (stage.stageType == StageType.Timed)
                    stage.timeLimitSeconds = 60f;
                if (stage.stageType == StageType.Handicap)
                    stage.playerHPMultiplier = 0.7f;

                string path = $"{stageDir}/Ch{ch.num}_Stage{i + 1}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(stage, path);
                stages[i] = stage;
                totalStages++;
            }

            // Boss
            var boss = ScriptableObject.CreateInstance<BossData>();
            boss.bossCharacter = LoadCharacter(ch.boss);
            boss.bossHPMultiplier = 1.5f + (ch.num * 0.1f);
            boss.coinReward = 150 + (ch.num * 25);
            if (ch.unlock != null)
                boss.rewardCharacterUnlock = LoadCharacter(ch.unlock);

            string bossPath = $"{bossDir}/Ch{ch.num}_Boss.asset";
            AssetDatabase.DeleteAsset(bossPath);
            AssetDatabase.CreateAsset(boss, bossPath);
            totalStages++; // Boss counts as stage 10

            // ChapterData
            string chPath = $"{chapterDir}/Chapter{ch.num}.asset";
            AssetDatabase.DeleteAsset(chPath);

            var chapter = ScriptableObject.CreateInstance<ChapterData>();
            chapter.chapterTitle = $"Bolum {ch.num}";
            chapter.chapterNumber = ch.num;
            chapter.stages = stages;
            chapter.boss = boss;
            chapter.coinReward = 100 + (ch.num * 20);
            if (ch.unlock != null)
                chapter.characterUnlockReward = LoadCharacter(ch.unlock);

            AssetDatabase.CreateAsset(chapter, chPath);
            EditorUtility.SetDirty(chapter);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VOLK] Created {totalStages} stages across 8 chapters!");
    }

    static CharacterData LoadCharacter(string name)
    {
        return AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/ScriptableObjects/Characters/{name}.asset");
    }

    static void EnsureFolder(string parent, string child)
    {
        string full = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(full))
        {
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets", parent.Replace("Assets/", ""));
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
