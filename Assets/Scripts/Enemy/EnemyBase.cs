using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class EnemyBase : MonoBehaviour
{


    public Transform Player => player;

    protected bool enableNav = true;

    // ===== AI settings =====
    [Header("Movement")]
    [SerializeField] protected float Speed = 3.5f;
    [SerializeField] protected float acceleration = 8f;
    [SerializeField] protected float rotationSpeed = 8f;
    [SerializeField] protected float stoppingDistance = 1.5f;

    protected NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    protected Transform player;

    [Header("Hitbox Settings")]
    [SerializeField] protected BoxCollider attackHitbox;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected bool triggerhit;

    [Header("Attack Settings")]
    [SerializeField] protected bool canAttack = false;
    [SerializeField] protected string attackParamName = "canAttack";   // Animator bool parameter
    [Header("Attack Angle")]
    private Animator animator; // Automatically found in Awake
    [Header("Attack Angle")]
    [SerializeField] protected float attackAngle = 60f;

    [Header("Attack Timing")]
    [SerializeField] protected float attackDuration = 2f;
    [SerializeField] protected float attackCooldown = 4f;

    [SerializeField] protected bool isAttacking = false;
    protected bool attackTriggered = false;
    [SerializeField] protected float attackTimer = 0f;
    [SerializeField] protected float cooldownTimer = 0f;
    [SerializeField] float FinleSpeed = 3.5f;
    [SerializeField] private float negetivepush = 1;
    Coroutine slowCoroutine;
    float originalSpeed;
    // ===== Unity Callbacks =====

    public void ApplySlow(float slowMultiplier, float duration)
    {
        if (agent == null) return;

        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowRoutine(slowMultiplier, duration));
    }
    private IEnumerator SlowRoutine(float slowMultiplier, float duration)
    {
        originalSpeed = Speed;

        Speed *= slowMultiplier;
        agent.speed = Speed;

        yield return new WaitForSeconds(duration);

        Speed = originalSpeed;
        agent.speed = Speed;

        slowCoroutine = null;
    }
    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        FinleSpeed = Speed;
        agent.speed = FinleSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
        agent.autoBraking = true;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (attackHitbox != null)
            attackHitbox.isTrigger = true;
    }

    protected virtual void Update()
    {
        if (player == null) return;
        HandleAttackTimers();
        CheckAttackCondition();
        agent.speed = FinleSpeed;

        UpdateHitBox();
        EnforcePlayerDistance();
        MoveTo(player.position);

    }

    // ===== Movement =====
    protected void MoveTo(Vector3 position)
    {
        if (agent == null) return;

        float distance = Vector3.Distance(transform.position, position);

        if (distance > stoppingDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(position);
            RotateTowardsMovement();
        }
        else
        {
            StopMovement();
        }
    }

    protected void StopMovement()
    {
        if (agent != null)
            agent.isStopped = true;
    }

    private void RotateTowardsMovement()
    {
        if (agent.velocity.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // ===== Hitbox =====
    private void UpdateHitBox()
    {
        if (attackHitbox == null) return;

        attackHitbox.enabled = triggerhit;

        if (triggerhit)
        {
            attackHitbox.center = new Vector3(0f, 0f, attackRange / 2f);
            attackHitbox.size = new Vector3(
                attackHitbox.size.x,
                attackHitbox.size.y,
                attackRange
            );
        }
    }

    public void StartHit() => triggerhit = true;
    public void EndHit() => triggerhit = false;

    // ===== Attack Condition =====
    private void CheckAttackCondition()
    {

        if (player == null) return;
        // Countdown cooldown
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer < 0f) attackTimer = 0f;
        }
        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        // Check if within attack range AND facing the player
        bool inRange = distance <= attackRange;
        bool inAngle = angle <= attackAngle * 0.5f;

        if (inRange && inAngle && cooldownTimer <= 0f && !isAttacking)
        {
            // Start attack
            isAttacking = true;
            canAttack = true;
            attackTimer = attackDuration;
            FinleSpeed = Speed * 0.1f;


        }
        if (attackTimer <= 0f && cooldownTimer >= 0f)
        {

            FinleSpeed = Speed;
            // Attack finished
            isAttacking = false;
            canAttack = false;

            // Resume movement
            if (agent != null)
                agent.isStopped = false;
        }
    }

    // ===== Attack Timers =====
    private void HandleAttackTimers()
    {
        // Countdown cooldown
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer < 0f) cooldownTimer = 0f;
        }

        if (animator == null || string.IsNullOrEmpty(attackParamName))
            return;

        // Trigger attack pulse
        if (canAttack && cooldownTimer == 0f && !attackTriggered)
        {
            animator.SetBool(attackParamName, true);
            StartCoroutine(ResetAttackBool());
            cooldownTimer = attackCooldown;
            attackTriggered = true;
        }
    }
    private IEnumerator ResetAttackBool()
    {
        // Short pulse for animator
        yield return new WaitForSeconds(0.001f);
        animator.SetBool(attackParamName, false);
        attackTriggered = false; // allow next attack after cooldown
    }
    private void EnforcePlayerDistance()
    {
        if (player == null) return;

        float minDistance = stoppingDistance - negetivepush;
        Vector3 direction = transform.position - player.position;
        float currentDistance = direction.magnitude;

        // If enemy is too close to player
        if (currentDistance < minDistance)
        {
            Vector3 pushDir = direction.normalized;

            // If exactly same position (rare but safe check)
            if (pushDir == Vector3.zero)
                pushDir = -player.forward;

            Vector3 targetPosition = player.position + pushDir * minDistance;

            // Move enemy to safe position
            agent.Warp(targetPosition);
        }
    }



















}



