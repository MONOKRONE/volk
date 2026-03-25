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
            punchButton.OnTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Punch");
                fighter?.DoAttack(AttackType.Punch, AttackVariant.Normal);
            };
            punchButton.OnHold += () =>
            {
                fighter?.inputBuffer?.RecordInput("Punch");
                fighter?.DoAttack(AttackType.Punch, AttackVariant.Heavy);
            };
            punchButton.OnDoubleTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Punch");
                if (fighter?.inputBuffer != null && fighter.inputBuffer.IsDoubleTap("Punch"))
                    fighter?.UseSkill(1);
            };
            punchButton.OnSlideTo += (target) =>
            {
                if (target == kickButton)
                    Debug.Log("[Touch] Punch→Kick combo (placeholder)");
            };
        }

        if (kickButton != null)
        {
            kickButton.OnTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Kick");
                fighter?.DoAttack(AttackType.Kick, AttackVariant.Normal);
            };
            kickButton.OnHold += () =>
            {
                fighter?.inputBuffer?.RecordInput("Kick");
                fighter?.DoAttack(AttackType.Kick, AttackVariant.Heavy);
            };
            kickButton.OnDoubleTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Kick");
                if (fighter?.inputBuffer != null && fighter.inputBuffer.IsDoubleTap("Kick"))
                    fighter?.UseSkill(2);
            };
            kickButton.OnSlideTo += (target) =>
            {
                if (target == punchButton)
                    Debug.Log("[Touch] Kick→Punch combo (placeholder)");
            };
        }

        if (parryButton != null)
        {
            parryButton.OnTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Block");
                fighter?.AttemptParry();
            };
        }

        if (sk1Button != null)
        {
            sk1Button.OnTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Skill1");
                fighter?.UseSkill(1);
            };
        }

        if (sk2Button != null)
        {
            sk2Button.OnTap += () =>
            {
                fighter?.inputBuffer?.RecordInput("Skill2");
                fighter?.UseSkill(2);
            };
        }
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
