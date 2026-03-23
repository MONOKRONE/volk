using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateQuestAssets
{
    [MenuItem("VOLK/Create Placeholder Quest Assets")]
    static void Create()
    {
        CreateQuest("Quest_Win3", "3 Mac Kazan", "3 mac kazanarak zaferini kanitla", QuestCondition.WinMatches, 3, 100);
        CreateQuest("Quest_Combo5", "5 Kombo Yap", "5 kombolu saldiri gerceklestir", QuestCondition.PerformCombos, 5, 50);
        CreateQuest("Quest_Flawless", "Hasarsiz Kazan", "Hasar almadan bir mac kazan", QuestCondition.WinWithoutDamage, 1, 200);
        CreateQuest("Quest_HardWin", "Zor Modda Kazan", "Zor seviyede 1 mac kazan", QuestCondition.WinOnHard, 1, 150);
        CreateQuest("Quest_Play5", "5 Mac Oyna", "5 mac oyna", QuestCondition.PlayMatches, 5, 75);

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 5 quest assets created!");
    }

    static void CreateQuest(string fileName, string name, string desc, QuestCondition cond, int target, int reward)
    {
        var q = ScriptableObject.CreateInstance<QuestData>();
        q.questName = name;
        q.description = desc;
        q.condition = cond;
        q.targetCount = target;
        q.coinReward = reward;
        AssetDatabase.CreateAsset(q, $"Assets/ScriptableObjects/Skills/{fileName}.asset");
    }
}
