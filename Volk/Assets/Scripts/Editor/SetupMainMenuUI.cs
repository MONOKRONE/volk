using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verifies and wires all MainMenu UI scripts to scene GameObjects.
/// Menu: VOLK > Setup > Wire MainMenu UI
/// </summary>
public class SetupMainMenuUI
{
    static readonly string[] requiredScripts = new[]
    {
        "MainHubUI",
        "VTabBar",
        "ShopUI",
        "BattlePassUI",
        "GhostProfileUI",
        "ClanUI",
        "RankedUI",
        "SettingsUI"
    };

    [MenuItem("VOLK/Setup/Wire MainMenu UI")]
    public static void WireMainMenuUI()
    {
        // Ensure MainMenu scene is loaded
        var scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains("MainMenu"))
        {
            string[] guids = AssetDatabase.FindAssets("MainMenu t:Scene");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                Debug.LogError("[SetupMainMenuUI] MainMenu scene not found!");
                return;
            }
        }

        int wired = 0;
        int alreadyOk = 0;

        foreach (string scriptName in requiredScripts)
        {
            // Check if any GameObject already has this script
            var existing = FindComponentByTypeName(scriptName);
            if (existing != null)
            {
                alreadyOk++;
                Debug.Log($"[MainMenuUI] OK: {scriptName} found on '{existing.gameObject.name}'");
                continue;
            }

            // Try to find a GameObject with matching name
            GameObject target = GameObject.Find(scriptName);
            if (target == null)
            {
                // Try common naming patterns
                target = GameObject.Find(scriptName.Replace("UI", ""));
                if (target == null)
                    target = GameObject.Find(scriptName + "Panel");
            }

            if (target == null)
            {
                // Create a new panel GameObject under Canvas
                var canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError($"[MainMenuUI] No Canvas found! Cannot create {scriptName} panel.");
                    continue;
                }

                target = new GameObject(scriptName);
                target.transform.SetParent(canvas.transform, false);
                var rt = target.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                target.SetActive(false); // Hidden by default, VTabBar activates
                Debug.Log($"[MainMenuUI] CREATED: {scriptName} panel under Canvas");
            }

            // Attach the script
            var type = FindType(scriptName);
            if (type != null && target.GetComponent(type) == null)
            {
                target.AddComponent(type);
                wired++;
                Debug.Log($"[MainMenuUI] WIRED: {scriptName} to '{target.name}'");
            }
            else if (type == null)
            {
                Debug.LogWarning($"[MainMenuUI] Type '{scriptName}' not found in assemblies!");
            }
        }

        if (wired > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[MainMenuUI] Done: {alreadyOk} already OK, {wired} newly wired.");
    }

    static Component FindComponentByTypeName(string typeName)
    {
        var type = FindType(typeName);
        if (type == null) return null;
        return Object.FindFirstObjectByType(type) as Component;
    }

    static System.Type FindType(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == typeName && typeof(MonoBehaviour).IsAssignableFrom(type))
                    return type;
            }
        }
        return null;
    }
}
