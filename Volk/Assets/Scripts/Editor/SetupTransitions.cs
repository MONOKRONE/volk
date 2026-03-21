using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupTransitions
{
    [MenuItem("Tools/Setup Animator Transitions")]
    public static void Setup()
    {
        string controllerPath = "Assets/Animations/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("PlayerAnimator.controller not found!");
            return;
        }

        // Add parameters
        AddTrigger(controller, "HookPunch");
        AddTrigger(controller, "MMAKick");
        AddTrigger(controller, "BodyBlock");
        AddTrigger(controller, "TakingPunch");
        AddTrigger(controller, "ReceivingUppercut");
        AddTrigger(controller, "Death");
        AddTrigger(controller, "Jump");
        AddBool(controller, "IsWalking");
        AddBool(controller, "IsRunning");

        var sm = controller.layers[0].stateMachine;

        // Find states
        AnimatorState idle = null, walk = null, run = null;
        AnimatorState hookPunch = null, mmaKick = null, bodyBlock = null;
        AnimatorState takingPunch = null, receivingUppercut = null;
        AnimatorState death = null, jump = null;

        foreach (var cs in sm.states)
        {
            switch (cs.state.name)
            {
                case "Idle": idle = cs.state; break;
                case "Walk": walk = cs.state; break;
                case "Run": run = cs.state; break;
                case "HookPunch": hookPunch = cs.state; break;
                case "MMAKick": mmaKick = cs.state; break;
                case "BodyBlock": bodyBlock = cs.state; break;
                case "TakingPunch": takingPunch = cs.state; break;
                case "ReceivingUppercut": receivingUppercut = cs.state; break;
                case "Death": death = cs.state; break;
                case "Jump": jump = cs.state; break;
            }
        }

        // Clear existing transitions
        sm.anyStateTransitions = new AnimatorStateTransition[0];
        foreach (var cs in sm.states)
        {
            cs.state.transitions = new AnimatorStateTransition[0];
        }

        // Any State → combat/action states (trigger, no exit time)
        AddAnyStateTrigger(sm, hookPunch, "HookPunch");
        AddAnyStateTrigger(sm, mmaKick, "MMAKick");
        AddAnyStateTrigger(sm, bodyBlock, "BodyBlock");
        AddAnyStateTrigger(sm, takingPunch, "TakingPunch");
        AddAnyStateTrigger(sm, receivingUppercut, "ReceivingUppercut");
        AddAnyStateTrigger(sm, jump, "Jump");
        AddAnyStateTrigger(sm, death, "Death");

        // Idle <-> Walk (bool IsWalking)
        AddBoolTransition(idle, walk, "IsWalking", true, false);
        AddBoolTransition(walk, idle, "IsWalking", false, false);

        // Walk <-> Run (bool IsRunning)
        AddBoolTransition(walk, run, "IsRunning", true, false);
        AddBoolTransition(run, walk, "IsRunning", false, false);

        // Action states → Idle (has exit time)
        AddExitTimeTransition(hookPunch, idle);
        AddExitTimeTransition(mmaKick, idle);
        AddExitTimeTransition(bodyBlock, idle);
        AddExitTimeTransition(takingPunch, idle);
        AddExitTimeTransition(receivingUppercut, idle);
        AddExitTimeTransition(jump, idle);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Animator transitions setup complete!");
    }

    static void AddTrigger(AnimatorController c, string name)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    static void AddBool(AnimatorController c, string name)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, AnimatorControllerParameterType.Bool);
    }

    static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState target, string triggerName)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false;
        t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
    }

    static void AddBoolTransition(AnimatorState from, AnimatorState to, string boolName, bool value, bool hasExitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExitTime;
        t.duration = 0.15f;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, boolName);
    }

    static void AddExitTimeTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.15f;
    }
}
