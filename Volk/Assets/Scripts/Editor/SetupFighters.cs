using UnityEngine;
using UnityEditor;

public class SetupFighters
{
    [MenuItem("Tools/Setup Fighters (Clean)")]
    public static void Setup()
    {
        // --- Player ---
        var playerRoot = GameObject.Find("Player_Root");
        if (playerRoot == null) { Debug.LogError("Player_Root not found!"); return; }

        // Remove old/missing scripts
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(playerRoot);
        RemoveAll<MonoBehaviour>(playerRoot, "PlayerController");
        RemoveAll<MonoBehaviour>(playerRoot, "CombatController");
        RemoveAll<MonoBehaviour>(playerRoot, "HealthSystem");
        RemoveAll<MonoBehaviour>(playerRoot, "EnemyAI");

        // Remove Hurtbox child
        var hurtbox = playerRoot.transform.Find("Hurtbox");
        if (hurtbox != null) Object.DestroyImmediate(hurtbox.gameObject);

        // Add Fighter
        var playerFighter = playerRoot.GetComponent<Fighter>();
        if (playerFighter == null) playerFighter = playerRoot.AddComponent<Fighter>();
        playerFighter.isAI = false;
        playerFighter.enemyTag = "Enemy";
        playerRoot.tag = "Player";
        playerRoot.layer = 0; // Default

        // Setup attack points on Player_Maria
        var playerMaria = playerRoot.transform.Find("Player_Maria");
        if (playerMaria != null)
        {
            var playerAnim = playerMaria.GetComponent<Animator>();
            if (playerAnim != null && playerAnim.isHuman)
            {
                SetupAttackPoints(playerFighter, playerAnim, playerMaria);
            }

            // Remove old relay scripts
            RemoveAll<MonoBehaviour>(playerMaria.gameObject, "AnimationEventRelay");
        }

        // Remove old HitBox GameObjects
        RemoveHitBoxChildren(playerRoot.transform);

        EditorUtility.SetDirty(playerRoot);

        // --- Enemy ---
        var enemyRoot = GameObject.Find("Enemy_Root");
        if (enemyRoot == null) { Debug.LogError("Enemy_Root not found!"); return; }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(enemyRoot);
        RemoveAll<MonoBehaviour>(enemyRoot, "PlayerController");
        RemoveAll<MonoBehaviour>(enemyRoot, "CombatController");
        RemoveAll<MonoBehaviour>(enemyRoot, "HealthSystem");
        RemoveAll<MonoBehaviour>(enemyRoot, "EnemyAI");

        var enemyHurtbox = enemyRoot.transform.Find("Hurtbox");
        if (enemyHurtbox != null) Object.DestroyImmediate(enemyHurtbox.gameObject);

        var enemyFighter = enemyRoot.GetComponent<Fighter>();
        if (enemyFighter == null) enemyFighter = enemyRoot.AddComponent<Fighter>();
        enemyFighter.isAI = true;
        enemyFighter.enemyTag = "Player";
        enemyRoot.tag = "Enemy";
        enemyRoot.layer = 0; // Default

        var enemyKachujin = enemyRoot.transform.Find("Enemy_Kachujin");
        if (enemyKachujin != null)
        {
            var enemyAnim = enemyKachujin.GetComponent<Animator>();
            if (enemyAnim != null && enemyAnim.isHuman)
            {
                SetupAttackPoints(enemyFighter, enemyAnim, enemyKachujin);
            }

            RemoveAll<MonoBehaviour>(enemyKachujin.gameObject, "AnimationEventRelay");
        }

        RemoveHitBoxChildren(enemyRoot.transform);

        EditorUtility.SetDirty(enemyRoot);

        // --- Health bars ---
        SetupHealthBar("PlayerHealthBar", playerFighter);
        SetupHealthBar("EnemyHealthBar", enemyFighter);

        // --- Camera ---
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            var cf = cam.GetComponent<CameraFollow>();
            if (cf != null) { cf.target = playerRoot.transform; EditorUtility.SetDirty(cf); }
        }

        Debug.Log("Fighter setup complete!");
        Debug.Log($"  Player_Root: Fighter(isAI=false), tag=Player");
        Debug.Log($"  Enemy_Root: Fighter(isAI=true), tag=Enemy");
    }

    static void SetupAttackPoints(Fighter fighter, Animator anim, Transform fbx)
    {
        // Right hand point
        Transform rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand != null)
        {
            var rhPoint = rightHand.Find("RightHandPoint");
            if (rhPoint == null)
            {
                var go = new GameObject("RightHandPoint");
                go.transform.SetParent(rightHand, false);
                go.transform.localPosition = Vector3.zero;
                rhPoint = go.transform;
            }
            // Add a small collider on the root for OverlapSphere to detect
            fighter.rightHandPoint = rhPoint;
            Debug.Log($"  RightHandPoint on {rightHand.name}");
        }

        // Right foot point
        Transform rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        if (rightFoot != null)
        {
            var rfPoint = rightFoot.Find("RightFootPoint");
            if (rfPoint == null)
            {
                var go = new GameObject("RightFootPoint");
                go.transform.SetParent(rightFoot, false);
                go.transform.localPosition = Vector3.zero;
                rfPoint = go.transform;
            }
            fighter.rightFootPoint = rfPoint;
            Debug.Log($"  RightFootPoint on {rightFoot.name}");
        }
    }

    static void SetupHealthBar(string barName, Fighter fighter)
    {
        var bar = GameObject.Find(barName);
        if (bar != null)
        {
            var hbui = bar.GetComponent<HealthBarUI>();
            if (hbui != null)
            {
                hbui.target = fighter;
                // Find slider
                var slider = bar.GetComponent<UnityEngine.UI.Slider>();
                if (slider != null) hbui.slider = slider;
                EditorUtility.SetDirty(hbui);
            }
        }
    }

    static void RemoveAll<T>(GameObject go, string typeName) where T : MonoBehaviour
    {
        foreach (var comp in go.GetComponents<T>())
        {
            if (comp == null) continue; // Missing script
            if (comp.GetType().Name == typeName)
                Object.DestroyImmediate(comp);
        }
    }

    static void RemoveHitBoxChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name.StartsWith("HitBox_"))
            {
                Object.DestroyImmediate(child.gameObject);
                continue;
            }
            RemoveHitBoxChildren(child);
        }
    }
}
