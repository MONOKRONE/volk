using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class FixEventSystem
{
    [MenuItem("Tools/Fix EventSystem For Touch")]
    public static void Fix()
    {
        // Find or create EventSystem
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            Debug.Log("Created EventSystem");
        }

        // Remove StandaloneInputModule if present
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
            Debug.Log("Removed StandaloneInputModule");
        }

#if ENABLE_INPUT_SYSTEM
        // Add InputSystemUIInputModule if missing
        var inputSystemModule = es.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            Debug.Log("Added InputSystemUIInputModule");
        }
        else
        {
            Debug.Log("InputSystemUIInputModule already present");
        }
#else
        // Fallback: ensure StandaloneInputModule exists
        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
            Debug.Log("Added StandaloneInputModule (old Input System)");
        }
#endif

        // Check TouchCanvas joystick area has Image for raycast
        var joystickArea = GameObject.Find("JoystickArea");
        if (joystickArea != null)
        {
            var img = joystickArea.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.raycastTarget = true;
                Debug.Log("JoystickArea raycastTarget = true");
            }
        }

        EditorUtility.SetDirty(es.gameObject);
        Debug.Log($"EventSystem: {es.gameObject.name}");
        foreach (var module in es.GetComponents<BaseInputModule>())
            Debug.Log($"  InputModule: {module.GetType().Name}");
    }
}
