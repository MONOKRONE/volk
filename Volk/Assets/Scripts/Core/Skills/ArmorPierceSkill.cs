using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Deals damage while completely ignoring defender's defense stat.
    /// Used by: ÇELİK Hassas Vuruş
    /// </summary>
    [CreateAssetMenu(fileName = "NewArmorPierceSkill", menuName = "VOLK/Skills/Armor Pierce Skill")]
    public class ArmorPierceSkill : SkillBase
    {
        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            // Bypass defense entirely: pass rawDamage directly, no attacker reference
            float dmg = GetScaledDamage(caster);
            target.TakeDamage(dmg, caster.transform.position, true); // no attacker = no defense calc
        }
    }
}
