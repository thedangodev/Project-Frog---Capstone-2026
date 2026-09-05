using UnityEngine;
using FMODUnity;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;

// A ranged enemy that inherits from EnemyBase and delegates all attack behaviour to a pluggable AttackBaseSO ScriptableObject. This is for the Croc! -E.M
// Locomotion is driven by a "Speed" float the Animator blends on — the code never asks for idle or walk explicitly.


public class EnemyCroc : EnemyBase
{
    [Header("Engagement Distances")]
    [Tooltip("The ideal distance the Croc tries to maintain from the player.")]
    [SerializeField] private float preferredDistance = 10f;

    [Tooltip("How far inside or outside preferredDistance is still acceptable. " +
             "The Croc won't move if it's within preferredDistance ± tolerance.")]
    [SerializeField] private float distanceTolerance = 1.5f;

    [Tooltip("If the player gets closer than this, the Croc backs away.")]
    [SerializeField] private float retreatDistance = 5f;

    [Header("Attack (Scriptable Object)")]
    [Tooltip("Drag any AttackBaseSO asset here — ranged, AoE, etc.")]
    [SerializeField] private AttackBaseSO attackSO;

    [Header("Rotation")]
    [Tooltip("How quickly the Croc turns to face the player while attacking.")]
    [SerializeField] private float lookRotationSpeed = 8f;

    [Header("Line of Sight")]
    [Tooltip("Height above enemy position used as ray origin for LOS checks.")]
    [SerializeField] private float eyeHeight = 1.0f;
    [Tooltip("Height above player position used as ray target for LOS checks.")]
    [SerializeField] private float targetEyeHeight = 1.0f;

    [Header("FMod Events")]
    [SerializeField] private EventReference spearThrowEvent;
    [SerializeField] private EventReference spearThrowNarratedEvent;

    [Header("Animation")]
    [Tooltip("Animator driving the Croc. Animation Events must be authored on clips this Animator plays.")]
    [SerializeField] private Animator animator;

    [Tooltip("The spear mesh parented under the right hand bone.")]
    [SerializeField] private GameObject heldSpear;

    [Tooltip("Failsafe: if the restore event never fires, force a reset after this long.")]
    [SerializeField] private float maxThrowDuration = 3f;

    [Tooltip("If true, the Croc holds position and only turns while a throw is in progress.")]
    [SerializeField] private bool lockMovementDuringThrow = true;

    [Header("Locomotion Animation")]
    [Tooltip("How quickly the animator's Speed parameter catches up to actual movement.")]
    [SerializeField] private float speedSmoothing = 10f;

    [Tooltip("Speeds below this are snapped to zero so the Croc settles cleanly into idle.")]
    [SerializeField] private float speedDeadzone = 0.05f;

    private static readonly int ThrowTrigger = Animator.StringToHash("Throw");
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private bool throwInProgress;
    private float throwStartTime;

    private Vector3 lastPosition;
    private float smoothedSpeed;

    protected override void Awake()
    {
        base.Awake();

        if (attackSO != null)
        {
            // Create a runtime clone so we don't write to the shared asset
            attackSO = Instantiate(attackSO);
        }
        else
        {
            Debug.LogError($"[EnemyCroc] No AttackBaseSO assigned on {gameObject.name}.");
        }

        if (animator == null)
        {
            // Fall back to a child Animator so a missing inspector reference isn't silently fatal.
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                Debug.LogError($"[EnemyCroc] No Animator found on {gameObject.name}. The Croc will never throw.");
        }

        if (heldSpear == null)
            Debug.LogError($"[EnemyCroc] No heldSpear assigned on {gameObject.name}. The hand spear won't hide on throw.");

        lastPosition = transform.position;
    }

    protected override void Update()
    {
        // Let the base class handle checks and such.
        base.Update();

        // Report how fast we're actually moving so the Animator can blend idle <-> walk.
        UpdateLocomotionAnimation();

        // Failsafe: if the restore animation event never fired (clip interrupted, event missing, state machine transitioned early) recover so the Croc isn't stuck unarmed forever.
        if (throwInProgress && Time.time - throwStartTime > maxThrowDuration)
        {
            //Debug.LogWarning($"[EnemyCroc] Throw timed out on {gameObject.name} — restore event may be missing from the clip.");
            AnimEvent_RestoreSpear();
        }

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Evaluate LOS and range
        bool hasLos = HasLineOfSight(player);
        bool inSORange = attackSO != null ? distanceToPlayer <= attackSO.range : false;

        // While committed to a throw, hold the ground and just track the player.
        if (throwInProgress && lockMovementDuringThrow)
        {
            StopMovement();
            FaceTarget();
            return;
        }

        // If the player is too close, retreat regardless
        if (distanceToPlayer < retreatDistance)
        {
            Retreat();
            return;
        }

        // If the enemy cannot attack because of LOS or range -> move toward the player to regain range/LOS.
        if (!(hasLos && inSORange))
        {
            // Direct chase toward the player's current position so the croc will try to reach the player and regain LOS/range.
            movement.MoveTo(player.position);
            return;
        }

        // At this point the croc has LOS and is within the SO range.
        if (distanceToPlayer > preferredDistance + distanceTolerance)
        {
            Approach();
        }
        else
        {
            StopMovement();
            FaceTarget();
        }

        // Attempt attack (TryAttack still enforces cooldown via attackSO.CanAttack and LOS as safety)
        TryAttack();
    }

