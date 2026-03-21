using UnityEngine;
using UnityEditor;

public class FixPhysicsMatrix
{
    [MenuItem("Tools/Fix Physics Matrix")]
    public static void Fix()
    {
        // First, reset ALL layer collisions to default (enabled)
        for (int i = 0; i < 32; i++)
            for (int j = i; j < 32; j++)
                Physics.IgnoreLayerCollision(i, j, false);

        // Now set specific overrides
        Physics.IgnoreLayerCollision(0, 0, false);  // Default vs Default: ALLOW
        Physics.IgnoreLayerCollision(0, 8, true);    // Default vs Hitbox: IGNORE
        Physics.IgnoreLayerCollision(0, 9, true);    // Default vs Hurtbox: IGNORE
        Physics.IgnoreLayerCollision(8, 9, false);   // Hitbox vs Hurtbox: ALLOW
        Physics.IgnoreLayerCollision(8, 8, true);    // Hitbox vs Hitbox: IGNORE
        Physics.IgnoreLayerCollision(9, 9, true);    // Hurtbox vs Hurtbox: IGNORE

        Debug.Log("Physics matrix fixed:");
        Debug.Log($"  Default(0) vs Default(0): {!Physics.GetIgnoreLayerCollision(0, 0)}");
        Debug.Log($"  Default(0) vs Hitbox(8):  {!Physics.GetIgnoreLayerCollision(0, 8)}");
        Debug.Log($"  Default(0) vs Hurtbox(9): {!Physics.GetIgnoreLayerCollision(0, 9)}");
        Debug.Log($"  Hitbox(8) vs Hurtbox(9):  {!Physics.GetIgnoreLayerCollision(8, 9)}");
        Debug.Log($"  Hitbox(8) vs Hitbox(8):   {!Physics.GetIgnoreLayerCollision(8, 8)}");
        Debug.Log($"  Hurtbox(9) vs Hurtbox(9): {!Physics.GetIgnoreLayerCollision(9, 9)}");

        // Check Arena_Floor collider
        var floor = GameObject.Find("Arena_Floor");
        if (floor != null)
        {
            var mc = floor.GetComponent<MeshCollider>();
            var bc = floor.GetComponent<BoxCollider>();
            Debug.Log($"  Arena_Floor: MeshCollider={mc != null}, BoxCollider={bc != null}, layer={floor.layer} ({LayerMask.LayerToName(floor.layer)})");
        }
    }
}
