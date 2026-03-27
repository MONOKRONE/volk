using UnityEngine;

namespace Volk.Core
{
    public enum ComboInputType
    {
        Tap,        // Single press
        Hold,       // Hold for duration
        SkillCancel // Cancel current attack into skill
    }

    [System.Serializable]
    public class ComboInput
    {
        public AttackType attackType;
        public ComboInputType inputType = ComboInputType.Tap;
        public float holdDuration = 0.3f; // Only for Hold type
    }

    [CreateAssetMenu(fileName = "NewCombo", menuName = "VOLK/Combo Data")]
    public class ComboData : ScriptableObject
    {
        public string comboName;
        [TextArea] public string description;

        [Header("Input (Legacy — simple sequence)")]
        public AttackType[] inputSequence;

        [Header("Input (Advanced — tap/hold/cancel)")]
        public ComboInput[] advancedSequence;

        [Header("Effect")]
        public float damageMultiplier = 1.5f;
        public string bonusAnimTrigger;
        public AudioClip bonusSfx;
        public GameObject bonusVfx;

        [Header("Hit Tier")]
        public HitTier hitTier = HitTier.Medium;
    }

    public enum HitTier
    {
        Light,   // Jab, quick tap
        Medium,  // Standard combo
        Heavy,   // Hold attacks, finishers
        Skill    // Skill-cancelled attacks
    }
}
