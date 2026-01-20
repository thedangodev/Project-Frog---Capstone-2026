using UnityEngine;
using UnityEngine.AI;
 
  // This script is specifically for the Corrupted Swamp Elder - Enemy will chase the Player Transform or object tagged as Player until in range, and will execute a Melee attack. 
 // Simple Raycast system has been built to visually show status between Chase/Attack behaviours. (Yellow = Chasing/Not in range, Red = Attacking Player.)
// Swamp Elder Script by Eric M.

[DisallowMultipleComponent]                 // Prevents adding multiple of this script to the same GameObject
[RequireComponent(typeof(Collider))]       
[RequireComponent(typeof(NavMeshAgent))]  // Ensures the enemy has a NavMeshAgent component
class GenericEnemy : MonoBehaviour
{
    // Enemy Targets

    [Header("Targets")]
    [SerializeField] private Transform player;             // Reference to the Player's Transform location.
    [SerializeField] private string playerTag = "Player"; // Tag used to find the player automatically if not assigned by User.

    // Enemy Chase Settings

    [Header("Chase Settings")]
    [SerializeField] private float moveSpeed = 3.5f;               // Enemy's movement speed via NavMeshAgent.
    [SerializeField] private float stopDistance = 0.6f;           // Distance at which the enemy stops moving toward the Player.
    [SerializeField] private bool faceTargetWhileChasing = true; // Enemy rotates to face the player while chasing the Player.


    // Line of Sight fields

    [Header("Line of Sight")]
    [SerializeField] private float viewDistance = 12f;            // How far the enemy can "see" the Player.
    [SerializeField] private float viewHalfAngle = 55f;          // Half of the FOV cone angle (left/right from forward).
    [SerializeField] private LayerMask obstructionLayers = ~0;  // Which layers the raycast can collide with (includes player/walls/etc).
    [SerializeField] private float visionRefresh = 0.1f;       // How often LOS updates (optimization).
    [SerializeField] private Transform eyes;                  // Optional eye transform for raycast origin.
    [SerializeField] private float eyeHeight = 1.6f;         // Fallback eye height if no "eyes" transform exists.


    // Enemy Attack Raycast, Test/Debugging feature
 

    [Header("Attack Ray (Debug/Visual)")]
    [SerializeField] private float attackRange = 1.25f; // Distance at which enemy is "attacking" the Player.

    // Controls how the attack ray is shown to the User visually.
    private enum AttackLineMode
    {
        AlwaysVisibleChangesColor,      // Ray is always visible; changes color when attacking.
        OnlyVisibleWhenAttacking       // Ray only appears once the enemy is attacking.
    }

    [SerializeField] private AttackLineMode showAttackLineMode = AttackLineMode.AlwaysVisibleChangesColor;

    [Tooltip("Optional: assign a LineRenderer to show the attack ray in the Game view (and builds).")]
    [SerializeField] private LineRenderer attackLine; // The visual line displayed in the world.

    [SerializeField] private Color idleLineColor = Color.yellow;     
    [SerializeField] private Color attackingLineColor = Color.red;   

    [Tooltip("Small vertical offset so the line doesn't clip the floor.")]
    [SerializeField] private float attackLineHeightOffset = 0.15f;

    // Enemy Clump Prevention (If more than one)

    [Header("Anti-Clump")]
    [SerializeField] private bool useSeparation = true;         // Enables pushing away from other enemies
    [SerializeField] private float separationRadius = 1.0f;    // How far to check for nearby enemies
    [SerializeField] private float separationStrength = 1.0f; // How strong the push-away offset is
    [SerializeField] private LayerMask enemyLayers = ~0;     // Which layers count as "enemies" for separation

    // Movement Backend

    [Header("Movement Backend")]
    [SerializeField] private bool useNavMeshAgent = true; // If false, the enemy won't chase using NavMeshAgent.

    // Enemy Death Anims

    [Header("Death / Animation")]
    [SerializeField] private Animator animator;                 // Animator used for death animation.
    [SerializeField] private string deathTriggerName = "Die";  // Trigger parameter name for death.
    [SerializeField] private string deadBoolName = "IsDead";  // Bool parameter name for death state.

    // Hit Detection

    [Header("Hit Detection")]
    [Tooltip("Tag used to identify projectiles that can kill this enemy.")]
    [SerializeField] private string projectileTag = "Projectile";  // Tag for player projectiles.
    [SerializeField] private bool disableColliderOnDeath = true;  // If true, disables collider when dead.

    // Component References

    private NavMeshAgent agent;     // Controls pathfinding + movement.
    private Rigidbody rb;          // Present for stability (kinematic).
    private Collider col;         // Used for trigger/hit detection.

