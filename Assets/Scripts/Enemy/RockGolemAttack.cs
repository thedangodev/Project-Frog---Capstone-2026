using TMPro;
using UnityEngine;

public class RockGolemAttack : EnemyAttack
{
    [Header("Projectile")]
    [SerializeField] private GameObject burrowProjectilePrefab;
    [SerializeField] private EnemyBurrowAttackDataSO attackData;

    private Vector3 pendingTargetPosition;
    private Transform player;

    private Animator animator;
    private static readonly int AttackingHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        GameObject playerObject = GameObject.FindWithTag("Player");
        player = playerObject.transform;
    }

    protected override void OnExecuteAttack(Vector3 targetPosition)
    {
        if (burrowProjectilePrefab == null)
        {
            //Debug.LogError($"{name}: burrowProjectilePrefab not assigned.", this);
            return;
        }
        if (attackData == null)
        {
            //Debug.LogError($"{name}: attackData (BurrowAttackDataSO) not assigned.", this);
            return;
        }

        pendingTargetPosition = targetPosition;
        IsAttacking = true;
        animator.SetTrigger(AttackingHash);
    }

    private Vector3 GetGroundPosition(Vector3 rawPos)
    {
        Vector3 rayStart = rawPos + Vector3.up * 10f;
        if (attackData != null && attackData.groundLayer != 0 &&
            Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, attackData.groundLayer))
        {
            return hit.point;
        }
        return rawPos;
    }

    public void AnimEvent_Attack()
    {
        if (burrowProjectilePrefab == null || attackData == null) return;

        Vector3 origin = GetGroundPosition(transform.position);

        Vector3 targetPosition = player != null ? player.position : pendingTargetPosition;

        GameObject go = Instantiate(burrowProjectilePrefab, origin, Quaternion.identity);

        if (!go.TryGetComponent<EnemyBurrowProjectile>(out var projectile))
        {
            //Debug.LogError($"{name}: prefab missing BurrowProjectile component.", this);
            Destroy(go);
            return;
        }

        // targetPosition = player's last-known position at initiation; not re-tracked.
        projectile.Initialize(origin, targetPosition, attackData);
    }

    public void AnimEvent_AttackEnd()
    {
        IsAttacking = false;
    }
}