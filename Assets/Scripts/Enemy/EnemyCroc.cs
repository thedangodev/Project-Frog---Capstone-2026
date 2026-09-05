using UnityEngine;
using FMODUnity;

//Summary: A ranged enemy that inherits from EnemyBase and delegates all attack behaviour to a pluggable AttackBaseSO ScriptableObject. This is for the Croc! -E.M


public class EnemyCroc : EnemyBase
{
    [Header("Engagement Distances")]
    [Tooltip("The ideal distance the Axolotl tries to maintain from the player.")]
    [SerializeField] private float preferredDistance = 10f;

    [Tooltip("How far inside or outside preferredDistance is still acceptable. " +
             "The Axolotl won't move if it's within preferredDistance ± tolerance.")]
    [SerializeField] private float distanceTolerance = 1.5f;

    [Tooltip("If the player gets closer than this, the Axolotl backs away.")]
    [SerializeField] private float retreatDistance = 5f;

    [Header("Attack (Scriptable Object)")]
    [Tooltip("Drag any AttackBaseSO asset here — ranged, AoE, etc.")]
    [SerializeField] private AttackBaseSO attackSO;

    [Header("Rotation")]
    [Tooltip("How quickly the Axolotl turns to face the player while attacking.")]
    [SerializeField] private float lookRotationSpeed = 8f;

    [Header("Line of Sight")]
    [Tooltip("Height above enemy position used as ray origin for LOS checks.")]
    [SerializeField] private float eyeHeight = 1.0f;
    [Tooltip("Height above player position used as ray target for LOS checks.")]
    [SerializeField] private float targetEyeHeight = 1.0f;

    [Header("FMod Events")]
    [SerializeField] private EventReference spearThrowEvent;
    [SerializeField] private EventReference spearThrowNarratedEvent;

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
            Debug.LogError($"[EnemyAxolotl] No AttackBaseSO assigned on {gameObject.name}.");
        }
    }

    protected override void Update()
    {
        // Let the base class handle checks and such
        base.Update();

        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Evaluate LOS and range
        bool hasLos = HasLineOfSight(player);
        bool inSORange = attackSO != null ? Vector3.Distance(transform.position, player.position) <= attackSO.range : false;

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
        // Maintain preferred distance behavior as before.
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

    // Delegates entirely to the ScriptableObject. 
    private void TryAttack()
    {
        if (attackSO == null) return;

        // Safety: require LOS as well before firing
        if (!HasLineOfSight(player)) return;

        if (attackSO.CanAttack(player, transform))
        {
            attackSO.Attack(player, transform);

            RuntimeManager.PlayOneShot(spearThrowEvent, transform.position);
            RuntimeManager.PlayOneShot(spearThrowNarratedEvent, transform.position);
        }
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
