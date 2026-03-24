using UnityEngine;
using System.Collections;

namespace Volk.Core
{
    /// <summary>
    /// Launches a projectile toward target. If no vfxPrefab, uses a physics sphere.
    /// Used by: TOPRAK Taş Fırlatma
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileSkill", menuName = "VOLK/Skills/Projectile Skill")]
    public class ProjectileSkill : SkillBase
    {
        [Header("Projectile")]
        public float projectileSpeed = 12f;
        public float projectileLifetime = 3f;
        public float projectileRadius = 0.25f;

        public override void Execute(Fighter caster, Fighter target)
        {
            if (target == null) return;
            caster.StartCoroutine(LaunchProjectile(caster, target));
        }

        System.Collections.IEnumerator LaunchProjectile(Fighter caster, Fighter target)
        {
            // Use vfxPrefab if assigned, else create primitive sphere
            GameObject proj;
            if (vfxPrefab != null)
            {
                proj = Object.Instantiate(vfxPrefab, caster.transform.position + Vector3.up * 1f, Quaternion.identity);
            }
            else
            {
                proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                proj.transform.position = caster.transform.position + Vector3.up * 1f;
                proj.transform.localScale = Vector3.one * (projectileRadius * 2f);
                Object.Destroy(proj.GetComponent<SphereCollider>()); // use manual overlap
            }

            float elapsed = 0f;
            bool hit = false;
            float dmg = GetScaledDamage(caster);

            while (elapsed < projectileLifetime && proj != null && !hit)
            {
                elapsed += Time.deltaTime;
                Vector3 targetPos = target != null ? target.transform.position + Vector3.up : proj.transform.position;
                proj.transform.position = Vector3.MoveTowards(proj.transform.position, targetPos, projectileSpeed * Time.deltaTime);

                // Hit check
                if (target != null && !target.isDead &&
                    Vector3.Distance(proj.transform.position, target.transform.position + Vector3.up) < 0.8f)
                {
                    target.TakeDamage(dmg, caster.transform.position, true, caster);
                    hit = true;
                }
                yield return null;
            }

            if (proj != null) Object.Destroy(proj);
        }
    }
}
