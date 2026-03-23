using UnityEngine;
using UnityEditor;
using Volk.Core;
using Volk.Story;

public class CreateChapterAssets
{
    [MenuItem("VOLK/Create Placeholder Chapter Assets")]
    static void Create()
    {
        var kachujin = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Kachujin.asset");

        CreateChapter("Chapter1", "Sokak Dovusu", "Istanbul sokaklarinda ilk meydan okuma",
            1, AIDifficulty.Easy, 0.8f, 50, kachujin,
            StoryContent.CH1_INTRO, StoryContent.CH1_OUTRO);

        CreateChapter("Chapter2", "Yeralti Turnuvasi", "Gercek turnuva basliyor",
            2, AIDifficulty.Normal, 1f, 100, kachujin,
            StoryContent.CH2_INTRO, StoryContent.CH2_OUTRO);

        CreateChapter("Chapter3", "Saha Ustasi", "Turnuvanin en guclu rakibi",
            3, AIDifficulty.Normal, 1.2f, 150, kachujin,
            StoryContent.CH3_INTRO, StoryContent.CH3_OUTRO);

        CreateChapter("Chapter4", "Final — VOLK", "Son savas. Kazanan her seyi alir.",
            4, AIDifficulty.Hard, 1.5f, 500, kachujin,
            StoryContent.CH4_INTRO, StoryContent.CH4_OUTRO);

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 4 chapter assets created with full dialogue!");
    }

    static void CreateChapter(string assetName, string title, string desc,
        int num, AIDifficulty diff, float hpMult, int reward, CharacterData enemy,
        string[][] introLines, string[][] outroLines)
    {
        var ch = ScriptableObject.CreateInstance<ChapterData>();
        ch.chapterTitle = title;
        ch.description = desc;
        ch.chapterNumber = num;
        ch.difficulty = diff;
        ch.enemyHPMultiplier = hpMult;
        ch.coinReward = reward;
        ch.enemyCharacter = enemy;
        ch.introDialogue = ConvertDialogue(introLines);
        ch.outroDialogue = ConvertDialogue(outroLines);
        AssetDatabase.CreateAsset(ch, $"Assets/ScriptableObjects/Chapters/{assetName}.asset");
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
