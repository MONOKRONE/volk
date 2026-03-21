using UnityEngine;
using UnityEditor;

public class FixTags
{
    [MenuItem("Tools/Fix Tags And Colliders")]
    public static void Fix()
    {
        // Add "Enemy" tag if it doesn't exist
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");

        bool enemyTagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Enemy")
            {
                enemyTagExists = true;
                break;
            }
        }

        if (!enemyTagExists)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Enemy";
            tagManager.ApplyModifiedProperties();
            Debug.Log("Added 'Enemy' tag to TagManager");
        }

        // Set Enemy_Kachujin tag
        var enemy = GameObject.Find("Enemy_Kachujin");
        if (enemy != null)
        {
            enemy.tag = "Enemy";
            EditorUtility.SetDirty(enemy);
            Debug.Log($"Enemy_Kachujin tag set to: '{enemy.tag}'");
        }

        // Add CapsuleCollider to Player_Maria if missing
        var player = GameObject.Find("Player_Maria");
        if (player != null)
        {
            var capsule = player.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = player.AddComponent<CapsuleCollider>();
                capsule.height = 1.8f;
                capsule.radius = 0.4f;
                capsule.center = new Vector3(0, 0.9f, 0);
                EditorUtility.SetDirty(player);
                Debug.Log("Added CapsuleCollider to Player_Maria");
            }
            Debug.Log($"Player_Maria tag: '{player.tag}'");
        }

        // Verify Enemy CapsuleCollider
        if (enemy != null)
        {
            var capsule = enemy.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height = 1.8f;
                capsule.radius = 0.4f;
                capsule.center = new Vector3(0, 0.9f, 0);
                EditorUtility.SetDirty(enemy);
                Debug.Log($"Enemy CapsuleCollider updated: center={capsule.center}, height={capsule.height}, radius={capsule.radius}");
            }
        }

        Debug.Log("Tags and colliders fix complete!");
    }
}
