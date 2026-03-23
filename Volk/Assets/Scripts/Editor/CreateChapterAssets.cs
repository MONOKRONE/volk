using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateChapterAssets
{
    [MenuItem("VOLK/Create Placeholder Chapter Assets")]
    static void Create()
    {
        // Load existing character SOs if available
        var kachujin = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/ScriptableObjects/Characters/Kachujin.asset");

        var ch1 = ScriptableObject.CreateInstance<ChapterData>();
        ch1.chapterTitle = "Sokak Dovusu";
        ch1.description = "Istanbul sokaklarinda ilk meydan okuma";
        ch1.chapterNumber = 1;
        ch1.difficulty = AIDifficulty.Easy;
        ch1.enemyHPMultiplier = 0.8f;
        ch1.coinReward = 50;
        ch1.enemyCharacter = kachujin;
        ch1.introDialogue = new DialogueEntry[]
        {
            new DialogueEntry { speakerName = "???", text = "Sen mi bu mahalle'nin en iyisisin?", isPlayerSpeaking = false },
            new DialogueEntry { speakerName = "Volk", text = "Dene de gor.", isPlayerSpeaking = true }
        };
        AssetDatabase.CreateAsset(ch1, "Assets/ScriptableObjects/Chapters/Chapter1.asset");

        var ch2 = ScriptableObject.CreateInstance<ChapterData>();
        ch2.chapterTitle = "Yeralti Turnuvasi";
        ch2.description = "Gercek turnuva basliyor";
        ch2.chapterNumber = 2;
        ch2.difficulty = AIDifficulty.Normal;
        ch2.enemyHPMultiplier = 1f;
        ch2.coinReward = 100;
        ch2.enemyCharacter = kachujin;
        AssetDatabase.CreateAsset(ch2, "Assets/ScriptableObjects/Chapters/Chapter2.asset");

        var ch3 = ScriptableObject.CreateInstance<ChapterData>();
        ch3.chapterTitle = "Saha Ustasi";
        ch3.description = "Turnuvanin en guclu rakibi";
        ch3.chapterNumber = 3;
        ch3.difficulty = AIDifficulty.Normal;
        ch3.enemyHPMultiplier = 1.2f;
        ch3.coinReward = 150;
        ch3.enemyCharacter = kachujin;
        AssetDatabase.CreateAsset(ch3, "Assets/ScriptableObjects/Chapters/Chapter3.asset");

        var ch4 = ScriptableObject.CreateInstance<ChapterData>();
        ch4.chapterTitle = "Final — VOLK";
        ch4.description = "Son savas. Kazanan her seyi alir.";
        ch4.chapterNumber = 4;
        ch4.difficulty = AIDifficulty.Hard;
        ch4.enemyHPMultiplier = 1.5f;
        ch4.coinReward = 500;
        ch4.enemyCharacter = kachujin;
        ch4.outroDialogue = new DialogueEntry[]
        {
            new DialogueEntry { speakerName = "Volk", text = "Bu turnuvanin galibi... benim.", isPlayerSpeaking = true }
        };
        AssetDatabase.CreateAsset(ch4, "Assets/ScriptableObjects/Chapters/Chapter4.asset");

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 4 placeholder chapter assets created!");
    }
}
