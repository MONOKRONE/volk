using UnityEngine;
using System.Collections;

/// <summary>
/// Manages ragdoll activation on KO. Keeps ragdoll disabled during gameplay
/// for mobile performance (max 8 Rigidbodies, only active on KO).
/// Supports smooth blend from animation to ragdoll (BlendToRagdoll).
/// </summary>
public class RagdollController : MonoBehaviour
{
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Animator anim;
    private CharacterController cc;

    /// <summary>True when ragdoll physics are active (KO state).</summary>
    public bool IsActive { get; private set; }

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
        IsActive = false;
        foreach (var rb in ragdollBodies)
        {
            if (rb.gameObject == gameObject) continue;
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

    /// <summary>
    /// Smooth blend from animator pose to ragdoll over blendTime seconds.
    /// Records current bone transforms, enables ragdoll, then lerps.
    /// </summary>
    public void BlendToRagdoll(float blendTime = 0.3f, Vector3 attackDir = default, float force = 5f)
    {
        StartCoroutine(DoBlendToRagdoll(blendTime, attackDir, force));
    }

    IEnumerator DoBlendToRagdoll(float blendTime, Vector3 attackDir, float force)
    {
        IsActive = true;

        // Capture current animated bone positions
        var bonePositions = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        var boneRotations = new System.Collections.Generic.Dictionary<Transform, Quaternion>();
        foreach (var rb in ragdollBodies)
        {
            if (rb.gameObject == gameObject) continue;
            bonePositions[rb.transform] = rb.transform.position;
            boneRotations[rb.transform] = rb.transform.rotation;
        }

        // Disable animator and CC
        if (anim != null) anim.enabled = false;
        if (cc != null) cc.enabled = false;

        // Enable ragdoll colliders
        foreach (var col in ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            if (col is CharacterController) continue;
            col.enabled = true;
        }

        // Enable ragdoll bodies and apply force
        int bodyCount = 0;
        foreach (var rb in ragdollBodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.isKinematic = false;
            if (attackDir.sqrMagnitude > 0.001f)
            {
                Vector3 launchDir = (attackDir.normalized + Vector3.up * 0.3f).normalized;
                rb.velocity = launchDir * force;
            }
            bodyCount++;
            if (bodyCount >= 8) break;
        }

        // Blend from animated pose to ragdoll pose
        float elapsed = 0f;
        while (elapsed < blendTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendTime);
            // Ease out for natural feel
            float smooth = t * t * (3f - 2f * t);

            foreach (var rb in ragdollBodies)
            {
                if (rb.gameObject == gameObject) continue;
                if (!bonePositions.ContainsKey(rb.transform)) continue;

                // Blend position and rotation from animated to physics
                rb.transform.position = Vector3.Lerp(bonePositions[rb.transform], rb.transform.position, smooth);
                rb.transform.rotation = Quaternion.Slerp(boneRotations[rb.transform], rb.transform.rotation, smooth);
            }
            yield return null;
        }

        // Ground impact after delay
        yield return new WaitForSeconds(0.8f);
        OnGroundImpact();
    }

    IEnumerator DoRagdoll(Vector3 attackDir, float force)
    {
        // Let death animation play briefly
        yield return new WaitForSeconds(0.5f);

        IsActive = true;

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
            Vector3 launchDir = (attackDir.normalized + Vector3.up * 0.3f).normalized;
            rb.velocity = launchDir * force;
            bodyCount++;
            if (bodyCount >= 8) break;
        }

        // Ground impact after delay
        yield return new WaitForSeconds(0.8f);
        OnGroundImpact();
    }

    void OnGroundImpact()
    {
        HitEffectManager.Instance?.SpawnHitEffect(transform.position + Vector3.up * 0.1f, false);
        if (Camera.main != null)
            StartCoroutine(SmallShake());
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
