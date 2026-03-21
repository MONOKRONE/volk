using UnityEngine;

public class TouchCombatBridge : MonoBehaviour
{
    [Header("References")]
    public Fighter fighter;
    public TouchInputHandler joystick;

    [Header("Buttons")]
    public FightButton punchButton;
    public FightButton kickButton;
    public FightButton parryButton;
    public FightButton sk1Button;
    public FightButton sk2Button;

    [Header("Settings")]
    public bool useTouchInput = false;

    void Start()
    {
        if (punchButton != null)
        {
            punchButton.OnTap += () => fighter?.DoAttack(AttackType.Punch, AttackVariant.Normal);
            punchButton.OnHold += () => fighter?.DoAttack(AttackType.Punch, AttackVariant.Heavy);
            punchButton.OnDoubleTap += () => fighter?.DoAttack(AttackType.Punch, AttackVariant.Special);
            punchButton.OnSlideTo += (target) =>
            {
                if (target == kickButton)
                    Debug.Log("[Touch] Punch→Kick combo (placeholder)");
            };
        }

        if (kickButton != null)
        {
            kickButton.OnTap += () => fighter?.DoAttack(AttackType.Kick, AttackVariant.Normal);
            kickButton.OnHold += () => fighter?.DoAttack(AttackType.Kick, AttackVariant.Heavy);
            kickButton.OnDoubleTap += () => fighter?.DoAttack(AttackType.Kick, AttackVariant.Special);
            kickButton.OnSlideTo += (target) =>
            {
                if (target == punchButton)
                    Debug.Log("[Touch] Kick→Punch combo (placeholder)");
            };
        }

        if (parryButton != null)
            parryButton.OnTap += () => fighter?.AttemptParry();

        if (sk1Button != null)
            sk1Button.OnTap += () => fighter?.UseSkill(1);

        if (sk2Button != null)
            sk2Button.OnTap += () => fighter?.UseSkill(2);
    }

    void Update()
    {
        if (!useTouchInput || fighter == null || joystick == null) return;

        // Feed joystick input to fighter's touch input
        fighter.touchMoveInput = joystick.MoveInput;
        fighter.useTouchMovement = true;

        if (joystick.JumpTriggered)
            Debug.Log("[Touch] Flick Up - Jump (placeholder)");
        if (joystick.CrouchTriggered)
            Debug.Log("[Touch] Flick Down - Crouch (placeholder)");
    }

    void OnDisable()
    {
        if (fighter != null)
        {
            fighter.touchMoveInput = Vector2.zero;
            fighter.useTouchMovement = false;
        }
    }
}
