using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Raises a temporary wall/obstacle that blocks movement and deals contact damage.
    /// Used by: TOPRAK Duvar Yükselt
    /// </summary>
    [CreateAssetMenu(fileName = "NewWallSkill", menuName = "VOLK/Skills/Wall Skill")]
    public class WallSkill : SkillBase
    {
        [Header("Wall")]
        public float wallDuration = 2.5f;
        public float wallHeight = 2f;
        public float wallWidth = 1.5f;
        public float spawnDistance = 1.5f; // spawn in front of caster

        public override void Execute(Fighter caster, Fighter target)
        {
            Vector3 spawnPos = caster.transform.position + caster.transform.forward * spawnDistance;
            spawnPos.y = 0f;

            GameObject wall;
            if (vfxPrefab != null)
            {
                wall = Object.Instantiate(vfxPrefab, spawnPos, caster.transform.rotation);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = spawnPos + Vector3.up * (wallHeight / 2f);
                wall.transform.localScale = new Vector3(wallWidth, wallHeight, 0.3f);
                wall.transform.rotation = caster.transform.rotation;

                // Simple material tint
                var renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.5f, 0.35f, 0.2f); // earthy brown
            }

            // Damage trigger on contact
            wall.AddComponent<WallDamageTrigger>().Init(damage, caster);
            caster.StartCoroutine(DestroyAfter(wall, wallDuration));
        }

        System.Collections.IEnumerator DestroyAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) Object.Destroy(obj);
        }
    }

    /// <summary>
    /// Helper component attached to wall — deals damage on first contact.
    /// </summary>
    public class WallDamageTrigger : MonoBehaviour
    {
        float damage;
        Fighter caster;
        bool triggered;

        public void Init(float dmg, Fighter c) { damage = dmg; caster = c; }

        void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            Fighter f = other.GetComponentInParent<Fighter>();
            if (f == null || f == caster || f.isDead) return;
            f.TakeDamage(damage, caster.transform.position, true, caster);
            triggered = true;
        }
    }
}