//public Transform Player => player;

//    protected bool enableNav = true;
//    protected NavMeshAgent agent;
//    public NavMeshAgent Agent => agent;










//// Enemy BaseClass
//public abstract class EnemyBase : MonoBehaviour, IDamageable
//{
//    [Header("References")]
//    [SerializeField] protected Transform player;

//    protected bool enableNav = true;
//    protected NavMeshAgent agent;

//    protected bool canAttack = true;

//    [Header("References")]
//    [SerializeField] protected GameObject attackHitbox;
//    [SerializeField] private protected Health health;



//    private bool isActive;
//    private Rigidbody rb;

//    public void Activate(Transform playerTransform)
//    {
//        player = playerTransform;
//        isActive = true;
//        rb.isKinematic = false;
//    }
//    protected virtual void Awake()
//    {
//        agent = GetComponent<NavMeshAgent>();
//        rb = GetComponent<Rigidbody>();

//        // Initialize health component
//        if (health == null)
//        {
//            health = GetComponent<Health>();
//            if (health == null)
//            {
//                Debug.LogWarning($"No Health component found on {gameObject.name}. Adding one automatically.");
//                health = gameObject.AddComponent<Health>();

//                //// Add Healthbar if missing (required by Health)
//                //if (GetComponent<Healthbar>() == null)
//                //{
//                //    gameObject.AddComponent<Healthbar>();
//                //}
//            }
//        }

//        if (player == null)
//        {
//            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
//            if (playerObject != null)
//            {
//                player = playerObject.transform;
//                Debug.Log($"Found player: {player.name}");
//            }
//            else
//            {
//                Debug.LogError("No GameObject with 'Player' tag found!");
//            }
//        }
//          enableNav = true;
//    }

//    protected virtual void Update()
//    {
//        if (health != null && health.IsDead) return;
//    }

//    #region Health
//    void IDamageable.TakeDmg(float dmg)
//    {
//        if (health != null)
//            health.TakeDmg(dmg);
//    }
//    #endregion

//    #region Navigation

//    public virtual void MoveTo(Vector3 destination)
//    {
//        if (!enableNav) return;

//        agent.isStopped = false;
//        agent.SetDestination(destination);

//    }
//    public virtual void StopMovement()
//    {
//        if (!enableNav) return;
//        agent.isStopped = true;
//    }
//    public virtual void ResumeMovement()
//    {
//        if (!enableNav) return;
//        agent.isStopped = false;
//    }
//    #endregion

//}
