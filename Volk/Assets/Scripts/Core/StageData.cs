using UnityEngine;

namespace Volk.Core
{
    public enum StageType
    {
        Standard,
        Survival,
        Timed,
        Handicap,
        Boss
    }

    public enum GhostScenarioType
    {
        None,
        MirrorMatch,
        AggressiveClone,
        DefensiveClone,
        LowHPPressure,
        CornerTrap,
        SkillPressure,
        ComboChain,
        ParryCounter,
        AdaptiveClone
    }

    [CreateAssetMenu(fileName = "NewStage", menuName = "VOLK/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Identity")]
        public string stageName;
        public int stageIndex;

        [Header("Type")]
        public StageType stageType = StageType.Standard;

        [Header("Opponent")]
        public CharacterData opponentCharacter;
        public AIDifficulty difficulty = AIDifficulty.Normal;
        public float hpMultiplier = 1f;

        [Header("Ghost AI")]
        public bool isGhostSimulation;
        public GhostScenarioType ghostScenarioType = GhostScenarioType.None;

        [Header("Modifiers")]
        public float timeLimitSeconds; // 0 = no limit
        public float playerHPMultiplier = 1f; // Handicap: reduce player HP

        [Header("Rewards")]
        public int coinReward = 50;
    }
}
