using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewChapter", menuName = "VOLK/Chapter Data")]
    public class ChapterData : ScriptableObject
    {
        public string chapterTitle;
        [TextArea(2, 4)] public string description;
        public int chapterNumber;

        [Header("Stages")]
        public StageData[] stages;
        public BossData boss;

        [Header("Legacy Enemy (single-stage compat)")]
        public CharacterData enemyCharacter;
        public AIDifficulty difficulty = AIDifficulty.Normal;
        public float enemyHPMultiplier = 1f;

        [Header("Rewards")]
        public int coinReward = 100;
        public CharacterData characterUnlockReward;

        [Header("Dialogue")]
        public DialogueEntry[] introDialogue;
        public DialogueEntry[] outroDialogue;

        [Header("Arena")]
        public string arenaSceneName = "CombatTest";
        public ArenaData arenaData;
    }

    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        public Sprite speakerPortrait;
        [TextArea(2, 4)] public string text;
        public bool isPlayerSpeaking;
    }
}
