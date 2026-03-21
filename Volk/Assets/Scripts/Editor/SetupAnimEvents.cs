using UnityEngine;
using UnityEditor;

public class SetupAnimEvents
{
    [MenuItem("Tools/Setup Animation Events")]
    public static void Setup()
    {
        SetupClipEvents("Assets/Animations/HookPunch.fbx", new[]
        {
            new AnimEventData { timePercent = 0.20f, functionName = "EnableRightHandHitBox" },
            new AnimEventData { timePercent = 0.60f, functionName = "DisableRightHandHitBox" },
            new AnimEventData { timePercent = 0.95f, functionName = "OnAttackEnd" }
        });

        SetupClipEvents("Assets/Animations/MMAKick.fbx", new[]
        {
            new AnimEventData { timePercent = 0.25f, functionName = "EnableRightFootHitBox" },
            new AnimEventData { timePercent = 0.65f, functionName = "DisableRightFootHitBox" },
            new AnimEventData { timePercent = 0.95f, functionName = "OnAttackEnd" }
        });

        Debug.Log("Animation events setup complete!");
    }

    struct AnimEventData
    {
        public float timePercent;
        public string functionName;
    }

    static void SetupClipEvents(string path, AnimEventData[] events)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("Could not find importer for: " + path);
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips.Length == 0)
            clips = importer.defaultClipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            float duration = clips[i].lastFrame - clips[i].firstFrame;
            AnimationEvent[] animEvents = new AnimationEvent[events.Length];

            for (int e = 0; e < events.Length; e++)
            {
                animEvents[e] = new AnimationEvent
                {
                    time = clips[i].firstFrame + duration * events[e].timePercent,
                    functionName = events[e].functionName
                };
                Debug.Log($"{path} clip '{clips[i].name}': {events[e].functionName} at frame {animEvents[e].time:F1}");
            }

            clips[i].events = animEvents;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        Debug.Log("Reimported: " + path);
    }
}
