using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Abstract base class for all skills.
    /// Each skill type implements Execute() with its own logic.
    /// Uses Command Pattern + ScriptableObject (data lives in assets, logic in subclasses).
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        [Header("Identity")]
        public string skillName;
        public string description;

        [Header("Combat")]
        public float damage = 25f;
        public float cooldown = 5f;

        [Header("FX")]
        public string animationTrigger;
        public GameObject vfxPrefab;
        public AudioClip sfxClip;

        /// <summary>
        /// Execute the skill. Called by Fighter.UseSkill().
        /// </summary>
        public abstract void Execute(Fighter caster, Fighter target);

        /// <summary>
        /// Called every frame while skill is active (optional).
        /// </summary>
        public virtual void OnUpdate(Fighter caster) { }

        /// <summary>
        /// Returns skill's effective damage after attacker power bonus.
        /// </summary>
        protected float GetScaledDamage(Fighter caster)
        {
            // Power bonus: power=5 → 1.0x, power=10 → 1.5x, power=1 → 0.6x
            float powerMult = 0.5f + (caster.power / 10f);
            return damage * powerMult;
        }
    }
}
