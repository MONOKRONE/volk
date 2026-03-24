using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Grabs target, deals high damage, ignores a portion of defense.
    /// Used by: KAYA Ayı Kucağı
    /// </summary>
    [CreateAssetMenu(fileName = "NewGrappleSkill", menuName = "VOLK/Skills/Grapple Skill")]
    public class GrappleSkill : SkillBase
    {
        [Header("Grapple")]
        public float grabRange = 1.8f;
        [Range(0f, 1f)] public float defenseBypass = 0.4f; // % of defense ignored

        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            if (dist > grabRange) return;

            // Partially bypass defense
            float effectiveDefense = target.defense * (1f - defenseBypass);
            // Pre-calculated damage with partial defense bypass — pass without attacker to skip double-calc
            float dmg = Fighter.CalculateDamage(GetScaledDamage(caster), caster.power, effectiveDefense);
            target.TakeDamage(dmg, caster.transform.position, true, null);
            target.ApplyStun(0.8f);
        }
    }
}
