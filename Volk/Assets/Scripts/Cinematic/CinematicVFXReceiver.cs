using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Volk.Cinematic
{
    /// <summary>
    /// Receives Timeline signals and triggers VFX (hit effects, screen flash, slow-mo).
    /// Attach to the CinematicDirector GameObject alongside SignalReceiver.
    /// </summary>
    public class CinematicVFXReceiver : MonoBehaviour
    {
        [Header("VFX References")]
        public HitEffectManager hitEffectManager;
        public JuiceManager juiceManager;

        [Header("Hit Effect Settings")]
        public Transform hitSpawnPoint;

        public void OnHitEffect()
        {
            if (hitEffectManager != null && hitSpawnPoint != null)
                hitEffectManager.SpawnHitEffect(hitSpawnPoint.position);
            else
                Debug.Log("[Cinematic] HitEffect signal fired (no manager assigned)");
        }

        public void OnScreenFlash()
        {
            if (juiceManager != null)
                juiceManager.ScreenFlash(0.5f);
            else
                Debug.Log("[Cinematic] ScreenFlash signal fired (no manager assigned)");
        }

        public void OnSlowMotionKO()
        {
            if (juiceManager != null)
                juiceManager.SlowMotionKO(0.15f, 1.5f);
            else
                Debug.Log("[Cinematic] SlowMotionKO signal fired (no manager assigned)");
        }
    }
}
