using System;
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

    // PLA-130: Stored delegates for proper unsubscription
    private Action punchTap, punchHold, punchDouble;
    private Action<FightButton> punchSlide;
    private Action kickTap, kickHold, kickDouble;
    private Action<FightButton> kickSlide;
    private Action parryTap, sk1Tap, sk2Tap;

    void Start()
    {
        if (punchButton != null)
        {
            punchTap = () => { fighter?.inputBuffer?.RecordInput("Punch"); fighter?.DoAttack(AttackType.Punch, AttackVariant.Normal); };
            punchHold = () => { fighter?.inputBuffer?.RecordInput("Punch"); fighter?.DoAttack(AttackType.Punch, AttackVariant.Heavy); };
            punchDouble = () => { fighter?.inputBuffer?.RecordInput("Punch"); if (fighter?.inputBuffer != null && fighter.inputBuffer.IsDoubleTap("Punch")) fighter?.UseSkill(1); };
            punchSlide = (target) => { if (target == kickButton) Debug.Log("[Touch] Punch→Kick combo (placeholder)"); };
            punchButton.OnTap += punchTap;
            punchButton.OnHold += punchHold;
            punchButton.OnDoubleTap += punchDouble;
            punchButton.OnSlideTo += punchSlide;
        }

        if (kickButton != null)
        {
            kickTap = () => { fighter?.inputBuffer?.RecordInput("Kick"); fighter?.DoAttack(AttackType.Kick, AttackVariant.Normal); };
            kickHold = () => { fighter?.inputBuffer?.RecordInput("Kick"); fighter?.DoAttack(AttackType.Kick, AttackVariant.Heavy); };
            kickDouble = () => { fighter?.inputBuffer?.RecordInput("Kick"); if (fighter?.inputBuffer != null && fighter.inputBuffer.IsDoubleTap("Kick")) fighter?.UseSkill(2); };
            kickSlide = (target) => { if (target == punchButton) Debug.Log("[Touch] Kick→Punch combo (placeholder)"); };
            kickButton.OnTap += kickTap;
            kickButton.OnHold += kickHold;
            kickButton.OnDoubleTap += kickDouble;
            kickButton.OnSlideTo += kickSlide;
        }

        if (parryButton != null)
        {
            parryTap = () => { fighter?.inputBuffer?.RecordInput("Block"); fighter?.AttemptParry(); };
            parryButton.OnTap += parryTap;
        }

        if (sk1Button != null)
        {
            sk1Tap = () => { fighter?.inputBuffer?.RecordInput("Skill1"); fighter?.UseSkill(1); };
            sk1Button.OnTap += sk1Tap;
        }

        if (sk2Button != null)
        {
            sk2Tap = () => { fighter?.inputBuffer?.RecordInput("Skill2"); fighter?.UseSkill(2); };
            sk2Button.OnTap += sk2Tap;
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

        // PLA-130: Unsubscribe all button events
        if (punchButton != null) { punchButton.OnTap -= punchTap; punchButton.OnHold -= punchHold; punchButton.OnDoubleTap -= punchDouble; punchButton.OnSlideTo -= punchSlide; }
        if (kickButton != null) { kickButton.OnTap -= kickTap; kickButton.OnHold -= kickHold; kickButton.OnDoubleTap -= kickDouble; kickButton.OnSlideTo -= kickSlide; }
        if (parryButton != null) { parryButton.OnTap -= parryTap; }
        if (sk1Button != null) { sk1Button.OnTap -= sk1Tap; }
        if (sk2Button != null) { sk2Button.OnTap -= sk2Tap; }
    }
}
