using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Basic damage skill — hits target for scaled damage.
    /// Used by: YILDIZ Alev Yumruk
    /// </summary>
    [CreateAssetMenu(fileName = "NewDamageSkill", menuName = "VOLK/Skills/Damage Skill")]
    public class DamageSkill : SkillBase
    {
        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            float dmg = GetScaledDamage(caster);
            target.TakeDamage(dmg, caster.transform.position, true, caster);
        }
    }
}
