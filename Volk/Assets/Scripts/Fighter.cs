using UnityEngine;
using System.Collections;
using Volk.Core;

public enum AttackType { Punch, Kick }
public enum AttackVariant { Normal, Heavy, Special }
public enum AIDifficulty { Easy, Normal, Hard }
public enum AIState { Idle, Approach, Combat, Retreat, Stunned }

public class Fighter : MonoBehaviour
{
    [Header("Character Data (optional - overrides stats below)")]
    public CharacterData characterData;

    [Header("Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float attackDamage = 15f;
    public float attackRange = 1.2f;
    public string enemyTag = "Enemy";

    // Combat stats from CharacterData (1-10 scale)
    // power: attacker multiplier, defense: damage reduction
    [HideInInspector] public float power = 5f;
    [HideInInspector] public float defense = 5f;

    [Header("Attack Points - assign hand/foot transforms")]
    public Transform rightHandPoint;
    public Transform rightFootPoint;

    // QUANTUM: Movement fields must migrate to FPVector2/FPVector3 in Quantum simulation.
    // CharacterController.Move() calls replaced by Quantum KCC (Kinematic Character Controller).
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 8.5f;
    public float rotSpeed = 1200f;
    public float combatRotSpeed = 2400f;
    private Vector3 currentVelocity; // QUANTUM: FPVector3, synced via Quantum state
    public float jumpHeight = 1.8f;

    [Header("Lock-On")]
    public bool lockOnEnabled = true;

    [Header("AI - leave null for player")]
    public Transform aiTarget;
    public bool isAI = false;
    public AIDifficulty difficulty = AIDifficulty.Normal;
    public AIState currentAIState = AIState.Idle;

    // AI internals
    private float aiAttackCooldown;
    private float aiRetreatTimer;
    private float aiStunnedTimer;
    private float aiDecisionTimer;
    private float reactionTime;
    private float parryChance;
    private float retreatHPThreshold;
    private float aiAttackRange = 1.8f;
    private float aiApproachStop = 0.9f;
    private Fighter cachedTargetFighter;

    // AI state commitment
    private float stateMinDuration;
    private float stateTimer;
    private Coroutine aiTelegraphCoroutine;

    // AI approach zigzag
    private float zigzagAngle;
    private float zigzagTimer;

    // Touch input
    [HideInInspector] public Vector2 touchMoveInput;
    [HideInInspector] public bool useTouchMovement;
    private TouchInputHandler touchHandler;

    // Input buffer
    [HideInInspector] public Volk.InputBuffer inputBuffer;

    // Parry
    private bool isParrying;
    private float parryTimer;

    // Jump & Crouch
    [HideInInspector] public bool isCrouching;
    private float normalHeight;
    private float crouchHeight;

    // Lean/tilt
    private float leanAmount = 0.8f;
    private Transform meshTransform;

    // QUANTUM: Knockback must be deterministic. Use FP math for force/velocity.
    // knockbackVelocity → FPVector3 in Quantum frame data.
    public float knockbackForce = 2f;
    public float knockbackDuration = 0.2f;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    // Combo
    [HideInInspector] public bool comboWindowOpen;
    public float comboWindowDuration = 0.55f;
    private float comboWindowTimer;

    // Skill cooldowns
    private float skill1CooldownTimer;
    private float skill2CooldownTimer;

    // EX Meter (0-100)
    [HideInInspector] public float exMeter;
    public const float EX_METER_MAX = 100f;
    private const float EX_GAIN_ON_HIT_DEALT = 5f;
    private const float EX_GAIN_ON_HIT_TAKEN = 10f;
    public float ExMeterRatio => exMeter / EX_METER_MAX;

    // Perfect Block
    private bool isPerfectBlockWindow;
    private float perfectBlockTimer;
    private const float PERFECT_BLOCK_WINDOW = 0.2f; // 200ms

    // Super Armor (active during skill animations)
    private bool isPlayingSkillAnim;
    public float Skill1CooldownRatio => characterData?.skill1 != null && characterData.skill1.cooldown > 0
        ? Mathf.Clamp01(skill1CooldownTimer / characterData.skill1.cooldown) : 0f;
    public float Skill2CooldownRatio => characterData?.skill2 != null && characterData.skill2.cooldown > 0
        ? Mathf.Clamp01(skill2CooldownTimer / characterData.skill2.cooldown) : 0f;

    // Private
    private CharacterController cc;
    private Animator anim;
    public bool isAttacking { get; private set; }
    public event System.Action<Volk.Core.PlayerAction> OnActionPerformed;
    public event System.Action OnAttackWhiff;
    private Volk.Core.PlayerBehaviorTracker behaviorTracker;
    public bool isDead { get; private set; }
    private float yVelocity;
    private float postAttackCooldown;

    // PLA-125: Anti-spam — damage only via AnimationEvent
    private bool isAttackActive;
    private bool hasDealtDamage;

    // PLA-125: Animation phases (Startup / Active / Recovery)
    public enum AttackPhase { None, Startup, Active, Recovery }
    [HideInInspector] public AttackPhase attackPhase = AttackPhase.None;

    // PLA-125: Tap + Hold combo chain
    private int comboStep; // 0=none, 1=Light1, 2=Light2, 3=Light3
    private float lastComboInputTime;
    private const float COMBO_TIMEOUT = 1.5f;
    private float attackHoldStartTime;
    private bool attackButtonHeld;
    static int hLight1 = Animator.StringToHash("Light1");
    static int hLight2 = Animator.StringToHash("Light2");
    static int hLight3 = Animator.StringToHash("Light3");
    static int hHeavyFinisher = Animator.StringToHash("HeavyFinisher");

    // PLA-125: Hit Stack / Mini-Rage
    [HideInInspector] public int hitStack;
    private float hitStackTimer;
    private const int HIT_STACK_THRESHOLD = 8;
    private const float HIT_STACK_MULTIPLIER = 1.3f;
    private const float HIT_STACK_DURATION = 4f;
    public bool IsRaging => hitStack >= HIT_STACK_THRESHOLD && hitStackTimer > 0f;
    public float HitStackRatio => (float)hitStack / HIT_STACK_THRESHOLD;

    // Animator hashes
    static int hWalk = Animator.StringToHash("IsWalking");
    static int hRun = Animator.StringToHash("IsRunning");
    static int hWalkSpeed = Animator.StringToHash("WalkSpeed");
    static int hPunch = Animator.StringToHash("HookPunch");
    static int hKick = Animator.StringToHash("MMAKick");
    static int hBlock = Animator.StringToHash("BodyBlock");
    static int hHit1 = Animator.StringToHash("TakingPunch");
    static int hHit2 = Animator.StringToHash("ReceivingUppercut");
    static int hDeath = Animator.StringToHash("Death");
    static int hJump = Animator.StringToHash("IsJumping");
    static int hCrouch = Animator.StringToHash("IsCrouching");
    static int hStaggerLight = Animator.StringToHash("StaggerLight");
    static int hStaggerHeavy = Animator.StringToHash("StaggerHeavy");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;

        // Apply CharacterData SO if assigned
        if (characterData != null)
            ApplyCharacterData(characterData);

        // NG+ HP modifier
        float ngHPMult = Volk.Core.NewGamePlusManager.Instance?.GetHPMultiplier() ?? 1f;
        if (ngHPMult < 1f)
            maxHP *= ngHPMult;

        currentHP = maxHP;
        normalHeight = cc.height;
        crouchHeight = cc.height * 0.55f;

        if (Application.isMobilePlatform || useTouchMovement)
            touchHandler = FindFirstObjectByType<TouchInputHandler>();

        inputBuffer = GetComponent<Volk.InputBuffer>();
        if (inputBuffer == null)
            inputBuffer = gameObject.AddComponent<Volk.InputBuffer>();

        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null) meshTransform = smr.transform;
        else meshTransform = anim != null ? anim.transform : transform;

        if (!isAI)
        {
            behaviorTracker = Volk.Core.PlayerBehaviorTracker.Instance;
            if (behaviorTracker != null)
            {
                string myChar = characterData != null ? characterData.characterName : "Unknown";
                string enemyChar = aiTarget?.GetComponent<Fighter>()?.characterData?.characterName ?? "Unknown";
                behaviorTracker.SetMatchup(myChar, enemyChar);
            }
            OnActionPerformed += OnPlayerAction;
        }

        if (isAI) InitAIDifficulty();

        // Auto-find target for both player and AI
        if (aiTarget == null)
            aiTarget = GameObject.FindWithTag(enemyTag)?.transform;
    }

