using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Deals damage and applies heavy knockback.
    /// Used by: YILDIZ Girdap Tekme
    /// </summary>
    [CreateAssetMenu(fileName = "NewKnockbackSkill", menuName = "VOLK/Skills/Knockback Skill")]
    public class KnockbackSkill : SkillBase
    {
        [Header("Knockback")]
        public float knockbackMultiplier = 3f; // Multiplier on top of base knockback

        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            float dmg = GetScaledDamage(caster);
            target.TakeDamage(dmg, caster.transform.position, true, caster);
            // Extra knockback burst via reflection (Fighter exposes knockback fields)
            target.ApplyKnockback(caster.transform.position, knockbackMultiplier);
        }
    }
}