    // State Variables

    private bool isDead = false;       
    private bool hasLOS = false;       
    private float nextVisionTime = 0f; // Controls how often LOS updates

    private bool isAttacking = false;  // True if player is within attack range (ray test).

    // Unity Lifecycle

    private void Awake()
    {
        // Grab and configure collider.
        col = GetComponent<Collider>();
        col.isTrigger = true; // Trigger collider for simple hit detection (no physics collisions).

        // Ensure Rigidbody is present (kinematic so it won't fight the NavMeshAgent).
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Setup NavMeshAgent movement behaviour.
        agent = GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.updateRotation = false;  // Manually rotate for more control.
            ApplyAgentSettings(force: true);

            // Makes movement more responsive.
            agent.acceleration = 50f;
            agent.angularSpeed = 720f;
            agent.autoBraking = false;
        }

        // Auto-find animator if not assigned.
        if (!animator) animator = GetComponentInChildren<Animator>();

        // Setup attack line renderer if it exists.
        if (!attackLine) attackLine = GetComponent<LineRenderer>();
        if (attackLine)
        {
            attackLine.positionCount = 2;

            if (showAttackLineMode == AttackLineMode.OnlyVisibleWhenAttacking)
                attackLine.enabled = false;

            ApplyLineColor(idleLineColor);
        }
    }

    private void Start()
    {
        // Auto-find player transform if not manually assigned.
        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
    }

    private void Update()
    {
        // Stop all behaviour if dead or missing player reference.
        if (isDead || !player) return;

        // Refresh line-of-sight check on a timer (optimization).
        if (Time.time >= nextVisionTime)
        {
            hasLOS = ComputeHasLOS();
            nextVisionTime = Time.time + visionRefresh;
        }

        // Update attack ray visuals/logic.
        UpdateAttackRay();

        // If NavMesh movement is disabled, stop here (still allows ray visuals to run).
        if (!useNavMeshAgent || agent == null) return;

        // Update agent speed/stopping distance live in play mode if tweaked in inspector.
        ApplyAgentSettings(force: false);

        // Direction from enemy to player (flat / no vertical influence).
        Vector3 toPlayer = player.position - transform.position;
        Vector3 toPlayerFlat = new Vector3(toPlayer.x, 0f, toPlayer.z);

        // Rotate to face the player while chasing if enabled.
        if (hasLOS && faceTargetWhileChasing)
            FaceDirection(toPlayerFlat, Vector3.up);

        if (!hasLOS)
        {
            StopChasing();
            return;
        }

        // Stop chasing once within stop distance.
        float sqrDist = toPlayerFlat.sqrMagnitude;
        if (sqrDist <= stopDistance * stopDistance)
        {
            StopChasing();
            return;
        }

        // Set agent destination if on the NavMesh.
        if (agent.isOnNavMesh)
        {
            if (agent.isStopped) agent.isStopped = false;

            Vector3 dest = player.position;

            // Push away from other enemies to prevent clumping.
            if (useSeparation)
                dest += ComputeSeparationOffset();

            agent.SetDestination(dest);
        }
    }

    // Raycast Attack Visual (For testing)

    private void UpdateAttackRay()
    {
        isAttacking = false;

        // If no LOS, hide attack line and exit.
        if (!hasLOS || !player)
        {
            SetAttackLineVisible(false, idleLineColor);
            return;
        }

        // Determine ray origin point.
        Vector3 origin = eyes ? eyes.position : (transform.position + Vector3.up * eyeHeight);
        origin.y += attackLineHeightOffset;

        // Direction toward player.
        Vector3 target = player.position;
        Vector3 dir = target - origin;

        float distToPlayer = dir.magnitude;
        if (distToPlayer < 0.001f)
        {
            SetAttackLineVisible(false, idleLineColor);
            return;
        }

        Vector3 dirNorm = dir / distToPlayer;

        // Ray length is capped at attack range.
        float rayLen = Mathf.Min(attackRange, distToPlayer);

        // Raycast checks for player within range (blocked by walls/layers).
        if (Physics.Raycast(origin, dirNorm, out RaycastHit hit, rayLen, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag(playerTag) || hit.collider.transform.root.CompareTag(playerTag))
                isAttacking = true;
        }
        else
        {
            // If nothing was hit (no obstruction) and the player is within range.
            if (distToPlayer <= attackRange)
                isAttacking = true;
        }

        // Draw/Update attack line.
        if (attackLine)
        {
            Vector3 endPoint = origin + dirNorm * rayLen;

            attackLine.SetPosition(0, origin);
            attackLine.SetPosition(1, endPoint);

            // Always visible mode: show line, just switch colors.
            if (showAttackLineMode == AttackLineMode.AlwaysVisibleChangesColor)
            {
                attackLine.enabled = true;
                SetAttackLineVisible(true, isAttacking ? attackingLineColor : idleLineColor);
            }
            else
            {
                // Only visible mode: show only while attacking.
                SetAttackLineVisible(isAttacking, attackingLineColor);
            }
        }
        else
        {
            Debug.DrawRay(origin, dirNorm * rayLen, isAttacking ? attackingLineColor : idleLineColor);
        }
    }

    private void SetAttackLineVisible(bool visible, Color color)
    {
        if (!attackLine) return;

        // Always-visible mode forces the attack raycast to stay visible.
        if (showAttackLineMode == AttackLineMode.AlwaysVisibleChangesColor)
            visible = true;

        attackLine.enabled = visible;
        ApplyLineColor(color);
    }

    private void ApplyLineColor(Color c)
    {
        if (!attackLine) return;

        attackLine.startColor = c;
        attackLine.endColor = c;
    }

    // NavMesh Settings

    private void ApplyAgentSettings(bool force)
    {
        if (!agent) return;

        // Clamp values for safety.
        if (moveSpeed < 0f) moveSpeed = 0f;
        if (stopDistance < 0f) stopDistance = 0f;

        // Apply speed only when changed or forced.
        if (force || !Mathf.Approximately(agent.speed, moveSpeed))
            agent.speed = moveSpeed;

        // Apply stopping distance only when changed or forced.
        if (force || !Mathf.Approximately(agent.stoppingDistance, stopDistance))
            agent.stoppingDistance = stopDistance;
    }

    // Death/Damage System (No Player Attacks at this time)

    //private void Die()
    //{
    //    if (isDead) return;
    //    isDead = true;

    //    // Stop NavMesh movement
    //    if (agent)
    //    {
    //        agent.isStopped = true;
    //        agent.ResetPath();
    //        agent.updatePosition = false;
    //        agent.updateRotation = false;
    //    }

    //    // Freeze physics (still kinematic)
    //    rb.isKinematic = true;

    //    // Disable collider so it doesn't interfere after death
    //    if (disableColliderOnDeath && col) col.enabled = false;

    //    // Hide attack line on death
    //    if (attackLine) attackLine.enabled = false;

    //    // Trigger death animation parameters
    //    if (animator)
    //    {
    //        if (!string.IsNullOrEmpty(deadBoolName))
    //            animator.SetBool(deadBoolName, true);

    //        if (!string.IsNullOrEmpty(deathTriggerName))
    //            animator.SetTrigger(deathTriggerName);
    //    }
    //}

    //private void TakeHit(Collider projectile, Collision c)
    //{
    //    if (isDead) return;

    //    // Destroy the projectile that hit the enemy
    //    if (projectile && projectile.gameObject)
    //        Destroy(projectile.gameObject);

    //    // Kill enemy
    //    Die();
    //}

    private bool ComputeHasLOS()
    {
        if (!player) return false;

        // Ray origin from eyes or fallback height.
        Vector3 origin = eyes ? eyes.position : (transform.position + Vector3.up * eyeHeight);

        // Direction to player.
        Vector3 toTarget = player.position - origin;

        // Too far = no LOS.
        if (toTarget.magnitude > viewDistance) return false;

        // Field-of-view cone check (horizontal only).
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z).normalized;

        if (Vector3.Angle(flatForward, flatToTarget) > viewHalfAngle)
            return false;

        // Raycast checks for obstructions.
        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, toTarget.magnitude, obstructionLayers))
            return hit.collider.CompareTag(playerTag);

        return true;
    }


    // Anti-Clump System (if more than one chasing Player)

    private Vector3 ComputeSeparationOffset()
    {
        Vector3 push = Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius, enemyLayers);

        foreach (Collider h in hits)
        {
            if (h == col) continue;

            Vector3 diff = transform.position - h.bounds.center;
            diff.y = 0f;

            float d = diff.magnitude;
            if (d < 0.001f) continue;

            push += diff / (d * d);
        }

        if (push.sqrMagnitude > 0f)
            push = push.normalized * separationStrength;

        return push;
    }

    // Enemy Rotation/Stop Movement

    private void FaceDirection(Vector3 worldDir, Vector3 up)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude <= 0.01f) return;

        // Smoothly rotate toward Player's direction/transform.
        Quaternion targetRot = Quaternion.LookRotation(worldDir.normalized, up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
    }

    private void StopChasing()
    {
        if (agent && agent.isOnNavMesh)
        {
            if (!agent.isStopped) agent.isStopped = true;
            agent.ResetPath();
        }
    }
}