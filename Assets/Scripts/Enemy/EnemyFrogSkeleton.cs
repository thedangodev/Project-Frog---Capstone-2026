using System.Collections;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(EnemyAttack))]
public class EnemyFrogSkeleton : EnemyBase
{
    [Header("Attack config")]
    [SerializeField] private float attackRange = 1f;

    [Header("Post-attack movement")]
    [Tooltip("Distance to back away after performing an attack.")]
    [SerializeField] private float postAttackRetreatDistance = 2.5f;
    [Tooltip("Maximum seconds to wait for the attack to finish before backing away.")]
    [SerializeField] private float postAttackWaitTimeout = 2f;
    [Tooltip("Time to allow for the retreat movement before picking a new target.")]
    [SerializeField] private float postAttackRetreatTime = 1.2f;

    [Header("FMod Events")]
    [SerializeField] private EventReference regularShotDamageEvent;
    [SerializeField] private EventReference chargedShotDamageEvent;

    // Small tolerance to handle floating point / very-close cases
    [SerializeField, Tooltip("Extra tolerance (meters) added to attack range checks to avoid missing attacks when extremely close.")]
    private float attackRangeLeeway = 0.15f;

    private EnemyAttack enemyAttack;
    private Animator animator;
    private bool isHandlingPostAttack = false;

    protected override void Awake()
    {
        base.Awake();
        enemyAttack = GetComponent<EnemyAttack>();
        animator = GetComponent<Animator>();
        // or GetComponentInChildren<Animator>() if the Animator sits on a child mesh
    }

    protected override void Update()
    {
        base.Update();

        if (player == null)
        {
            //Debug.Log("player missing");
            return;
        }

        // If we're actively running the post-attack routine, don't run normal movement logic
        if (isHandlingPostAttack) return;

        if (enemyAttack.IsAttacking) return;

        // Use horizontal distance only so small Y offsets (ground/step) don't block the attack.
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float horizontalDist = toPlayer.magnitude;

        // Treat as "in range" when within attackRange + leeway.
        if (horizontalDist <= attackRange + attackRangeLeeway)
        {
            // Ensure the enemy is stopped and facing the player before attempting to attack.
            StopMovement();
            FacePlayerInstant();

            if (enemyAttack != null && enemyAttack.CanAttack)
            {
                // Temporarily disable movement component to avoid other movement code stepping in.
                movement.SetMovementEnabled(false);

                enemyAttack.TriggerAttack();

                if (!isHandlingPostAttack)
                    StartCoroutine(HandlePostAttack());
            }
            return;
        }
        else
        {
            // Not close enough — continue normal slot-based approach behaviour.
            movement.MoveToTarget(movement.Target.position);
        }
    }

    // Called by a player projectile (via Projectile.OnTriggerEnter) when this enemy is hit.
    // Plays the matching reaction animation. Stagger = regular shot, KnockbackReact = charged shot.
    public override void PlayHitReaction(HitReaction reaction)
    {
        if (animator == null) return;

        switch (reaction)
        {
            case HitReaction.Stagger:
                animator.SetTrigger("Stagger");
                RuntimeManager.PlayOneShot(regularShotDamageEvent, transform.position);
                break;
            case HitReaction.Knockback:
                animator.SetTrigger("KnockbackReact");
                RuntimeManager.PlayOneShot(chargedShotDamageEvent, transform.position);
                break;
        }
    }

    // Instantly rotate to face the player (Y axis only)
    private void FacePlayerInstant()
    {
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(lookDir.normalized);
    }

    private IEnumerator HandlePostAttack()
    {
        isHandlingPostAttack = true;
        //Debug.Log($"[Frog] HandlePostAttack START on '{name}'", this);

        // Wait for the attack animation/logic to complete (or timeout)
        float waited = 0f;
        while (enemyAttack != null && enemyAttack.IsAttacking && waited < postAttackWaitTimeout)
        {
            if (Mathf.Approximately(waited, 0f))
                //Debug.Log($"[Frog] Waiting for attack to finish on '{name}'", this);
                waited += Time.deltaTime;
            yield return null;
        }
        //Debug.Log($"[Frog] Finished waiting (waited={waited:F2}) on '{name}'", this);

        if (player != null && movement != null)
        {
            // Keep movement disabled so MovementComponent.Update does not overwrite our retreat destination.
            // movement.SetMovementEnabled(false); // already disabled at attack time

            // Compute retreat target away from player and move there (MoveTo ignores the movement enabled flag)
            Vector3 away = (transform.position - player.position);
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
                away = -transform.forward;
            Vector3 retreatTarget = transform.position + away.normalized * postAttackRetreatDistance;

            //Debug.Log($"[Frog] Issuing retreat to {retreatTarget} on '{name}' (movement still disabled)", this);
            movement.MoveTo(retreatTarget);

            // Allow some time to retreat (stop early if close enough)
            float elapsed = 0f;
            while (elapsed < postAttackRetreatTime)
            {
                if (Vector3.Distance(transform.position, retreatTarget) < 0.6f)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Request a new slot while movement is still disabled (safe) then re-enable movement so the agent goes to the new slot.
            //Debug.Log($"[Frog] Requesting new slot for '{name}'", this);
            movement.RequestSlot();
            movement.SetMovementEnabled(true);
        }

        // Re-enable movement after post-attack behavior
        movement.SetMovementEnabled(true);

        //Debug.Log($"[Frog] HandlePostAttack END on '{name}'", this);
        isHandlingPostAttack = false;
        yield break;
    }
}