using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewBoss", menuName = "VOLK/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("Boss Character")]
        public CharacterData bossCharacter;
        public float bossHPMultiplier = 1.5f;

        [Header("Special Modifier")]
        public string specialModifier; // e.g. "DoubleKnockback", "NoFlinch"
        public float modifierValue;

        [Header("Rewards")]
        public CharacterData rewardCharacterUnlock;
        public int coinReward = 200;
    }
}
