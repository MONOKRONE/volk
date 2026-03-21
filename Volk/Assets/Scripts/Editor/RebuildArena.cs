using UnityEngine;
using UnityEditor;

public class RebuildArena
{
    [MenuItem("Tools/Rebuild Arena Walls")]
    public static void Rebuild()
    {
        // Delete all existing Cube objects
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name == "Cube" || go.name.StartsWith("Cube ("))
                Undo.DestroyObjectImmediate(go);
        }

        CreateWall("Wall_North", new Vector3(0, 1.5f, 50), new Vector3(100, 3, 1));
        CreateWall("Wall_South", new Vector3(0, 1.5f, -50), new Vector3(100, 3, 1));
        CreateWall("Wall_East", new Vector3(50, 1.5f, 0), new Vector3(1, 3, 100));
        CreateWall("Wall_West", new Vector3(-50, 1.5f, 0), new Vector3(1, 3, 100));

        Debug.Log("Arena walls rebuilt!");
    }

    static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.isStatic = true;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(wall, "Create " + name);
        Debug.Log($"Created {name} at {position}");
    }
}
