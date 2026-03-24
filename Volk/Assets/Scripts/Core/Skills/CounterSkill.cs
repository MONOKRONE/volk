using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Opens a counter window. If hit during window, reflects damage back.
    /// Used by: ÇELİK Ayna Kalkan
    /// </summary>
    [CreateAssetMenu(fileName = "NewCounterSkill", menuName = "VOLK/Skills/Counter Skill")]
    public class CounterSkill : SkillBase
    {
        [Header("Counter")]
        public float counterWindow = 1.0f;   // seconds the counter is active
        public float reflectMultiplier = 1.5f; // reflected damage multiplier

        public override void Execute(Fighter caster, Fighter target)
        {
            caster.StartCoroutine(CounterRoutine(caster));
        }

        System.Collections.IEnumerator CounterRoutine(Fighter caster)
        {
            caster.SetCounterActive(true, reflectMultiplier);
            yield return new WaitForSeconds(counterWindow);
            caster.SetCounterActive(false, 0f);
        }
    }
}
