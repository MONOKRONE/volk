using UnityEngine;
using UnityEditor;

public class VerifyFighters
{
    [MenuItem("Tools/Verify Fighters")]
    public static void Verify()
    {
        Check("Player_Root");
        Check("Enemy_Root");
    }

    static void Check(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) { Debug.LogError(name + " NOT FOUND"); return; }

        Debug.Log($"=== {name} ===");
        Debug.Log($"  tag='{go.tag}', layer={go.layer}");

        var f = go.GetComponent<Fighter>();
        if (f != null)
            Debug.Log($"  Fighter: isAI={f.isAI}, enemyTag='{f.enemyTag}', difficulty={f.difficulty}, rightHand={f.rightHandPoint?.name ?? "NULL"}, rightFoot={f.rightFootPoint?.name ?? "NULL"}");
        else
            Debug.LogError("  NO Fighter component!");

        var cc = go.GetComponent<CharacterController>();
        Debug.Log($"  CharacterController: {(cc != null ? $"center={cc.center}, h={cc.height}" : "MISSING")}");
    }
}
