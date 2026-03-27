using UnityEngine;
using Volk.Core;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance;

    [Header("Hit Particles (Legacy)")]
    public GameObject punchHitPrefab;
    public GameObject kickHitPrefab;
    public GameObject blockHitPrefab;

    [Header("Tier-Based VFX")]
    public GameObject lightHitPrefab;    // Small spark
    public GameObject mediumHitPrefab;   // Standard impact
    public GameObject heavyHitPrefab;    // Large burst + screen shake
    public GameObject skillHitPrefab;    // Colored energy burst

    [Header("Tier Colors")]
    public Color lightColor = new Color(1f, 1f, 1f, 0.8f);
    public Color mediumColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color heavyColor = new Color(1f, 0.3f, 0.1f, 1f);
    public Color skillColor = new Color(0.4f, 0.6f, 1f, 1f);

    [Header("Tier Scale")]
    public float lightScale = 0.5f;
    public float mediumScale = 1f;
    public float heavyScale = 1.5f;
    public float skillScale = 1.8f;

    [Header("Pool Settings")]
    public int poolPreWarm = 10;

    // Object pools
    private SimplePool punchPool;
    private SimplePool kickPool;
    private SimplePool blockPool;
    private SimplePool lightPool;
    private SimplePool mediumPool;
    private SimplePool heavyPool;
    private SimplePool skillPool;

    // Cached camera reference
    private CameraFollow cachedCamFollow;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Pre-warm pools
        if (punchHitPrefab) punchPool = new SimplePool(punchHitPrefab, poolPreWarm, transform);
        if (kickHitPrefab) kickPool = new SimplePool(kickHitPrefab, poolPreWarm, transform);
        if (blockHitPrefab) blockPool = new SimplePool(blockHitPrefab, poolPreWarm / 2, transform);
        if (lightHitPrefab) lightPool = new SimplePool(lightHitPrefab, poolPreWarm, transform);
        if (mediumHitPrefab) mediumPool = new SimplePool(mediumHitPrefab, poolPreWarm, transform);
        if (heavyHitPrefab) heavyPool = new SimplePool(heavyHitPrefab, poolPreWarm / 2, transform);
        if (skillHitPrefab) skillPool = new SimplePool(skillHitPrefab, poolPreWarm / 2, transform);

        // Cache camera
        if (Camera.main != null)
            cachedCamFollow = Camera.main.GetComponent<CameraFollow>();
    }

    // Legacy API — unchanged signature
    public void SpawnHitEffect(Vector3 position, bool isKick = false, bool isBlock = false)
    {
        SimplePool pool = isBlock ? blockPool : (isKick ? kickPool : punchPool);
        if (pool != null)
        {
            pool.GetTimed(position, Quaternion.identity, 2f, this);
            return;
        }

        // Fallback if no pool (prefab was null)
        GameObject prefab = isBlock ? blockHitPrefab : (isKick ? kickHitPrefab : punchHitPrefab);
        if (prefab == null) return;
        var fx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(fx, 2f);
    }

    // Tier-based VFX system
    public void SpawnTieredEffect(Vector3 position, HitTier tier)
    {
        SimplePool pool;
        GameObject fallbackPrefab;
        Color color;
        float scale;

        switch (tier)
        {
            case HitTier.Light:
                pool = lightPool ?? punchPool;
                fallbackPrefab = lightHitPrefab ?? punchHitPrefab;
                color = lightColor;
                scale = lightScale;
                break;
            case HitTier.Heavy:
                pool = heavyPool ?? kickPool;
                fallbackPrefab = heavyHitPrefab ?? kickHitPrefab;
                color = heavyColor;
                scale = heavyScale;
                break;
            case HitTier.Skill:
                pool = skillPool ?? kickPool;
                fallbackPrefab = skillHitPrefab ?? kickHitPrefab;
                color = skillColor;
                scale = skillScale;
                break;
            default: // Medium
                pool = mediumPool ?? punchPool;
                fallbackPrefab = mediumHitPrefab ?? punchHitPrefab;
                color = mediumColor;
                scale = mediumScale;
                break;
        }

        GameObject fx;
        if (pool != null)
        {
            fx = pool.GetTimed(position, Quaternion.identity, 3f, this);
        }
        else
        {
            if (fallbackPrefab == null) return;
            fx = Instantiate(fallbackPrefab, position, Quaternion.identity);
            Destroy(fx, 3f);
        }

        fx.transform.localScale = Vector3.one * scale;

        // Apply color to particle systems
        var particles = fx.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }

        // Apply color to renderers
        var renderers = fx.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
                r.material.color = color;
            if (r.material.HasProperty("_EmissionColor"))
                r.material.SetColor("_EmissionColor", color * 2f);
        }

        // Heavy/skill: trigger screen effects
        if (tier == HitTier.Heavy || tier == HitTier.Skill)
        {
            JuiceManager.Instance?.ScreenFlash(tier == HitTier.Skill ? 0.5f : 0.3f);
            if (cachedCamFollow != null)
                cachedCamFollow.TriggerShake(tier == HitTier.Skill ? 0.15f : 0.1f);
        }

        // Hitstop scaling by tier
        float hitstopDuration = tier switch
        {
            HitTier.Light => HitstopManager.LightHit,
            HitTier.Medium => HitstopManager.LightHit * 1.2f,
            HitTier.Heavy => HitstopManager.HeavyHit,
            HitTier.Skill => HitstopManager.SkillHit,
            _ => HitstopManager.LightHit
        };
        HitstopManager.Instance?.Trigger(hitstopDuration);
    }
}
