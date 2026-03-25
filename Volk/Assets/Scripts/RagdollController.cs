using UnityEngine;
using System.Collections;

/// <summary>
/// Manages ragdoll activation on KO. Keeps ragdoll disabled during gameplay
/// for mobile performance (max 8 Rigidbodies, only active on KO).
/// </summary>
public class RagdollController : MonoBehaviour
{
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Animator anim;
    private CharacterController cc;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        DisableRagdoll();
    }

    void DisableRagdoll()
    {
        foreach (var rb in ragdollBodies)
        {
            if (rb.gameObject == gameObject) continue; // Skip root
            rb.isKinematic = true;
        }
        foreach (var col in ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            if (col is CharacterController) continue;
            col.enabled = false;
        }
    }

    /// <summary>
    /// Activate ragdoll on KO with directional launch force.
    /// </summary>
    public void ActivateRagdoll(Vector3 attackDirection, float force = 5f)
    {
        StartCoroutine(DoRagdoll(attackDirection, force));
    }

    IEnumerator DoRagdoll(Vector3 attackDir, float force)
    {
        // Let death animation play briefly
        yield return new WaitForSeconds(0.5f);

        // Disable animator and character controller
        if (anim != null) anim.enabled = false;
        if (cc != null) cc.enabled = false;

        // Enable ragdoll
        foreach (var col in ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            if (col is CharacterController) continue;
            col.enabled = true;
        }

        // Apply force — limit to 8 bodies for mobile perf
        int bodyCount = 0;
        foreach (var rb in ragdollBodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.isKinematic = false;

            // Apply launch velocity in attack direction + slight upward (single force only)
            Vector3 launchDir = (attackDir.normalized + Vector3.up * 0.3f).normalized;
            rb.linearVelocity = launchDir * force;

            bodyCount++;
            if (bodyCount >= 8) break; // Mobile limit
        }

        // Ground impact after delay
        yield return new WaitForSeconds(0.8f);
        OnGroundImpact();
    }

    void OnGroundImpact()
    {
        // Dust VFX
        HitEffectManager.Instance?.SpawnHitEffect(transform.position + Vector3.up * 0.1f, false);

        // Camera shake
        if (Camera.main != null)
            StartCoroutine(SmallShake());

        // Bass sound
        AudioManager.Instance?.PlayFall();
        VibrationManager.Instance?.VibrateLight();
    }

    IEnumerator SmallShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 orig = cam.transform.localPosition;
        for (int i = 0; i < 4; i++)
        {
            float x = Random.Range(-0.02f, 0.02f);
            float y = Random.Range(-0.02f, 0.02f);
            cam.transform.localPosition = orig + new Vector3(x, y, 0f);
            yield return new WaitForSecondsRealtime(1f / 60f);
        }
        cam.transform.localPosition = orig;
    }

    /// <summary>
    /// Reset ragdoll state for next round.
    /// </summary>
    public void ResetRagdoll()
    {
        StopAllCoroutines();
        DisableRagdoll();
        if (anim != null) anim.enabled = true;
        if (cc != null) cc.enabled = true;
    }
}
