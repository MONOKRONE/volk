using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupJumpCrouchAnim
{
    [MenuItem("Tools/Setup Jump Crouch Animator")]
    public static void Setup()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
        if (controller == null) { Debug.LogError("PlayerAnimator.controller not found!"); return; }

        // Add parameters
        AddBool(controller, "IsJumping");
        AddBool(controller, "IsCrouching");

        var sm = controller.layers[0].stateMachine;

        // Find states
        AnimatorState idle = null, jump = null, block = null;
        foreach (var cs in sm.states)
        {
            if (cs.state.name == "Idle") idle = cs.state;
            if (cs.state.name == "Jump") jump = cs.state;
            if (cs.state.name == "BodyBlock") block = cs.state;
        }

        if (idle == null) { Debug.LogError("Idle state not found!"); return; }

        // Create Crouch state using BodyBlock animation (stand-in)
        AnimatorState crouch = null;
        foreach (var cs in sm.states)
            if (cs.state.name == "Crouch") crouch = cs.state;

        if (crouch == null && block != null)
        {
            crouch = sm.AddState("Crouch");
            crouch.motion = block.motion;
            Debug.Log("Created Crouch state (using BodyBlock animation)");
        }

        // Any State → Jump (IsJumping=true, no exit time)
        if (jump != null)
        {
            // Check if transition already exists
            bool exists = false;
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == jump) { exists = true; break; }

            if (!exists)
            {
                var t = sm.AddAnyStateTransition(jump);
                t.hasExitTime = false;
                t.duration = 0.1f;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
                Debug.Log("Added AnyState → Jump transition");
            }

            // Jump → Idle (IsJumping=false)
            bool jumpToIdle = false;
            foreach (var t in jump.transitions)
                if (t.destinationState == idle) { jumpToIdle = true; break; }

            if (!jumpToIdle)
            {
                var t = jump.AddTransition(idle);
                t.hasExitTime = false;
                t.duration = 0.15f;
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
                Debug.Log("Added Jump → Idle transition");
            }
        }

        // Any State → Crouch (IsCrouching=true, no exit time)
        if (crouch != null)
        {
            bool exists = false;
            foreach (var t in sm.anyStateTransitions)
                if (t.destinationState == crouch) { exists = true; break; }

            if (!exists)
            {
                var t = sm.AddAnyStateTransition(crouch);
                t.hasExitTime = false;
                t.duration = 0.1f;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");
                Debug.Log("Added AnyState → Crouch transition");
            }

            // Crouch → Idle (IsCrouching=false)
            bool crouchToIdle = false;
            foreach (var t in crouch.transitions)
                if (t.destinationState == idle) { crouchToIdle = true; break; }

            if (!crouchToIdle)
            {
                var t = crouch.AddTransition(idle);
                t.hasExitTime = false;
                t.duration = 0.15f;
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
                Debug.Log("Added Crouch → Idle transition");
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Jump/Crouch animator setup complete!");
    }

    static void AddBool(AnimatorController c, string name)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, AnimatorControllerParameterType.Bool);
        Debug.Log($"Added parameter: {name}");
    }
}
