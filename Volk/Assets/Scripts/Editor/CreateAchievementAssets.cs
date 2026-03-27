using UnityEngine;
using UnityEditor;
using Volk.Core;

public class CreateAchievementAssets
{
    [MenuItem("VOLK/Create Achievement Assets")]
    static void Create()
    {
        // Combat
        A("ach_first_punch", "First Punch", "Throw 1 punch", AchievementCondition.TotalPunches, 1, 10, 0, 5);
        A("ach_punch_master", "Punch Master", "Throw 100 punches", AchievementCondition.TotalPunches, 100, 100, 5, 50);
        A("ach_kick_king", "Kick King", "Throw 100 kicks", AchievementCondition.TotalKicks, 100, 100, 5, 50);
        A("ach_combo_hunter", "Combo Hunter", "Land 50 combos", AchievementCondition.TotalCombos, 50, 150, 10, 75);
        A("ach_perfect", "Perfect Victory", "Win without taking damage", AchievementCondition.PerfectWin, 1, 200, 10, 100);

        // Story
        A("ach_first_step", "First Step", "Complete Chapter 1", AchievementCondition.CompleteChapter, 1, 50, 3, 25);
        A("ach_champion", "Champion", "Complete all chapters", AchievementCondition.CompleteAllChapters, 1, 500, 25, 200);

        // Survival
        A("ach_durable", "Enduring", "Reach Survival round 5", AchievementCondition.SurvivalRounds, 5, 75, 3, 30);
        A("ach_warrior", "Warrior", "Reach Survival round 10", AchievementCondition.SurvivalRounds, 10, 150, 8, 60);
        A("ach_legend", "Legend", "Reach Survival round 20", AchievementCondition.SurvivalRounds, 20, 300, 15, 120);

        // Equipment
        A("ach_first_equip", "First Gear", "Equip one item", AchievementCondition.EquipItem, 1, 25, 1, 10);
        A("ach_rare_collector", "Rare Collector", "Collect 5 Rare items", AchievementCondition.CollectRareItems, 5, 200, 10, 80);
        A("ach_epic_hunter", "Epic Hunter", "Collect 3 Epic items", AchievementCondition.CollectEpicItems, 3, 300, 15, 100);

        // Progression
        A("ach_wins_10", "Rookie Fighter", "Win 10 matches", AchievementCondition.TotalWins, 10, 50, 3, 25);
        A("ach_wins_50", "Experienced", "Win 50 matches", AchievementCondition.TotalWins, 50, 200, 10, 75);
        A("ach_wins_100", "Master Fighter", "Win 100 matches", AchievementCondition.TotalWins, 100, 500, 25, 150);
        A("ach_level_10", "Growing Power", "Reach Level 10", AchievementCondition.ReachLevel, 10, 100, 5, 50);
        A("ach_level_25", "Strong Will", "Reach Level 25", AchievementCondition.ReachLevel, 25, 250, 15, 100);
        A("ach_stars_30", "Star Collector", "Collect 30 stars", AchievementCondition.TotalStars, 30, 200, 10, 75);

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
