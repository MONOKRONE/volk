using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public static HitEffectManager Instance;

    [Header("Hit Particles")]
    public GameObject punchHitPrefab;
    public GameObject kickHitPrefab;
    public GameObject blockHitPrefab;

    void Awake() { Instance = this; }

    public void SpawnHitEffect(Vector3 position, bool isKick = false, bool isBlock = false)
    {
        GameObject prefab = isBlock ? blockHitPrefab : (isKick ? kickHitPrefab : punchHitPrefab);
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        Destroy(fx, 2f);
    }
}
