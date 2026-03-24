using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Rapid multi-hit combo: deals damage N times with short delays.
    /// Used by: RÜZGAR Fırtına Serisi
    /// </summary>
    [CreateAssetMenu(fileName = "NewMultiHitSkill", menuName = "VOLK/Skills/Multi-Hit Skill")]
    public class MultiHitSkill : SkillBase
    {
        [Header("Multi-Hit")]
        public int hitCount = 4;
        public float timeBetweenHits = 0.12f;
        [Range(0f, 1f)] public float damagePerHit = 0.3f; // fraction of total damage per hit

        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null || target.isDead) return;
            caster.StartCoroutine(MultiHitRoutine(caster, target));
        }

        System.Collections.IEnumerator MultiHitRoutine(Fighter caster, Fighter target)
        {
            float singleHit = GetScaledDamage(caster) * damagePerHit;
            for (int i = 0; i < hitCount; i++)
            {
                if (target == null || target.isDead) yield break;
                target.TakeDamage(singleHit, caster.transform.position, true, caster);
                yield return new WaitForSeconds(timeBetweenHits);
            }
        }
    }
}
