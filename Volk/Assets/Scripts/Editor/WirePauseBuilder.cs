using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class WirePauseBuilder
{
    [MenuItem("Tools/Wire Pause Menu Builder")]
    public static void Wire()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");

        var pauseCanvas = GameObject.Find("PauseCanvas");
        if (pauseCanvas == null) { Debug.LogError("PauseCanvas not found!"); return; }

        // Ensure PauseMenu exists
        var pm = pauseCanvas.GetComponent<PauseMenu>();
        if (pm == null) pm = pauseCanvas.AddComponent<PauseMenu>();

        // Ensure PauseMenuBuilder exists and is wired
        var builder = pauseCanvas.GetComponent<PauseMenuBuilder>();
        if (builder == null) builder = pauseCanvas.AddComponent<PauseMenuBuilder>();
        builder.pauseMenu = pm;

        // Clear old pauseContainer/children that were editor-created
        // PauseMenuBuilder will recreate at runtime
        for (int i = pauseCanvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = pauseCanvas.transform.GetChild(i);
            if (child.name == "PauseContainer" || child.name == "Overlay")
                Object.DestroyImmediate(child.gameObject);
        }

        EditorUtility.SetDirty(pauseCanvas);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("PauseMenuBuilder wired to PauseCanvas!");
    }
}