    public void ApplyCharacterData(CharacterData data)
    {
        characterData = data;
        maxHP = data.maxHP;
        attackDamage = data.attackDamage;
        attackRange = data.attackRange;
        walkSpeed = data.walkSpeed * data.walkSpeedMultiplier;
        runSpeed = data.runSpeed * data.runSpeedMultiplier;
        knockbackForce = data.knockbackForce * data.knockbackMultiplier;
        rotSpeed = data.rotationSpeed * data.rotationSpeedMultiplier;
        combatRotSpeed = data.combatRotationSpeed * data.rotationSpeedMultiplier;
        power = data.power;
        defense = data.defense;

        if (data.animController != null)
            anim.runtimeAnimatorController = data.animController;

        // Combat feel parameters
        if (anim != null)
            anim.speed = data.animationSpeedMult;
        knockbackForce = data.attackKnockbackForce;

        // Apply palette swap material
        if (data.characterMaterial != null)
        {
            var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var r in renderers)
                r.material = data.characterMaterial;
        }
    }

    public void InitAIDifficulty()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                reactionTime = 1.2f; parryChance = 0.08f; retreatHPThreshold = 0.4f; break;
            case AIDifficulty.Normal:
                reactionTime = 0.7f; parryChance = 0.28f; retreatHPThreshold = 0.25f; break;
            case AIDifficulty.Hard:
                reactionTime = 0.22f; parryChance = 0.55f; retreatHPThreshold = 0.1f; break;
        }
        aiAttackCooldown = reactionTime;
    }

    void Update()
    {
        if (isDead) return;

        // Skip update while ragdoll is active
        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null && ragdoll.IsActive) return;

        // Skill cooldown tick
        if (skill1CooldownTimer > 0f) skill1CooldownTimer -= Time.deltaTime;
        if (skill2CooldownTimer > 0f) skill2CooldownTimer -= Time.deltaTime;

        // PLA-125: Hit stack timer decay
        if (hitStackTimer > 0f)
        {
            hitStackTimer -= Time.deltaTime;
            if (hitStackTimer <= 0f) { hitStack = 0; hitStackTimer = 0f; }
        }

        // PLA-125: Combo timeout — reset chain if idle too long
        if (comboStep > 0 && Time.time - lastComboInputTime > COMBO_TIMEOUT)
            comboStep = 0;

        if (comboWindowOpen)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
                comboWindowOpen = false;
        }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
            return;
        }

        if (postAttackCooldown > 0f)
        {
            postAttackCooldown -= Time.deltaTime;
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
            return;
        }

        anim.applyRootMotion = false;

        // QUANTUM: Gravity must use FP.Gravity and deterministic deltaTime.
        // Replace Physics.gravity with Quantum fixed-point gravity constant.
        if (cc.isGrounded && yVelocity < 0)
            yVelocity = -2f;
        else
            yVelocity += Physics.gravity.y * Time.deltaTime;

        if (isAI) UpdateAI();
        else UpdatePlayer();
    }

    void UpdatePlayer()
    {
        if (knockbackTimer > 0f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 15f * Time.deltaTime);
            cc.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            if (knockbackVelocity.magnitude < 0.1f)
                knockbackVelocity = Vector3.zero;
            return;
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        bool mobile = touchHandler != null && (Application.isMobilePlatform || useTouchMovement);
        float h = mobile ? touchHandler.MoveInput.x : Input.GetAxisRaw("Horizontal");
        float v = mobile ? touchHandler.MoveInput.y : Input.GetAxisRaw("Vertical");
        // Forward/backward along facing direction, strafe perpendicular (world-horizontal)
        Vector3 forwardDir = transform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();
        Vector3 strafeDir = Vector3.Cross(Vector3.up, forwardDir);
        Vector3 rawDir = forwardDir * v + strafeDir * h;
        rawDir.y = 0;
        float inputMag = rawDir.magnitude;
        Vector3 dir = inputMag > 0.15f ? rawDir.normalized : Vector3.zero;

        // Jump
        if (touchHandler != null && touchHandler.JumpTriggered && cc.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        anim.SetBool(hJump, !cc.isGrounded);

        // Crouch toggle (touch + keyboard)
        bool crouchInput = (touchHandler != null && touchHandler.CrouchTriggered) || Input.GetKeyDown(KeyCode.C);
        if (crouchInput)
        {
            isCrouching = !isCrouching;
            cc.height = isCrouching ? crouchHeight : normalHeight;
            cc.center = new Vector3(0, cc.height / 2f, 0);
        }
        anim.SetBool(hCrouch, isCrouching);

        // PLA-125: Swipe gestures — Skill1 on swipe up, Block on swipe down
        if (touchHandler != null)
        {
            if (touchHandler.SwipeUpTriggered) UseSkill(1);
            if (touchHandler.SwipeDownTriggered) inputBuffer.RecordInput("Block");
        }

        // Reduce walk speed while crouching
        float currentSpeed = isCrouching ? walkSpeed * 0.5f : walkSpeed;

        // Buffer attack inputs for responsiveness (PLA-89/PLA-125: 20 frame buffer)
        if (Input.GetKeyDown(KeyCode.J)) { inputBuffer.RecordInput("Punch"); attackHoldStartTime = Time.time; attackButtonHeld = true; }
        if (Input.GetKeyUp(KeyCode.J)) attackButtonHeld = false;
        if (Input.GetKeyDown(KeyCode.K)) inputBuffer.RecordInput("Kick");
        if (Input.GetKeyDown(KeyCode.L)) inputBuffer.RecordInput("Block");

        // PLA-125: Recovery cancel — during Recovery phase, allow dodge/combo
        if (attackPhase == AttackPhase.Recovery)
        {
            if (Input.GetKeyDown(KeyCode.Space) || (touchHandler != null && touchHandler.CrouchTriggered))
            {
                // Dodge cancel out of recovery
                StopAllCoroutines();
                isAttacking = false;
                isAttackActive = false;
                attackPhase = AttackPhase.None;
                anim.speed = characterData != null ? characterData.animationSpeedMult : 1f;
                // Let movement handle dodge
            }
            // Allow combo input during recovery
            else if (inputBuffer.ConsumeInput("Punch") || Input.GetKeyDown(KeyCode.J))
            {
                StopAllCoroutines();
                anim.speed = 1f;
                attackPhase = AttackPhase.None;
                ExecuteComboAttack(false);
                return;
            }
        }

        // PLA-125: Tap + Hold combo — check on key release or buffer
        bool canAttack = !isAttacking || comboWindowOpen || attackPhase == AttackPhase.Recovery;
        if (canAttack)
        {
            // Hold detection: if J held > 300ms, trigger heavy finisher
            if (attackButtonHeld && Time.time - attackHoldStartTime > 0.3f && !isAttackActive)
            {
                attackButtonHeld = false;
                comboWindowOpen = false;
                if (isAttacking) StopAllCoroutines();
                anim.speed = 1f;
                StartCoroutine(DoComboAttack(hHeavyFinisher, rightHandPoint, true));
                return;
            }

            // Tap: light combo chain
            if (inputBuffer.ConsumeInput("Punch"))
            {
                if (isAttacking && comboWindowOpen) StopAllCoroutines();
                anim.speed = 1f;
                ExecuteComboAttack(false);
                return;
            }
            if (inputBuffer.ConsumeInput("Kick"))
            {
                comboWindowOpen = false;
                if (isAttacking) StopAllCoroutines();
                anim.speed = 1f;
                StartCoroutine(DoAttack(hKick, rightFootPoint));
                return;
            }
            if (!isAttacking && inputBuffer.ConsumeInput("Block"))
            {
                StartCoroutine(DoBlock());
                return;
            }
        }

        if (isAttacking)
        {
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
            return;
        }

        // Deceleration curve: smooth velocity towards target
        // accel ~0.05s, decel ~0.1s (PLA-88)
        Vector3 targetVelocity = dir * currentSpeed;
        float accelRate = inputMag > 0.15f ? 20f : currentSpeed / 0.1f;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.deltaTime);

        Vector3 move = currentVelocity;
        move.y = yVelocity;
        cc.Move(move * Time.deltaTime);

        if (inputMag > 0.15f)
        {
            bool movingBackward = v < -0.15f;
            anim.SetFloat(hWalkSpeed, movingBackward ? -1f : 1f);
            anim.SetBool(hWalk, true);
            anim.SetBool(hRun, false);
        }
        else
        {
            anim.SetFloat(hWalkSpeed, 1f);
            anim.SetBool(hWalk, currentVelocity.magnitude > 0.1f);
            anim.SetBool(hRun, false);
        }

        // Auto face enemy when locked on - using RotateTowards
        // Use combatRotSpeed (2400) when close to enemy, rotSpeed (1200) otherwise
        float activeRotSpeed = rotSpeed;
        if (aiTarget != null)
        {
            float distToTarget = Vector3.Distance(transform.position, aiTarget.position);
            activeRotSpeed = distToTarget < attackRange * 2f ? combatRotSpeed : rotSpeed;
        }

        if (lockOnEnabled && aiTarget != null && !isAttacking)
        {
            Vector3 lookDir = (aiTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(lookDir), activeRotSpeed * Time.deltaTime);
        }
        else if (!lockOnEnabled && !isAttacking)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (camForward.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(camForward), rotSpeed * Time.deltaTime);
            }
        }

        // Lean/tilt based on lateral velocity — applied to mesh, not root
        if (meshTransform != null && meshTransform != transform)
        {
            float lateralSpeed = Vector3.Dot(currentVelocity, transform.right);
            float targetLean = -lateralSpeed * leanAmount;
            targetLean = Mathf.Clamp(targetLean, -8f, 8f);
            meshTransform.localRotation = Quaternion.Euler(0f, 0f, targetLean);
        }
    }

    float GetReactionDelay()
    {
        float baseMs = 150f;
        float variance = Random.Range(-30f, 80f);
        float diffMod = difficulty == AIDifficulty.Hard ? -60f : difficulty == AIDifficulty.Easy ? 60f : 0f;
        return (baseMs + variance + diffMod) / 1000f;
    }

    void SetAIState(AIState newState)
    {
        if (newState == currentAIState) return;
        // State commitment: don't switch if minimum duration hasn't elapsed (except Stunned)
        if (stateTimer < stateMinDuration && newState != AIState.Stunned) return;
        // Validate transition — only allowed edges in the FSM graph
        if (!IsValidTransition(currentAIState, newState)) return;

        // Cancel telegraph coroutine when leaving Combat
        if (currentAIState == AIState.Combat && aiTelegraphCoroutine != null)
        {
            StopCoroutine(aiTelegraphCoroutine);
            aiTelegraphCoroutine = null;
        }

        currentAIState = newState;
        stateTimer = 0f;
        stateMinDuration = Random.Range(2.5f, 4.0f);

        // Reset zigzag on state change
        zigzagAngle = 0f;
        zigzagTimer = Random.Range(0.3f, 0.8f);
    }

    bool IsValidTransition(AIState from, AIState to)
    {
        // Stunned can be entered from any state (knockback/hit reaction)
        if (to == AIState.Stunned) return true;
        switch (from)
        {
            case AIState.Idle:     return to == AIState.Approach;
            case AIState.Approach: return to == AIState.Combat || to == AIState.Retreat;
            case AIState.Combat:   return to == AIState.Approach || to == AIState.Retreat;
            case AIState.Retreat:  return to == AIState.Approach;
            case AIState.Stunned:  return to == AIState.Approach;
            default: return false;
        }
    }

    void UpdateAI()
    {
        if (aiTarget == null)
        {
            // Cached lookup — only retry once per second instead of every frame
            if (Time.frameCount % 60 == 0)
                aiTarget = GameObject.FindWithTag(enemyTag)?.transform;
            if (aiTarget == null) return;
        }

        stateTimer += Time.deltaTime;

        // Knockback override
        if (knockbackTimer > 0f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 15f * Time.deltaTime);
            cc.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            if (knockbackVelocity.magnitude < 0.1f)
                knockbackVelocity = Vector3.zero;
            SetAIState(AIState.Stunned);
            stateMinDuration = 0f; // Stunned can always be exited
            aiStunnedTimer = 0.4f;
            return;
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        float distToTarget = Vector3.Distance(transform.position, aiTarget.position);
        float hpRatio = currentHP / maxHP;

        switch (currentAIState)
        {
            case AIState.Idle:
                anim.SetBool(hWalk, false);
                anim.SetBool(hRun, false);
                cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
                aiDecisionTimer -= Time.deltaTime;
                if (aiDecisionTimer <= 0f)
                    SetAIState(AIState.Approach);
                break;

            case AIState.Approach:
                if (hpRatio < retreatHPThreshold)
                { SetAIState(AIState.Retreat); aiRetreatTimer = 1.5f; break; }

                if (distToTarget <= aiAttackRange)
                { SetAIState(AIState.Combat); aiAttackCooldown = GetReactionDelay(); break; }

                if (!isAttacking)
                {
                    Vector3 approachDir = (aiTarget.position - transform.position).normalized;
                    approachDir.y = 0;

                    // Zigzag: periodically offset approach angle
                    zigzagTimer -= Time.deltaTime;
                    if (zigzagTimer <= 0f)
                    {
                        zigzagAngle = Random.Range(-20f, 20f);
                        zigzagTimer = Random.Range(0.3f, 0.8f);
                    }
                    approachDir = Quaternion.Euler(0f, zigzagAngle, 0f) * approachDir;

                    if (distToTarget > aiApproachStop)
                    {
                        Vector3 move = approachDir * walkSpeed;
                        move.y = yVelocity;
                        cc.Move(move * Time.deltaTime);
                    }
                    else
                    {
                        cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
                    }
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(approachDir), rotSpeed * Time.deltaTime);
                    anim.SetBool(hWalk, distToTarget > aiApproachStop);
                    anim.SetBool(hRun, false);
                }
                break;

            case AIState.Combat:
                // Face target
                Vector3 faceDir = (aiTarget.position - transform.position).normalized;
                faceDir.y = 0;
                if (faceDir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(faceDir), rotSpeed * Time.deltaTime);

                if (distToTarget > aiAttackRange + 0.5f)
                { SetAIState(AIState.Approach); break; }

                if (hpRatio < retreatHPThreshold)
                { SetAIState(AIState.Retreat); aiRetreatTimer = 1.5f; break; }

                // Try parry if target is attacking
                if (cachedTargetFighter == null) cachedTargetFighter = aiTarget.GetComponent<Fighter>();
                Fighter targetFighter = cachedTargetFighter;
                if (targetFighter != null && targetFighter.isAttacking && !isAttacking)
                {
                    if (Random.value < parryChance * Time.deltaTime * 10f)
                        AttemptParry();
                }

                // Attack on cooldown with telegraph
                aiAttackCooldown -= Time.deltaTime;
                if (aiAttackCooldown <= 0f && !isAttacking && aiTelegraphCoroutine == null)
                {
                    aiTelegraphCoroutine = StartCoroutine(AITelegraphAttack());
                    aiAttackCooldown = GetReactionDelay() + Random.Range(0.1f, 0.4f);
                }

                anim.SetBool(hWalk, false);
                anim.SetBool(hRun, false);
                cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
                break;

            case AIState.Retreat:
                aiRetreatTimer -= Time.deltaTime;
                if (aiRetreatTimer <= 0f)
                { SetAIState(AIState.Approach); break; }

                Vector3 retreatDir = (transform.position - aiTarget.position).normalized;
                retreatDir.y = 0;
                Vector3 retreatMove = retreatDir * walkSpeed;
                retreatMove.y = yVelocity;
                cc.Move(retreatMove * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(-retreatDir), rotSpeed * Time.deltaTime);
                anim.SetBool(hWalk, true);
                anim.SetBool(hRun, false);
                break;

            case AIState.Stunned:
                aiStunnedTimer -= Time.deltaTime;
                anim.SetBool(hWalk, false);
                anim.SetBool(hRun, false);
                cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
                if (aiStunnedTimer <= 0f)
                    SetAIState(AIState.Approach);
                break;
        }
    }

    IEnumerator AITelegraphAttack()
    {
        // Subtle telegraph: brief rotation twitch before attacking
        float telegraphDuration = Random.Range(0.2f, 0.4f);
        Quaternion originalRot = transform.rotation;
        float twitchAngle = Random.Range(-5f, 5f);
        transform.rotation *= Quaternion.Euler(0f, twitchAngle, 0f);
        yield return new WaitForSeconds(telegraphDuration);
        transform.rotation = originalRot;

        // Now attack
        bool usePunch = Random.value < 0.6f;
        StartCoroutine(DoAttack(usePunch ? hPunch : hKick,
            usePunch ? rightHandPoint : rightFootPoint));
        aiTelegraphCoroutine = null;
    }

    // PLA-125: Execute the correct combo step
    void ExecuteComboAttack(bool isHeavy)
    {
        comboWindowOpen = false;
        lastComboInputTime = Time.time;

        if (isHeavy)
        {
            comboStep = 0;
            StartCoroutine(DoComboAttack(hHeavyFinisher, rightHandPoint, true));
        }
        else
        {
            comboStep++;
            if (comboStep > 3) comboStep = 1;
            int animHash = comboStep switch
            {
                1 => hLight1,
                2 => hLight2,
                3 => hLight3,
                _ => hPunch
            };
            StartCoroutine(DoComboAttack(animHash, rightHandPoint, false));
        }
    }

    // PLA-125: Phase-based combo attack with anti-spam
    IEnumerator DoComboAttack(int animHash, Transform hitPoint, bool isHeavy)
    {
        isAttacking = true;
        isAttackActive = true;
        hasDealtDamage = false;
        anim.applyRootMotion = false;
        anim.SetTrigger(animHash);

        float heavyMult = isHeavy ? 1.5f : 1f;
        float startupTime = isHeavy ? 0.25f : 0.15f;
        float activeTime = isHeavy ? 0.45f : 0.35f;
        float recoveryTime = isHeavy ? 0.45f : 0.3f;

        if (!isAI && behaviorTracker != null)
        {
            var action = isHeavy ? Volk.Core.PlayerAction.HeavyPunch : Volk.Core.PlayerAction.Punch;
            behaviorTracker.Record(GetCurrentSituation(), action);
        }

        AudioManager.Instance?.PlayWhoosh();

        // === STARTUP PHASE ===
        attackPhase = AttackPhase.Startup;
        yield return new WaitForSeconds(startupTime);

        // === ACTIVE PHASE — damage window ===
        attackPhase = AttackPhase.Active;
        float hitTimer = 0f;

        while (hitTimer < activeTime)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hasDealtDamage && hitPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.5f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        // PLA-125: Anti-spam — mark damage dealt immediately
                        hasDealtDamage = true;
                        isAttackActive = false;

                        // PLA-125: Hit stack / mini-rage multiplier
                        float rageMult = IsRaging ? HIT_STACK_MULTIPLIER : 1f;
                        float finalDmg = attackDamage * heavyMult * rageMult * ConsumeNextAttackBonus();
                        target.TakeDamage(finalDmg, transform.position, true, this);

                        // PLA-125: Hit stack increment
                        hitStack++;
                        hitStackTimer = HIT_STACK_DURATION;

                        comboWindowOpen = true;
                        comboWindowTimer = comboWindowDuration;
                        exMeter = Mathf.Min(exMeter + EX_GAIN_ON_HIT_DEALT, EX_METER_MAX);

                        if (!isAI && Volk.Core.MatchStatsTracker.Instance != null)
                            Volk.Core.MatchStatsTracker.Instance.RecordHitLanded(finalDmg);
                        if (!isAI && Volk.Core.ComboTracker.Instance != null)
                            Volk.Core.ComboTracker.Instance.RegisterInput(AttackType.Punch);

                        AudioManager.Instance?.PlayPunch();
                        AudioManager.Instance?.PlayLayeredHit(isHeavy, false);

                        float hitstopDur = isHeavy ? HitstopManager.HeavyHit : HitstopManager.LightHit;
                        HitstopManager.Instance?.Trigger(hitstopDur);
                        AudioManager.Instance?.PauseHitSounds(hitstopDur);
                        StartCoroutine(HitStop(hitstopDur));
                        if (target != null && !target.isDead) target.StartCoroutine(target.HitStop(hitstopDur));

                        Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                        HitEffectManager.Instance?.SpawnHitEffect(hitPos, false);
                        VibrationManager.Instance?.VibrateLight();
                        break;
                    }
                }
            }
            yield return null;
        }

        if (!hasDealtDamage)
        {
            AudioManager.Instance?.PlayWhiff();
            JuiceManager.Instance?.WhiffFreeze();
            OnAttackWhiff?.Invoke();
            isAttackActive = false;
        }

        // === RECOVERY PHASE — cancelable ===
        attackPhase = AttackPhase.Recovery;
        yield return new WaitForSeconds(recoveryTime);

        attackPhase = AttackPhase.None;
        anim.applyRootMotion = false;
        isAttacking = false;
        isAttackActive = false;
        postAttackCooldown = isHeavy ? 0.2f : 0.15f;
    }

    // PLA-125: Legacy DoAttack redirects to combo system
    IEnumerator DoAttack(int animHash, Transform hitPoint)
    {
        bool isKick = (animHash == hKick);
        isAttacking = true;
        isAttackActive = true;
        hasDealtDamage = false;
        anim.applyRootMotion = false;
        anim.SetTrigger(animHash);

        if (!isAI && behaviorTracker != null)
        {
            var action = isKick ? Volk.Core.PlayerAction.Kick : Volk.Core.PlayerAction.Punch;
            behaviorTracker.Record(GetCurrentSituation(), action);
        }

        AudioManager.Instance?.PlayWhoosh();

        // Startup
        attackPhase = AttackPhase.Startup;
        yield return new WaitForSeconds(0.15f);

        // Active
        attackPhase = AttackPhase.Active;
        float hitTimer = 0f;
        float hitWindowDuration = 0.35f;

        while (hitTimer < hitWindowDuration)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hasDealtDamage && hitPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.5f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        hasDealtDamage = true;
                        isAttackActive = false;

                        float rageMult = IsRaging ? HIT_STACK_MULTIPLIER : 1f;
                        float finalDmg = attackDamage * rageMult * ConsumeNextAttackBonus();
                        target.TakeDamage(finalDmg, transform.position, true, this);

                        hitStack++;
                        hitStackTimer = HIT_STACK_DURATION;

                        comboWindowOpen = true;
                        comboWindowTimer = comboWindowDuration;
                        exMeter = Mathf.Min(exMeter + EX_GAIN_ON_HIT_DEALT, EX_METER_MAX);

                        if (!isAI && Volk.Core.MatchStatsTracker.Instance != null)
                            Volk.Core.MatchStatsTracker.Instance.RecordHitLanded(finalDmg);
                        if (!isAI && Volk.Core.ComboTracker.Instance != null)
                            Volk.Core.ComboTracker.Instance.RegisterInput(isKick ? AttackType.Kick : AttackType.Punch);

                        if (isKick) AudioManager.Instance?.PlayKick();
                        else AudioManager.Instance?.PlayPunch();
                        AudioManager.Instance?.PlayLayeredHit(false, false);

                        HitstopManager.Instance?.Trigger(HitstopManager.LightHit);
                        AudioManager.Instance?.PauseHitSounds(HitstopManager.LightHit);
                        StartCoroutine(HitStop(HitstopManager.LightHit));
                        if (target != null && !target.isDead) target.StartCoroutine(target.HitStop(HitstopManager.LightHit));

                        Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                        HitEffectManager.Instance?.SpawnHitEffect(hitPos, isKick);
                        VibrationManager.Instance?.VibrateLight();
                        break;
                    }
                }
            }
            yield return null;
        }

        if (!hasDealtDamage)
        {
            AudioManager.Instance?.PlayWhiff();
            JuiceManager.Instance?.WhiffFreeze();
            OnAttackWhiff?.Invoke();
            isAttackActive = false;
        }

        // Recovery — cancelable
        attackPhase = AttackPhase.Recovery;
        yield return new WaitForSeconds(0.3f);

        attackPhase = AttackPhase.None;
        anim.applyRootMotion = false;
        isAttacking = false;
        isAttackActive = false;
        postAttackCooldown = 0.15f;
    }

    IEnumerator DoBlock()
    {
        isAttacking = true;
        anim.SetTrigger(hBlock);
        AudioManager.Instance?.PlayBlock();

        // Record behavior for ghost AI
        if (!isAI && behaviorTracker != null)
            behaviorTracker.Record(GetCurrentSituation(), Volk.Core.PlayerAction.Block);
        yield return new WaitForSeconds(0.8f);
        isAttacking = false;
    }

    public IEnumerator HitStop(float duration)
    {
        anim.speed = 0f;
        yield return new WaitForSecondsRealtime(duration);
        anim.speed = 1f;
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cam.transform.localPosition = originalPos;
    }

    /// <summary>
    /// Calculates final damage using defense formula:
    /// finalDamage = (attackerPower * rawDamage) / (defender.defense * 5 + attackerPower)
    /// Ensures damage is never zero and defense scales meaningfully.
    /// </summary>
    /// <summary>
    /// Damage formula — balanced for power/defense range 1-10:
    /// powerMult: 0.6x (power=1) to 1.5x (power=10)
    /// defReduction: 0% (defense=1) to ~50% (defense=10)
    /// finalDamage = rawDamage * powerMult * (1 - defReduction)
    /// </summary>
    public static float CalculateDamage(float rawDamage, float attackerPower, float defenderDefense)
    {
        float powerMult    = 0.5f + (attackerPower / 10f);          // 0.6 – 1.5
        float defReduction = (defenderDefense - 1f) / 18f;          // 0.0 – 0.5
        float result       = rawDamage * powerMult * (1f - defReduction);
        return Mathf.Max(result, 1f); // never zero
    }

    public void TakeDamage(float amount, Vector3 attackerPos = default, bool hasAttackerPos = false, Fighter attacker = null)
    {
        if (isDead) return;

        // Apply defense formula if attacker is known
        if (attacker != null)
            amount = CalculateDamage(amount, attacker.power, defense);

        CheckParryOnDamage(ref amount);
        CheckPerfectBlock(ref amount);

        // Super Armor: take damage but don't stagger during skill animations
        bool superArmorActive = characterData != null && characterData.hasSuperArmor && isPlayingSkillAnim;

        if (amount <= 0f) return;
        currentHP -= amount;

        // EX meter gain on damage taken
        exMeter = Mathf.Min(exMeter + EX_GAIN_ON_HIT_TAKEN, EX_METER_MAX);
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHP}/{maxHP}");

        // Track stats (player receiving damage)
        if (!isAI && Volk.Core.MatchStatsTracker.Instance != null)
            Volk.Core.MatchStatsTracker.Instance.RecordHitReceived(amount);

        float shakeMult = characterData != null ? characterData.cameraShakeMultiplier : 1f;
        JuiceManager.Instance?.CharacterShake(transform, 0.05f * shakeMult);
        StartCoroutine(ShakeCamera(0.1f, 0.05f * shakeMult));
        AudioManager.Instance?.PlayHit();
        float vibMult = attacker?.characterData?.vibrationMultiplier ?? 1f;
        VibrationManager.Instance?.VibrateLight(vibMult);

        // QUANTUM: Knockback calculation must use FP math for determinism.
        // All vector math here → FPVector3, multipliers → FP.
        if (hasAttackerPos || attackerPos.sqrMagnitude > 0.001f)
        {
            Vector3 dir = (transform.position - attackerPos).normalized;
            dir.y = 0;
            float kbResist = characterData != null ? (1f - characterData.knockbackResistance) : 1f;
            float weightMult = GetWeightKnockbackMultiplier();
            knockbackVelocity = dir * knockbackForce * kbResist * weightMult;
            knockbackTimer = knockbackDuration;
        }

        // AI state transition on hit
        if (isAI)
        {
            currentAIState = AIState.Stunned;
            aiStunnedTimer = 0.35f;
        }

        if (currentHP <= 0)
        {
            isDead = true;
            currentHP = 0;
            StopAllCoroutines();
            isAttackActive = false;
            hasDealtDamage = false;
            attackPhase = AttackPhase.None;
            anim.SetTrigger(hDeath);
            AudioManager.Instance?.PlayKnockout();
            VibrationManager.Instance?.VibrateKO(vibMult);
            JuiceManager.Instance?.ScreenFlash();
            JuiceManager.Instance?.SlowMotionKO();
            PostProcessAnimator.Instance?.KOPulse();

            // Ragdoll on KO — smooth blend from animation to physics
            var ragdoll = GetComponent<RagdollController>();
            if (ragdoll != null && hasAttackerPos)
            {
                Vector3 attackDir = (transform.position - attackerPos).normalized;
                ragdoll.BlendToRagdoll(0.3f, attackDir, knockbackForce * 2f);
            }

            // Save behavior profile on match end
            if (!isAI) behaviorTracker?.SaveProfile();

            if (GameManager.Instance != null)
                GameManager.Instance.OnFighterDied(!isAI);
        }
        else if (superArmorActive)
        {
            // Super Armor: take damage but don't interrupt skill animation
            Debug.Log($"{gameObject.name} Super Armor absorbed stagger!");
        }
        else if (isAI && Volk.Core.NewGamePlusManager.Instance != null && Volk.Core.NewGamePlusManager.Instance.EnemyHasHyperArmor())
        {
            // PLA-132: NG+ HyperArmor — enemies take damage but never stagger
            Debug.Log($"{gameObject.name} NG+ HyperArmor absorbed stagger!");
        }
        else
        {
            StopAllCoroutines();
            anim.speed = characterData != null ? characterData.animationSpeedMult : 1f;
            isAttacking = false;
            anim.applyRootMotion = false;

            // Stagger reaction based on damage amount + weight class
            if (amount >= 25f || (hasAttackerPos && knockbackForce > 3f))
            {
                anim.SetTrigger(hStaggerHeavy);
                if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash != hStaggerHeavy)
                    anim.SetTrigger(hHit2); // Fallback if StaggerHeavy not in controller
            }
            else
            {
                anim.SetTrigger(hStaggerLight);
                if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash != hStaggerLight)
                    anim.SetTrigger(hHit1); // Fallback if StaggerLight not in controller
            }

            StartCoroutine(HitRecovery());
        }
    }

    void OnPlayerAction(Volk.Core.PlayerAction action)
    {
        if (behaviorTracker == null) return;
        var situation = GetCurrentSituation();
        behaviorTracker.Record(situation, action);
    }

    Volk.Core.GameSituation GetCurrentSituation()
    {
        float hpRatio = currentHP / maxHP;
        if (hpRatio < 0.3f) return Volk.Core.GameSituation.LowHP;
        if (hpRatio > 0.8f) return Volk.Core.GameSituation.HighHP;

        if (aiTarget != null)
        {
            float dist = Vector3.Distance(transform.position, aiTarget.position);
            if (dist > 4f) return Volk.Core.GameSituation.NeutralFar;
            if (dist > 2f) return Volk.Core.GameSituation.NeutralMid;
            return Volk.Core.GameSituation.NeutralClose;
        }
        return Volk.Core.GameSituation.NeutralMid;
    }

    IEnumerator HitRecovery()
    {
        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    /// <summary>
    /// PLA-125: AnimationEvent callback — place on attack anim clips at impact frame.
    /// Ensures damage is only dealt once per attack, preventing double-damage on spam.
    /// </summary>
    // PLA-132: AnimationEvent callback — deals damage via OverlapSphere
    public void OnAttackHit()
    {
        if (hasDealtDamage || !isAttackActive) return;

        Transform hp = hitPoint;
        if (hp == null) return;

        Collider[] hits = Physics.OverlapSphere(hp.position, attackRange * 0.5f);
        foreach (var hit in hits)
        {
            Fighter target = hit.GetComponentInParent<Fighter>();
            if (target != null && target != this && hit.CompareTag(enemyTag))
            {
                hasDealtDamage = true;
                isAttackActive = false;

                bool isHeavy = attackPhase == AttackPhase.Active && comboStep >= 3;
                float heavyMult = isHeavy ? 1.5f : 1f;
                float rageMult = IsRaging ? HIT_STACK_MULTIPLIER : 1f;
                float finalDmg = attackDamage * heavyMult * rageMult * ConsumeNextAttackBonus();
                target.TakeDamage(finalDmg, transform.position, true, this);

                hitStack++;
                hitStackTimer = HIT_STACK_DURATION;
                comboWindowOpen = true;
                comboWindowTimer = comboWindowDuration;
                exMeter = Mathf.Min(exMeter + EX_GAIN_ON_HIT_DEALT, EX_METER_MAX);

                // PLA-132: Mastery progress on hit
                if (!isAI && characterData != null)
                    Volk.Core.CharacterMasteryManager.Instance?.AddProgressByRequirement(characterData.characterName, "hit");

                if (!isAI && Volk.Core.MatchStatsTracker.Instance != null)
                    Volk.Core.MatchStatsTracker.Instance.RecordHitLanded(finalDmg);

                float hitstopDur = isHeavy ? HitstopManager.HeavyHit : HitstopManager.LightHit;
                HitstopManager.Instance?.Trigger(hitstopDur);
                StartCoroutine(HitStop(hitstopDur));
                if (target != null && !target.isDead) target.StartCoroutine(target.HitStop(hitstopDur));

                AudioManager.Instance?.PlayPunch();
                AudioManager.Instance?.PlayLayeredHit(isHeavy, false);
                Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                HitEffectManager.Instance?.SpawnHitEffect(hitPos, false);
                VibrationManager.Instance?.VibrateLight();
                break;
            }
        }
    }

    void OnDestroy()
    {
        OnActionPerformed = null;
    }

    public void ResetForRound()
    {
        StopAllCoroutines();
        GetComponent<RagdollController>()?.ResetRagdoll();
        currentHP = maxHP;
        isAttacking = false;
        isAttackActive = false;
        hasDealtDamage = false;
        attackPhase = AttackPhase.None;
        comboStep = 0;
        hitStack = 0;
        hitStackTimer = 0f;
        isDead = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        comboWindowOpen = false;
        isCrouching = false;
        isParrying = false;
        postAttackCooldown = 0f;
        skill1CooldownTimer = 0f;
        skill2CooldownTimer = 0f;
        currentAIState = AIState.Idle;
        aiAttackCooldown = 0f;
        aiStunnedTimer = 0f;
        aiRetreatTimer = 0f;
        aiDecisionTimer = 0.5f;
        cc.height = normalHeight;
        cc.center = new Vector3(0, normalHeight / 2f, 0);
        anim.Rebind();
        anim.Update(0f);
        anim.applyRootMotion = false;
    }

    // --- Touch combat API ---

    public void DoAttack(AttackType type, AttackVariant variant)
    {
        bool canAttack = !isAttacking || comboWindowOpen;
        if (!canAttack || isDead) return;
        if (comboWindowOpen) { comboWindowOpen = false; StopAllCoroutines(); anim.speed = 1f; }

        int animHash = type == AttackType.Punch ? hPunch : hKick;
        Transform hitPoint = type == AttackType.Punch ? rightHandPoint : rightFootPoint;

        // Track combo input
        if (!isAI && Volk.Core.ComboTracker.Instance != null)
            Volk.Core.ComboTracker.Instance.RegisterInput(type);

        // Behavior tracking
        if (!isAI)
        {
            var action = variant == AttackVariant.Heavy
                ? Volk.Core.PlayerAction.HeavyPunch
                : (type == AttackType.Punch ? Volk.Core.PlayerAction.Punch : Volk.Core.PlayerAction.Kick);
            OnActionPerformed?.Invoke(action);
        }

        switch (variant)
        {
            case AttackVariant.Normal:
                StartCoroutine(DoAttack(animHash, hitPoint));
                break;
            case AttackVariant.Heavy:
                StartCoroutine(DoHeavyAttack(animHash, hitPoint));
                break;
            case AttackVariant.Special:
                Debug.Log($"[Fighter] Special {type} (placeholder)");
                break;
        }
    }

    // PLA-127: Aligned with PLA-125 anti-spam + phase system
    IEnumerator DoHeavyAttack(int animHash, Transform hitPoint)
    {
        isAttacking = true;
        isAttackActive = true;
        hasDealtDamage = false;
        anim.applyRootMotion = false;
        anim.SetTrigger(animHash);
        AudioManager.Instance?.PlayWhoosh();

        // === STARTUP PHASE ===
        attackPhase = AttackPhase.Startup;
        yield return new WaitForSeconds(0.25f);

        // === ACTIVE PHASE ===
        attackPhase = AttackPhase.Active;
        float hitTimer = 0f;
        float heavyDamage = attackDamage * 2f;

        while (hitTimer < 0.45f)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hasDealtDamage && hitPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.6f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        hasDealtDamage = true;
                        isAttackActive = false;

                        float rageMult = IsRaging ? HIT_STACK_MULTIPLIER : 1f;
                        float finalDmg = heavyDamage * rageMult * ConsumeNextAttackBonus();
                        target.TakeDamage(finalDmg, transform.position, true, this);

                        hitStack++;
                        hitStackTimer = HIT_STACK_DURATION;
                        comboWindowOpen = true;
                        comboWindowTimer = comboWindowDuration;
                        exMeter = Mathf.Min(exMeter + EX_GAIN_ON_HIT_DEALT, EX_METER_MAX);

                        AudioManager.Instance?.PlayLayeredHit(true, false);
                        HitstopManager.Instance?.Trigger(HitstopManager.HeavyHit);
                        AudioManager.Instance?.PauseHitSounds(HitstopManager.HeavyHit);
                        StartCoroutine(HitStop(HitstopManager.HeavyHit));
                        if (target != null && !target.isDead) target.StartCoroutine(target.HitStop(HitstopManager.HeavyHit));
                        Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                        HitEffectManager.Instance?.SpawnHitEffect(hitPos, animHash == hKick);
                        VibrationManager.Instance?.VibrateLight();
                        break;
                    }
                }
            }
            yield return null;
        }

        if (!hasDealtDamage)
        {
            AudioManager.Instance?.PlayWhiff();
            JuiceManager.Instance?.WhiffFreeze();
            OnAttackWhiff?.Invoke();
            isAttackActive = false;
        }

        // === RECOVERY PHASE ===
        attackPhase = AttackPhase.Recovery;
        yield return new WaitForSeconds(0.45f);

        attackPhase = AttackPhase.None;
        anim.applyRootMotion = false;
        isAttacking = false;
        isAttackActive = false;
        postAttackCooldown = 0.2f;
    }

    public void AttemptParry()
    {
        if (isAttacking || isDead || isParrying) return;
        if (!isAI) OnActionPerformed?.Invoke(Volk.Core.PlayerAction.Parry);
        StartCoroutine(ParryWindow());
    }

    IEnumerator ParryWindow()
    {
        isParrying = true;
        anim.SetTrigger(hBlock);
        parryTimer = 0.25f;

        while (parryTimer > 0f)
        {
            parryTimer -= Time.deltaTime;
            yield return null;
        }

        isParrying = false;
    }

    public void UseSkill(int skillIndex)
    {
        if (isAttacking || isDead) return;
        if (!isAI) OnActionPerformed?.Invoke(skillIndex == 1 ? Volk.Core.PlayerAction.Skill1 : Volk.Core.PlayerAction.Skill2);
        if (characterData == null) { Debug.Log($"[Fighter] No CharacterData, skill {skillIndex} skipped"); return; }

        Volk.Core.SkillBase skill = skillIndex == 1 ? characterData.skill1 : characterData.skill2;
        if (skill == null) { Debug.Log($"[Fighter] Skill {skillIndex} not assigned"); return; }

        float cooldownTimer = skillIndex == 1 ? skill1CooldownTimer : skill2CooldownTimer;
        if (cooldownTimer > 0f) { Debug.Log($"[Fighter] Skill {skill.skillName} on cooldown ({cooldownTimer:F1}s)"); return; }

        StartCoroutine(DoSkillAttack(skill, skillIndex));
    }

    /// <summary>
    /// EX Skill: Costs full EX meter, deals exDamageMultiplier damage.
    /// Called on long-press of skill button when meter is full.
    /// </summary>
    public void UseSkillEX(int skillIndex)
    {
        if (isAttacking || isDead) return;
        if (exMeter < EX_METER_MAX) return;
        if (characterData == null) return;

        Volk.Core.SkillBase skill = skillIndex == 1 ? characterData.skill1 : characterData.skill2;
        if (skill == null) return;

        exMeter = 0f;
        JuiceManager.Instance?.ExSkillEffect();
        VibrationManager.Instance?.VibrateEX(characterData.vibrationMultiplier);
        StartCoroutine(DoSkillAttack(skill, skillIndex, characterData.exDamageMultiplier));
    }

    /// <summary>
    /// Perfect Block: 200ms window where blocking negates all damage and stuns attacker.
    /// </summary>
    public void AttemptPerfectBlock()
    {
        if (isAttacking || isDead || isParrying || isPerfectBlockWindow) return;
        if (!isAI) OnActionPerformed?.Invoke(Volk.Core.PlayerAction.Parry);
        StartCoroutine(PerfectBlockWindow());
    }

    IEnumerator PerfectBlockWindow()
    {
        isPerfectBlockWindow = true;
        anim.SetTrigger(hBlock);
        perfectBlockTimer = PERFECT_BLOCK_WINDOW;

        while (perfectBlockTimer > 0f)
        {
            perfectBlockTimer -= Time.deltaTime;
            yield return null;
        }

        isPerfectBlockWindow = false;
    }

    private void CheckPerfectBlock(ref float amount)
    {
        if (!isPerfectBlockWindow || amount <= 0f) return;

        // Perfect block: negate all damage
        amount = 0f;
        isPerfectBlockWindow = false;

        // Stun the attacker for 0.5s
        Fighter attacker = FindAttackerInRange();
        if (attacker != null)
            attacker.ApplyStun(0.5f);

        Debug.Log($"{gameObject.name} PERFECT BLOCK!");
        JuiceManager.Instance?.ScreenFlash(0.3f);
        AudioManager.Instance?.PlayHit();
    }

    IEnumerator DoSkillAttack(Volk.Core.SkillBase skill, int skillIndex, float damageMultiplier = 1f)
    {
        isAttacking = true;
        isPlayingSkillAnim = true;
        anim.applyRootMotion = false;

        // Trigger animation if set
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            int animHash = Animator.StringToHash(skill.animationTrigger);
            anim.SetTrigger(animHash);
        }

        // Set cooldown
        if (skillIndex == 1) skill1CooldownTimer = skill.cooldown;
        else skill2CooldownTimer = skill.cooldown;

        // Play skill SFX
        if (skill.sfxClip != null)
            AudioManager.Instance?.PlayOneShot(skill.sfxClip);

        yield return new WaitForSeconds(0.2f);

        // Find nearest enemy target for skill execution (use cached aiTarget)
        Fighter skillTarget = null;
        if (aiTarget != null) skillTarget = aiTarget.GetComponent<Fighter>();
        if (skillTarget == null && aiTarget == null)
        {
            aiTarget = GameObject.FindWithTag(enemyTag)?.transform;
            if (aiTarget != null) skillTarget = aiTarget.GetComponent<Fighter>();
        }

        // Execute skill behavior (command pattern)
        skill.Execute(this, skillTarget);

        // Legacy hit detection fallback (for SkillData / simple skills with no Execute logic)
        float hitWindowDuration = 0.4f;
        float hitTimer = 0f;
        bool hitLanded = false;
        Transform hitPoint = rightHandPoint != null ? rightHandPoint : rightFootPoint;

        while (hitTimer < hitWindowDuration)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hitLanded && hitPoint != null)
            {
                // QUANTUM: Deterministic physics query needed for EX skill hit detection
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.6f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        // Legacy path — only for SkillData, specialized skills self-handle
                        if (skill is Volk.Core.SkillData)
                            target.TakeDamage(skill.damage * damageMultiplier, transform.position, true, this);
                        hitLanded = true;
                        AudioManager.Instance?.PlaySkillHit();

                        // VFX
                        if (skill.vfxPrefab != null)
                        {
                            Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                            Instantiate(skill.vfxPrefab, hitPos, Quaternion.identity);
                        }
                        else
                        {
                            Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                            HitEffectManager.Instance?.SpawnHitEffect(hitPos, false);
                        }

                        HitstopManager.Instance?.Trigger(HitstopManager.SkillHit);
                        StartCoroutine(HitStop(HitstopManager.SkillHit));
                        if (target != null && !target.isDead) target.StartCoroutine(target.HitStop(HitstopManager.SkillHit));
                        float skillVibMult = characterData?.vibrationMultiplier ?? 1f;
                        VibrationManager.Instance?.VibrateHeavy(skillVibMult);
                        break;
                    }
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.7f);
        anim.applyRootMotion = false;
        isAttacking = false;
        isPlayingSkillAnim = false;
        postAttackCooldown = 0.2f;
    }

    // Override TakeDamage to check parry
    private void CheckParryOnDamage(ref float amount)
    {
        if (isParrying)
        {
            amount = 0f;
            isParrying = false;
            StopCoroutine(nameof(ParryWindow));
            Debug.Log($"{gameObject.name} PARRIED!");
        }

        // Counter reflect: if we're in counter window, reflect damage back
        if (isCounterActive && amount > 0f)
        {
            Fighter attacker = FindAttackerInRange();
            if (attacker != null)
                attacker.TakeDamage(amount * counterReflectMult, transform.position, true);
            amount = 0f;
            isCounterActive = false;
            Debug.Log($"{gameObject.name} COUNTERED!");
        }
    }

    // ── SKILL HELPER METHODS ────────────────────────────────────────────────

    // Knockback
    public void ApplyKnockback(Vector3 attackerPos, float multiplier)
    {
        Vector3 dir = (transform.position - attackerPos).normalized;
        dir.y = 0;
        knockbackVelocity = dir * knockbackForce * multiplier;
        knockbackTimer = knockbackDuration * multiplier * 0.5f;
    }

    // Stun
    private float stunTimer;
    private bool isStunned;
    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        // AI stun via FSM
        if (isAI)
        {
            currentAIState = AIState.Stunned;
            aiStunnedTimer = duration;
        }
        // Player stun: block input via postAttackCooldown
        else
        {
            postAttackCooldown = Mathf.Max(postAttackCooldown, duration);
        }
    }

    // Counter
    private bool isCounterActive;
    private float counterReflectMult;
    public void SetCounterActive(bool active, float reflectMult)
    {
        isCounterActive = active;
        counterReflectMult = reflectMult;
    }

    // Next attack bonus
    private float nextAttackBonus = 1f;
    public void SetNextAttackBonus(float multiplier) => nextAttackBonus = multiplier;
    public float ConsumeNextAttackBonus()
    {
        float b = nextAttackBonus;
        nextAttackBonus = 1f;
        return b;
    }

    // Find nearest enemy for counter
    private Fighter FindAttackerInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        foreach (var h in hits)
        {
            Fighter f = h.GetComponentInParent<Fighter>();
            if (f != null && f != this && h.CompareTag(enemyTag)) return f;
        }
        return null;
    }

    /// <summary>
    /// Weight class knockback multiplier: Heavy=0.5x, Medium=1x, Light=1.5x
    /// </summary>
    float GetWeightKnockbackMultiplier()
    {
        if (characterData == null) return 1f;
        return characterData.weightClass switch
        {
            Volk.Core.WeightClass.Heavy => 0.5f,
            Volk.Core.WeightClass.Light => 1.5f,
            _ => 1f
        };
    }

    /// <summary>Is the ragdoll currently active (KO state)?</summary>
    public bool IsRagdollActive()
    {
        var ragdoll = GetComponent<RagdollController>();
        return ragdoll != null && ragdoll.IsActive;
    }

    void OnDrawGizmosSelected()
    {
        if (rightHandPoint) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(rightHandPoint.position, attackRange * 0.5f); }
        if (rightFootPoint) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(rightFootPoint.position, attackRange * 0.5f); }
    }
}
