using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateAchievementAssets
{
    [MenuItem("VOLK/Create Achievement Assets")]
    static void Create()
    {
        // Combat
        A("ach_first_punch", "Ilk Yumruk", "1 yumruk at", AchievementCondition.TotalPunches, 1, 10, 0, 5);
        A("ach_punch_master", "Yumruk Ustasi", "100 yumruk at", AchievementCondition.TotalPunches, 100, 100, 5, 50);
        A("ach_kick_king", "Tekme Krali", "100 tekme at", AchievementCondition.TotalKicks, 100, 100, 5, 50);
        A("ach_combo_hunter", "Combo Avcisi", "50 kombo yap", AchievementCondition.TotalCombos, 50, 150, 10, 75);
        A("ach_perfect", "Mukemmel Zafer", "Hasar almadan kazan", AchievementCondition.PerfectWin, 1, 200, 10, 100);

        // Story
        A("ach_first_step", "Ilk Adim", "Bolum 1'i tamamla", AchievementCondition.CompleteChapter, 1, 50, 3, 25);
        A("ach_champion", "Sampiyon", "Tum bolumleri tamamla", AchievementCondition.CompleteAllChapters, 1, 500, 25, 200);

        // Survival
        A("ach_durable", "Dayanikli", "Survival 5. round'a ulas", AchievementCondition.SurvivalRounds, 5, 75, 3, 30);
        A("ach_warrior", "Savasci", "Survival 10. round'a ulas", AchievementCondition.SurvivalRounds, 10, 150, 8, 60);
        A("ach_legend", "Efsane", "Survival 20. round'a ulas", AchievementCondition.SurvivalRounds, 20, 300, 15, 120);

        // Equipment
        A("ach_first_equip", "Ilk Ekipman", "Bir ekipman tak", AchievementCondition.EquipItem, 1, 25, 1, 10);
        A("ach_rare_collector", "Nadir Koleksiyoner", "5 Rare ekipman topla", AchievementCondition.CollectRareItems, 5, 200, 10, 80);
        A("ach_epic_hunter", "Epik Avci", "3 Epic ekipman topla", AchievementCondition.CollectEpicItems, 3, 300, 15, 100);

        // Progression
        A("ach_wins_10", "Caylak Dovuscu", "10 mac kazan", AchievementCondition.TotalWins, 10, 50, 3, 25);
        A("ach_wins_50", "Deneyimli", "50 mac kazan", AchievementCondition.TotalWins, 50, 200, 10, 75);
        A("ach_wins_100", "Usta Dovuscu", "100 mac kazan", AchievementCondition.TotalWins, 100, 500, 25, 150);
        A("ach_level_10", "Gelisen Guc", "Seviye 10'a ulas", AchievementCondition.ReachLevel, 10, 100, 5, 50);
        A("ach_level_25", "Guclu Irade", "Seviye 25'e ulas", AchievementCondition.ReachLevel, 25, 250, 15, 100);
        A("ach_stars_30", "Yildiz Toplayici", "30 yildiz topla", AchievementCondition.TotalStars, 30, 200, 10, 75);

        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] 19 achievement assets created!");
    }

    static void A(string id, string title, string desc, AchievementCondition cond, int target, int coins, int gems, int xp)
    {
        var ach = ScriptableObject.CreateInstance<AchievementData>();
        ach.achievementId = id;
        ach.title = title;
        ach.description = desc;
        ach.condition = cond;
        ach.targetValue = target;
        ach.coinReward = coins;
        ach.gemReward = gems;
        ach.xpReward = xp;
        AssetDatabase.CreateAsset(ach, $"Assets/ScriptableObjects/Skills/Ach_{id}.asset");
    }
}
