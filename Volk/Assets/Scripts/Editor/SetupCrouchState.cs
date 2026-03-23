using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupCrouchState
{
    [MenuItem("VOLK/Setup Crouch Animator State")]
    static void Setup()
    {
        // Load animator controller
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
        if (controller == null)
        {
            Debug.LogError("[VOLK] PlayerAnimator.controller not found!");
            return;
        }

        var rootLayer = controller.layers[0];
        var stateMachine = rootLayer.stateMachine;

        // Check if Crouch state already exists
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == "Crouch_Idle")
            {
                Debug.Log("[VOLK] Crouch_Idle state already exists, skipping.");
                return;
            }
        }

        // Try to find a crouch animation clip, fall back to Idle
        var crouchClip = FindClip("Crouch") ?? FindClip("Crouch_Idle") ?? FindClip("Idle");
        if (crouchClip == null)
        {
            Debug.LogWarning("[VOLK] No crouch clip found. Using null clip placeholder.");
        }

        // Add Crouch_Idle state
        var crouchState = stateMachine.AddState("Crouch_Idle", new Vector3(400, 200, 0));
        if (crouchClip != null)
            crouchState.motion = crouchClip;
        crouchState.speed = 0.7f; // slightly slower

        // Find Idle state for transitions
        AnimatorState idleState = null;
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == "Idle")
            {
                idleState = state.state;
                break;
            }
        }

        if (idleState != null)
        {
            // Idle → Crouch when IsCrouching = true
            var toCrouch = idleState.AddTransition(crouchState);
            toCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
            toCrouch.hasExitTime = false;
            toCrouch.duration = 0.15f;

            // Crouch → Idle when IsCrouching = false
            var toIdle = crouchState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
        }

        // Add IsCrouching parameter if not exists
        bool hasParam = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "IsCrouching") { hasParam = true; break; }
        }
        if (!hasParam)
            controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[VOLK] Crouch_Idle state added to PlayerAnimator!");
    }

    static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {name}");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && clip.name.Contains(name))
                return clip;
        }
        return null;
    }
}
