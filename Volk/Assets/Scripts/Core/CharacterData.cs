using UnityEngine;

namespace Volk.Core
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "VOLK/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string characterName;
        public Sprite portrait;
        public GameObject prefab;

        [Header("Stats")]
        [Range(1f, 10f)] public float speed = 5f;
        [Range(1f, 10f)] public float power = 5f;
        [Range(1f, 10f)] public float defense = 5f;
        public float maxHP = 100f;

        [Header("Combat")]
        public float attackDamage = 15f;
        public float attackRange = 1.2f;
        public float walkSpeed = 4f;
        public float runSpeed = 7f;
        public float knockbackForce = 2f;
        public float rotationSpeed = 1800f;

        [Header("Animation")]
        public RuntimeAnimatorController animController;

        [Header("Visual")]
        public Material characterMaterial;

        [Header("Skills")]
        public SkillBase skill1;
        public SkillBase skill2;

        [Header("Unlock")]
        public bool unlockedByDefault = true;
        public UnlockCondition unlockType = UnlockCondition.None;
        public int unlockValue;
    }

    public enum UnlockCondition
    {
        None,
        StoryProgress,
        WinCount,
        Currency
    }
}
