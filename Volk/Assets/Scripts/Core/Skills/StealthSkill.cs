using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Briefly makes caster semi-invisible and boosts next attack's damage.
    /// Used by: SİS Sis Perdesi
    /// </summary>
    [CreateAssetMenu(fileName = "NewStealthSkill", menuName = "VOLK/Skills/Stealth Skill")]
    public class StealthSkill : SkillBase
    {
        [Header("Stealth")]
        public float stealthDuration = 1.5f;
        [Range(0f, 1f)] public float stealthOpacity = 0.2f;
        public float nextAttackBonus = 1.5f; // damage multiplier for next attack

        public override void Execute(Fighter caster, Fighter target)
        {
            caster.StartCoroutine(StealthRoutine(caster));
        }

        System.Collections.IEnumerator StealthRoutine(Fighter caster)
        {
            // Visual — fade out renderers
            Renderer[] renderers = caster.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = stealthOpacity;
                        mat.color = c;
                    }

            caster.SetNextAttackBonus(nextAttackBonus);
            yield return new WaitForSeconds(stealthDuration);

            // Fade back
            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = 1f;
                        mat.color = c;
                    }
        }
    }
}
