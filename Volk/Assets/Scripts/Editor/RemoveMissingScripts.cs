using UnityEngine;
using UnityEditor;

public class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts")]
    static void RemoveFromAll()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                Debug.Log($"  Removed {removed} from {go.name}");
                count += removed;
            }
        }
        Debug.Log($"Total removed: {count} missing script(s)");
    }
}
