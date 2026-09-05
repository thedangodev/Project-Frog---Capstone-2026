using System.Collections;
using UnityEngine;
using FMODUnity;

public class EnemyRockGolem : EnemyBase
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;

    private EnemyAttack enemyAttack;

    [Header("Line of Sight")]
    [Tooltip("Height above enemy position used as ray origin for LOS checks.")]
    [SerializeField] private float eyeHeight = 1.0f;
    [Tooltip("Height above player position used as ray target for LOS checks.")]
    [SerializeField] private float targetEyeHeight = 1.0f;

    [Header("FMod Events")]
    [SerializeField] private EventReference golemAttackEvent;
    [SerializeField] private EventReference voicedGolemAttackEvent;

    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        enemyAttack = GetComponent<EnemyAttack>();
        animator = GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update();

        if (player == null) return;

        if (enemyAttack.IsAttacking)
        {
            StopMovement();
            return;
        }

        HandleBehaviour();
    }

    private void HandleBehaviour() //if outside of range, chase the player, otherwise attack the player
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            AttackPlayer();
        }
    }

    #region Behaviours
    protected void ChasePlayer()
    {
        movement.MoveToTarget(player.position);  
    }

    protected void AttackPlayer()
    {
        // If player is behind an obstacle, move toward them to regain LOS instead of staying still.
        if (!HasLineOfSight(player))
        {
            ChasePlayer();
            return;
        }

        StopMovement();

        if (enemyAttack.CanAttack)
        {
            //Debug.Log("[Golem] Calling TriggerAttack");
            enemyAttack.TriggerAttack(player.position);

            RuntimeManager.PlayOneShot(golemAttackEvent, transform.position);
            RuntimeManager.PlayOneShot(voicedGolemAttackEvent, transform.position);
        }
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
        }
    }
    #endregion

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
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player")) return true;
                if (target != null && (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))) return true;
            }
            return false;
        }

        return true;
    }
}