    private void OnEnable()
    {
        // Position tracking would otherwise report a huge delta on the first frame after a respawn or teleport, spiking the Croc into a walk animation.
        lastPosition = transform.position;
        smoothedSpeed = 0f;
    }

    private void OnDisable()
    {
        // Reset visual + logical state so a pooled or respawned Croc comes back armed and idle.
        SetHeldSpearVisible(true);
        throwInProgress = false;
        smoothedSpeed = 0f;

        if (animator != null)
        {
            animator.ResetTrigger(ThrowTrigger);
            animator.SetFloat(SpeedParam, 0f);
        }
    }

    // Measures real world-space movement and feeds it to the Animator's blend parameter.
    private void UpdateLocomotionAnimation()
    {
        if (animator == null || Time.deltaTime <= 0f) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f; // ignore vertical so falling/steps don't read as walking
        lastPosition = transform.position;

        float rawSpeed = delta.magnitude / Time.deltaTime;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, Time.deltaTime * speedSmoothing);

        if (smoothedSpeed < speedDeadzone)
            smoothedSpeed = 0f;

        animator.SetFloat(SpeedParam, smoothedSpeed);
    }

    // Walk toward the player, stopping once reaching the comfort zone.
    private void Approach()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 targetPosition = player.position - directionToPlayer * preferredDistance;
        movement.MoveToTarget(targetPosition);
    }

    // Back away from the player.
    private void Retreat()
    {
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 retreatTarget = player.position + directionAwayFromPlayer * preferredDistance;
        movement.MoveToTarget(retreatTarget);
    }

    // Smoothly rotate to face the player on the Y axis only (no tilting).
    private void FaceTarget()
    {
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y = 0f; // stay level
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * lookRotationSpeed
        );
    }

    // Requests the throw animation. The projectile itself is spawned by the animation event.
    private void TryAttack()
    {
        if (attackSO == null || animator == null) return;
        if (throwInProgress) return;
        if (!HasLineOfSight(player)) return;
        if (!attackSO.CanAttack(player, transform)) return;

        throwInProgress = true;
        throwStartTime = Time.time;
        animator.SetTrigger(ThrowTrigger);
    }

    // Animation Event — call at the release frame of the throw animation.
    public void AnimEvent_ReleaseSpear()
    {
        if (attackSO != null && player != null)
        {
            attackSO.Attack(player, transform);
            RuntimeManager.PlayOneShot(spearThrowEvent, transform.position);
            RuntimeManager.PlayOneShot(spearThrowNarratedEvent, transform.position);
        }

        SetHeldSpearVisible(false);
    }

    // Animation Event — call once the croc has "drawn" a new spear.
    public void AnimEvent_RestoreSpear()
    {
        // Clear any queued trigger so the Croc doesn't immediately re-throw if the trigger was set while no valid transition existed.
        if (animator != null)
            animator.ResetTrigger(ThrowTrigger);

        SetHeldSpearVisible(true);
        throwInProgress = false;
    }

    public override void PlayHitReaction(HitReaction reaction)
    {
        if (animator == null) return;

        switch (reaction)
        {
            case HitReaction.Stagger:
                animator.SetTrigger("Stagger");
                break;
            case HitReaction.Knockback:
                animator.SetTrigger("Knockback");
                break;
            default:
                break;
        }
    }

    private void SetHeldSpearVisible(bool visible)
    {
        if (heldSpear != null && heldSpear.activeSelf != visible)
            heldSpear.SetActive(visible);
    }

    // Returns true when an unobstructed ray reaches the player (player tag or player's transforms).
    private bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 dest = target.position + Vector3.up * targetEyeHeight;
        Vector3 dir = dest - origin;
        float dist = dir.magnitude;
        if (dist < 0.001f) return true;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            // Consider LOS valid if the ray hit the player (or a child of the player)
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player")) return true;
                if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) return true;
            }
            return false;
        }

        // Nothing hit — assume clear LOS
        return true;
    }
}