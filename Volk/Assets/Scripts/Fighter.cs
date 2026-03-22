using UnityEngine;
using System.Collections;

public enum AttackType { Punch, Kick }
public enum AttackVariant { Normal, Heavy, Special }
public enum AIDifficulty { Easy, Normal, Hard }
public enum AIState { Idle, Approach, Combat, Retreat, Stunned }

public class Fighter : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public float attackDamage = 15f;
    public float attackRange = 1.2f;
    public string enemyTag = "Enemy";

    [Header("Attack Points - assign hand/foot transforms")]
    public Transform rightHandPoint;
    public Transform rightFootPoint;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float rotSpeed = 10f;
    public float jumpHeight = 1.8f;

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
    private float aiApproachStop = 1.4f;

    // Touch input
    [HideInInspector] public Vector2 touchMoveInput;
    [HideInInspector] public bool useTouchMovement;
    private TouchInputHandler touchHandler;

    // Parry
    private bool isParrying;
    private float parryTimer;

    // Jump & Crouch
    [HideInInspector] public bool isCrouching;
    private float normalHeight;
    private float crouchHeight;

    // Knockback
    public float knockbackForce = 2f;
    public float knockbackDuration = 0.2f;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    // Combo
    [HideInInspector] public bool comboWindowOpen;
    public float comboWindowDuration = 0.4f;
    private float comboWindowTimer;

    // Private
    private CharacterController cc;
    private Animator anim;
    public bool isAttacking { get; private set; }
    public bool isDead { get; private set; }
    private float yVelocity;
    private float postAttackCooldown;

    // Animator hashes
    static int hWalk = Animator.StringToHash("IsWalking");
    static int hRun = Animator.StringToHash("IsRunning");
    static int hPunch = Animator.StringToHash("HookPunch");
    static int hKick = Animator.StringToHash("MMAKick");
    static int hBlock = Animator.StringToHash("BodyBlock");
    static int hHit1 = Animator.StringToHash("TakingPunch");
    static int hHit2 = Animator.StringToHash("ReceivingUppercut");
    static int hDeath = Animator.StringToHash("Death");
    static int hJump = Animator.StringToHash("IsJumping");
    static int hCrouch = Animator.StringToHash("IsCrouching");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        currentHP = maxHP;
        normalHeight = cc.height;
        crouchHeight = cc.height * 0.55f;

        if (Application.isMobilePlatform || useTouchMovement)
            touchHandler = FindFirstObjectByType<TouchInputHandler>();

        if (isAI) InitAIDifficulty();
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

        if (comboWindowOpen)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
                comboWindowOpen = false;
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

        // Gravity
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
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
            cc.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            return;
        }
        else
        {
            knockbackVelocity = Vector3.zero;
        }

        bool mobile = touchHandler != null && (Application.isMobilePlatform || useTouchMovement);
        float h = mobile ? touchHandler.MoveInput.x : Input.GetAxisRaw("Horizontal");
        float v = mobile ? touchHandler.MoveInput.y : Input.GetAxisRaw("Vertical");
        Vector3 rawDir = new Vector3(h, 0, v);
        float inputMag = rawDir.magnitude;
        Vector3 dir = inputMag > 0.15f ? rawDir.normalized : Vector3.zero;
        bool run = false;

        // Jump
        if (touchHandler != null && touchHandler.JumpTriggered && cc.isGrounded)
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        anim.SetBool(hJump, !cc.isGrounded);

        // Crouch toggle
        if (touchHandler != null && touchHandler.CrouchTriggered)
        {
            isCrouching = !isCrouching;
            cc.height = isCrouching ? crouchHeight : normalHeight;
            cc.center = new Vector3(0, cc.height / 2f, 0);
        }
        anim.SetBool(hCrouch, isCrouching);

        // Check attack input FIRST - before any movement
        bool canAttack = !isAttacking || comboWindowOpen;
        if (canAttack)
        {
            if (Input.GetKeyDown(KeyCode.J)) { comboWindowOpen = false; StopAllCoroutines(); StartCoroutine(DoAttack(hPunch, rightHandPoint)); return; }
            if (Input.GetKeyDown(KeyCode.K)) { comboWindowOpen = false; StopAllCoroutines(); StartCoroutine(DoAttack(hKick, rightFootPoint)); return; }
            if (!isAttacking && Input.GetKeyDown(KeyCode.L)) { StartCoroutine(DoBlock()); return; }
        }

        if (isAttacking)
        {
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
            return;
        }

        if (inputMag > 0.15f)
        {
            float speed = run ? runSpeed : walkSpeed;
            Vector3 move = dir * speed;
            move.y = yVelocity;
            cc.Move(move * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), rotSpeed * Time.deltaTime);

            anim.SetBool(hWalk, !run);
            anim.SetBool(hRun, run);
        }
        else
        {
            // No input — only gravity, zero XZ
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
        }
    }

    void UpdateAI()
    {
        if (aiTarget == null) aiTarget = GameObject.FindWithTag(enemyTag)?.transform;
        if (aiTarget == null) return;

        // Knockback override
        if (knockbackTimer > 0f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 8f * Time.deltaTime);
            cc.Move(knockbackVelocity * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            currentAIState = AIState.Stunned;
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
                    currentAIState = AIState.Approach;
                break;

            case AIState.Approach:
                if (hpRatio < retreatHPThreshold)
                { currentAIState = AIState.Retreat; aiRetreatTimer = 1.5f; break; }

                if (distToTarget <= aiAttackRange)
                { currentAIState = AIState.Combat; aiAttackCooldown = reactionTime; break; }

                if (!isAttacking)
                {
                    Vector3 approachDir = (aiTarget.position - transform.position).normalized;
                    approachDir.y = 0;
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
                    transform.rotation = Quaternion.Slerp(transform.rotation,
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
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(faceDir), rotSpeed * Time.deltaTime);

                if (distToTarget > aiAttackRange + 0.5f)
                { currentAIState = AIState.Approach; break; }

                if (hpRatio < retreatHPThreshold)
                { currentAIState = AIState.Retreat; aiRetreatTimer = 1.5f; break; }

                // Try parry if target is attacking
                Fighter targetFighter = aiTarget.GetComponent<Fighter>();
                if (targetFighter != null && targetFighter.isAttacking && !isAttacking)
                {
                    if (Random.value < parryChance * Time.deltaTime * 10f)
                        AttemptParry();
                }

                // Attack on cooldown
                aiAttackCooldown -= Time.deltaTime;
                if (aiAttackCooldown <= 0f && !isAttacking)
                {
                    bool usePunch = Random.value < 0.6f;
                    StartCoroutine(DoAttack(usePunch ? hPunch : hKick,
                        usePunch ? rightHandPoint : rightFootPoint));
                    aiAttackCooldown = reactionTime + Random.Range(-0.1f, 0.3f);
                }

                anim.SetBool(hWalk, false);
                anim.SetBool(hRun, false);
                cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);
                break;

            case AIState.Retreat:
                aiRetreatTimer -= Time.deltaTime;
                if (aiRetreatTimer <= 0f)
                { currentAIState = AIState.Approach; break; }

                Vector3 retreatDir = (transform.position - aiTarget.position).normalized;
                retreatDir.y = 0;
                Vector3 retreatMove = retreatDir * walkSpeed;
                retreatMove.y = yVelocity;
                cc.Move(retreatMove * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation,
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
                    currentAIState = AIState.Approach;
                break;
        }
    }

    IEnumerator DoAttack(int animHash, Transform hitPoint)
    {
        isAttacking = true;
        anim.applyRootMotion = false;
        anim.SetTrigger(animHash);

        // Wait longer for animator to enter the attack state
        yield return new WaitForSeconds(0.15f);

        // Use fixed timing instead of clip length - more reliable
        float hitWindowDuration = 0.35f;
        float hitTimer = 0f;
        bool hitLanded = false;

        while (hitTimer < hitWindowDuration)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hitLanded && hitPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.5f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        target.TakeDamage(attackDamage, transform.position);
                        hitLanded = true;
                        comboWindowOpen = true;
                        comboWindowTimer = comboWindowDuration;

                        // Sound only on confirmed hit
                        if (animHash == hKick) AudioManager.Instance?.PlayKick();
                        else AudioManager.Instance?.PlayPunch();

                        StartCoroutine(HitStop(0.08f));
                        target.StartCoroutine(target.HitStop(0.08f));

                        // Spawn hit effect
                        Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                        bool isKick = (animHash == hKick);
                        HitEffectManager.Instance?.SpawnHitEffect(hitPos, isKick);
                        VibrationManager.Instance?.VibrateLight();
                        break;
                    }
                }
            }
            yield return null;
        }

        // Wait for rest of animation
        yield return new WaitForSeconds(0.6f);
        anim.applyRootMotion = false;
        isAttacking = false;
        postAttackCooldown = 0.15f;
    }

    IEnumerator DoBlock()
    {
        isAttacking = true;
        anim.SetTrigger(hBlock);
        yield return new WaitForSeconds(0.8f);
        isAttacking = false;
    }

    IEnumerator Hitstop()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.08f);
        Time.timeScale = 1f;
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

    public void TakeDamage(float amount, Vector3 attackerPos = default)
    {
        if (isDead) return;
        CheckParryOnDamage(ref amount);
        if (amount <= 0f) return;
        currentHP -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHP}/{maxHP}");
        StartCoroutine(ShakeCamera(0.1f, 0.05f));
        AudioManager.Instance?.PlayHit();
        VibrationManager.Instance?.VibrateLight();

        // Knockback
        if (attackerPos != default)
        {
            Vector3 dir = (transform.position - attackerPos).normalized;
            dir.y = 0;
            knockbackVelocity = dir * knockbackForce;
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
            anim.SetTrigger(hDeath);
            AudioManager.Instance?.PlayFall();
            VibrationManager.Instance?.VibrateHeavy();
            if (GameManager.Instance != null)
                GameManager.Instance.OnFighterDied(!isAI);
        }
        else
        {
            StopAllCoroutines();
            isAttacking = false;
            anim.applyRootMotion = false;
            anim.SetTrigger(Random.value > 0.5f ? hHit1 : hHit2);
            StartCoroutine(HitRecovery());
        }
    }

    IEnumerator HitRecovery()
    {
        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    public void ResetForRound()
    {
        StopAllCoroutines();
        currentHP = maxHP;
        isAttacking = false;
        isDead = false;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        comboWindowOpen = false;
        isCrouching = false;
        isParrying = false;
        postAttackCooldown = 0f;
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
        if (comboWindowOpen) { comboWindowOpen = false; StopAllCoroutines(); }

        int animHash = type == AttackType.Punch ? hPunch : hKick;
        Transform hitPoint = type == AttackType.Punch ? rightHandPoint : rightFootPoint;

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

    IEnumerator DoHeavyAttack(int animHash, Transform hitPoint)
    {
        isAttacking = true;
        anim.applyRootMotion = false;
        anim.SetTrigger(animHash);

        yield return new WaitForSeconds(0.2f);

        float hitWindowDuration = 0.45f;
        float hitTimer = 0f;
        bool hitLanded = false;
        float heavyDamage = attackDamage * 2f;

        while (hitTimer < hitWindowDuration)
        {
            hitTimer += Time.deltaTime;
            anim.applyRootMotion = false;

            if (!hitLanded && hitPoint != null)
            {
                Collider[] hits = Physics.OverlapSphere(hitPoint.position, attackRange * 0.6f);
                foreach (var hit in hits)
                {
                    Fighter target = hit.GetComponentInParent<Fighter>();
                    if (target != null && target != this && hit.CompareTag(enemyTag))
                    {
                        target.TakeDamage(heavyDamage, transform.position);
                        hitLanded = true;
                        StartCoroutine(Hitstop());
                        Vector3 hitPos = target.transform.position + Vector3.up * 1.2f;
                        HitEffectManager.Instance?.SpawnHitEffect(hitPos, animHash == hKick);
                        break;
                    }
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.9f); // 1.5x duration
        anim.applyRootMotion = false;
        isAttacking = false;
        postAttackCooldown = 0.2f;
    }

    public void AttemptParry()
    {
        if (isAttacking || isDead || isParrying) return;
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
        Debug.Log($"[Fighter] UseSkill({skillIndex}) - placeholder");
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
            // Could trigger counter animation here
        }
    }

    void OnDrawGizmosSelected()
    {
        if (rightHandPoint) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(rightHandPoint.position, attackRange * 0.5f); }
        if (rightFootPoint) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(rightFootPoint.position, attackRange * 0.5f); }
    }
}
