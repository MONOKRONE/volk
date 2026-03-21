using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class FixAnimatorTransitions
{
    [MenuItem("Tools/Fix Animator Transitions")]
    public static void Fix()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
        if (controller == null) { Debug.LogError("PlayerAnimator.controller not found!"); return; }

        var sm = controller.layers[0].stateMachine;

        foreach (var cs in sm.states)
        {
            var state = cs.state;
            foreach (var t in state.transitions)
            {
                string from = state.name;
                string to = t.destinationState != null ? t.destinationState.name : "???";

                // Log current settings
                Debug.Log($"  {from} → {to}: hasExitTime={t.hasExitTime}, duration={t.duration:F2}, offset={t.offset:F2}");

                // Fix Walk → Idle: disable exit time, fast transition
                if (from == "Walk" && to == "Idle")
                {
                    t.hasExitTime = false;
                    t.duration = 0.1f;
                    Debug.Log($"    FIXED: hasExitTime=false, duration=0.1");
                }

                // Fix Run → Walk: disable exit time, fast transition
                if (from == "Run" && to == "Walk")
                {
                    t.hasExitTime = false;
                    t.duration = 0.1f;
                    Debug.Log($"    FIXED: hasExitTime=false, duration=0.1");
                }

                // Fix Idle → Walk: disable exit time, fast transition
                if (from == "Idle" && to == "Walk")
                {
                    t.hasExitTime = false;
                    t.duration = 0.1f;
                    Debug.Log($"    FIXED: hasExitTime=false, duration=0.1");
                }

                // Fix Walk → Run: disable exit time
                if (from == "Walk" && to == "Run")
                {
                    t.hasExitTime = false;
                    t.duration = 0.1f;
                    Debug.Log($"    FIXED: hasExitTime=false, duration=0.1");
                }
            }
        }

        // Also fix Any State transitions — they should have no exit time
        foreach (var t in sm.anyStateTransitions)
        {
            string to = t.destinationState != null ? t.destinationState.name : "???";
            Debug.Log($"  AnyState → {to}: hasExitTime={t.hasExitTime}, duration={t.duration:F2}");
            t.hasExitTime = false;
            t.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Animator transitions fixed!");
    }
}
