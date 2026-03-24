using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Dash toward target and deal damage on arrival.
    /// Used by: RÜZGAR Gölge Adım
    /// </summary>
    [CreateAssetMenu(fileName = "NewDashSkill", menuName = "VOLK/Skills/Dash Skill")]
    public class DashSkill : SkillBase
    {
        [Header("Dash")]
        public float dashSpeed = 18f;
        public float dashDuration = 0.18f;

        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            caster.StartCoroutine(DashRoutine(caster, target));
        }

        System.Collections.IEnumerator DashRoutine(Fighter caster, Fighter target)
        {
            Vector3 dir = (target.transform.position - caster.transform.position).normalized;
            dir.y = 0;
            float elapsed = 0f;
            bool hitDealt = false;

            // Cache CharacterController once — avoid per-frame GetComponent
            UnityEngine.CharacterController cc = caster.GetComponent<UnityEngine.CharacterController>();

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                cc?.Move(dir * dashSpeed * Time.deltaTime);

                // Hit on close proximity
                if (!hitDealt && Vector3.Distance(caster.transform.position, target.transform.position) < 1.2f)
                {
                    float dmg = GetScaledDamage(caster);
                    target.TakeDamage(dmg, caster.transform.position, true, caster);
                    hitDealt = true;
                }
                yield return null;
            }

            // Guarantee hit even if missed during dash
            if (!hitDealt && !target.isDead)
            {
                float dmg = GetScaledDamage(caster) * 0.6f;
                target.TakeDamage(dmg, caster.transform.position, true, caster);
            }
        }
    }
}
