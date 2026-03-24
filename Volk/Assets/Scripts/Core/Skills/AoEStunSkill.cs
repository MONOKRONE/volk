using UnityEngine;

namespace Volk.Core
{
    /// <summary>
    /// Area of effect stun — damages all enemies in radius and stuns them.
    /// Used by: KAYA Deprem Darbesi
    /// </summary>
    [CreateAssetMenu(fileName = "NewAoEStunSkill", menuName = "VOLK/Skills/AoE Stun Skill")]
    public class AoEStunSkill : SkillBase
    {
        [Header("AoE")]
        public float radius = 2.5f;
        public float stunDuration = 1.2f;

        public override void Execute(Fighter caster, Fighter target)
        {
            float dmg = GetScaledDamage(caster);
            Collider[] hits = Physics.OverlapSphere(caster.transform.position, radius);
            foreach (var hit in hits)
            {
                Fighter f = hit.GetComponentInParent<Fighter>();
                if (f == null || f == caster || f.isDead) continue;
                if (!hit.CompareTag(caster.enemyTag)) continue;
                f.TakeDamage(dmg, caster.transform.position, true, caster);
                f.ApplyStun(stunDuration);
            }
        }
    }
}
