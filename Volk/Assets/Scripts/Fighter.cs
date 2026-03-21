using UnityEngine;
using System.Collections;

public enum AttackType { Punch, Kick }
public enum AttackVariant { Normal, Heavy, Special }

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
    public float aiAttackRange = 2.5f;
    public float aiAttackCooldown = 2.5f;

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
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.2f;
    private Vector3 knockbackVelocity;
    private float knockbackTimer;

    // Private
    private CharacterController cc;
    private Animator anim;
    private bool isAttacking;
    private bool isDead;
    private float yVelocity;
    private float aiAttackTimer;
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
    }

    void Update()
    {
        if (isDead) return;

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
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 10f * Time.deltaTime);
            cc.Move((knockbackVelocity + new Vector3(0, yVelocity, 0)) * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            return;
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
        if (!isAttacking)
        {
            if (Input.GetKeyDown(KeyCode.J)) { StartCoroutine(DoAttack(hPunch, rightHandPoint)); return; }
            if (Input.GetKeyDown(KeyCode.K)) { StartCoroutine(DoAttack(hKick, rightFootPoint)); return; }
            if (Input.GetKeyDown(KeyCode.L)) { StartCoroutine(DoBlock()); return; }
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
        if (knockbackTimer > 0f)
        {
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 10f * Time.deltaTime);
            cc.Move((knockbackVelocity + new Vector3(0, yVelocity, 0)) * Time.deltaTime);
            knockbackTimer -= Time.deltaTime;
            return;
        }

        if (aiTarget == null) aiTarget = GameObject.FindWithTag(enemyTag)?.transform;
        if (aiTarget == null) return;

        float dist = Vector3.Distance(transform.position, aiTarget.position);
        aiAttackTimer -= Time.deltaTime;

        if (dist > aiAttackRange)
        {
            if (!isAttacking)
            {
                Vector3 dir = (aiTarget.position - transform.position).normalized;
                dir.y = 0;
                Vector3 move = dir * walkSpeed;
                move.y = yVelocity;
                cc.Move(move * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), rotSpeed * Time.deltaTime);
                anim.SetBool(hWalk, true);
                anim.SetBool(hRun, false);
            }
        }
        else
        {
            anim.SetBool(hWalk, false);
            anim.SetBool(hRun, false);
            cc.Move(new Vector3(0, yVelocity, 0) * Time.deltaTime);

            if (!isAttacking && aiAttackTimer <= 0f)
            {
                aiAttackTimer = aiAttackCooldown + Random.Range(0f, 1f);
                bool usePunch = Random.value > 0.5f;
                StartCoroutine(DoAttack(usePunch ? hPunch : hKick, usePunch ? rightHandPoint : rightFootPoint));
            }
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
                        StartCoroutine(HitStop(0.08f));
                        target.StartCoroutine(target.HitStop(0.08f));
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

        // Knockback
        if (attackerPos != default)
        {
            Vector3 dir = (transform.position - attackerPos).normalized;
            dir.y = 0;
            knockbackVelocity = dir * knockbackForce;
            knockbackTimer = knockbackDuration;
        }

        if (currentHP <= 0)
        {
            isDead = true;
            currentHP = 0;
            anim.SetTrigger(hDeath);
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

    // --- Touch combat API ---

    public void DoAttack(AttackType type, AttackVariant variant)
    {
        if (isAttacking || isDead) return;

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
