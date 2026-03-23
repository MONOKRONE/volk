using UnityEngine;

namespace Volk.Core
{
    public enum QuestCondition { WinMatches, PerformCombos, WinWithoutDamage, WinOnHard, PlayMatches }

    [CreateAssetMenu(fileName = "NewQuest", menuName = "VOLK/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questName;
        [TextArea] public string description;
        public QuestCondition condition;
        public int targetCount = 1;
        public int coinReward = 100;
    }
}
