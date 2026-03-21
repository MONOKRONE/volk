using UnityEngine;
using System.Collections;

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

    [Header("AI - leave null for player")]
    public Transform aiTarget;
    public bool isAI = false;
    public float aiAttackRange = 2.5f;
    public float aiAttackCooldown = 2.5f;

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

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        anim.applyRootMotion = false;
        currentHP = maxHP;
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
        if (cc.isGrounded) yVelocity = -1f;
        else yVelocity -= 9.81f * Time.deltaTime;

        if (isAI) UpdateAI();
        else UpdatePlayer();
    }

    void UpdatePlayer()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0, v).normalized;
        bool run = Input.GetKey(KeyCode.LeftShift);

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

        float speed = run ? runSpeed : walkSpeed;
        Vector3 move = dir * speed;
        move.y = yVelocity;
        cc.Move(move * Time.deltaTime);

        if (dir.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), rotSpeed * Time.deltaTime);

        anim.SetBool(hWalk, dir.magnitude > 0.1f && !run);
        anim.SetBool(hRun, dir.magnitude > 0.1f && run);
    }

    void UpdateAI()
    {
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
                        target.TakeDamage(attackDamage);
                        hitLanded = true;
                        StartCoroutine(Hitstop());
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

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHP -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHP}/{maxHP}");

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

    void OnDrawGizmosSelected()
    {
        if (rightHandPoint) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(rightHandPoint.position, attackRange * 0.5f); }
        if (rightFootPoint) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(rightFootPoint.position, attackRange * 0.5f); }
    }
}
