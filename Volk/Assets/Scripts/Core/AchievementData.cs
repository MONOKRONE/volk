using UnityEngine;

namespace Volk.Core
{
    public enum AchievementCondition
    {
        TotalPunches, TotalKicks, TotalCombos, PerfectWin,
        CompleteChapter, CompleteAllChapters,
        SurvivalRounds, EquipItem, CollectRareItems, CollectEpicItems,
        TotalWins, ReachLevel, TotalStars
    }

    [CreateAssetMenu(fileName = "NewAchievement", menuName = "VOLK/Achievement")]
    public class AchievementData : ScriptableObject
    {
        public string achievementId;
        public string title;
        [TextArea] public string description;
        public Sprite icon;
        public AchievementCondition condition;
        public int targetValue = 1;

        [Header("Rewards")]
        public int coinReward;
        public int gemReward;
        public int xpReward;
    }
}
