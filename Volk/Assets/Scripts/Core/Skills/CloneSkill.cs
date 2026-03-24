using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Spawns a decoy clone that draws AI attention and deals contact damage.
    /// Used by: SİS Gölge Klon
    /// </summary>
    [CreateAssetMenu(fileName = "NewCloneSkill", menuName = "VOLK/Skills/Clone Skill")]
    public class CloneSkill : SkillBase
    {
        [Header("Clone")]
        public float cloneDuration = 3f;
        public float cloneContactDamage = 15f;
        public Vector3 spawnOffset = new Vector3(1.5f, 0f, 0f);

        public override void Execute(Fighter caster, Fighter target)
        {
            // Spawn a ghost copy of the caster's GameObject
            GameObject clone = Object.Instantiate(caster.gameObject,
                caster.transform.position + caster.transform.TransformDirection(spawnOffset),
                caster.transform.rotation);

            // Disable all scripts on clone so it's a static dummy
            foreach (var mono in clone.GetComponentsInChildren<MonoBehaviour>())
                if (mono != null) mono.enabled = false;

            // Add contact damage trigger (uses WallDamageTrigger pattern)
            var col = clone.GetComponent<Collider>();
            if (col == null) col = clone.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            clone.AddComponent<WallDamageTrigger>().Init(cloneContactDamage, caster);

            // Visual — make it semi-transparent
            foreach (var r in clone.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = 0.4f;
                        mat.color = c;
                    }

            caster.StartCoroutine(DestroyAfter(clone, cloneDuration));
        }

        System.Collections.IEnumerator DestroyAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) Object.Destroy(obj);
        }
    }
}
