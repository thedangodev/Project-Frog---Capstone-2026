using UnityEngine;
using FMODUnity;

// Use this script to create and inherit Scriptable Objects specifically for Ranged Enemy Behaviours. -E.M

[CreateAssetMenu(fileName = "RangedAttackSO", menuName = "Scriptable Objects/Attacks/RangedAttackSO")]
public class RangedAttackSO : AttackBaseSO
{
    [Header("Ranged Attack Stats")]
    [SerializeField] private float _range = 12f;
    [SerializeField] private string _attackName = "Ranged Attack";
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _cooldown = 1.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifetime = 4f;

    [Header("FMod Events")]
    [SerializeField] private EventReference spearHitEvent;

    // Abstract property implementations
    public override float range => _range;
    public override string attackName => _attackName;
    public override float damage => _damage;
    public override float cooldown => _cooldown;

    // Core attack logic
    protected override void PerformAttack(Transform target, Transform enemy)
    {
        if (projectilePrefab == null)
        {
            //Debug.LogWarning($"[{_attackName}] No projectile prefab assigned.");
            return;
        }

        // Aim direction: flat toward the target (zeroed Y keeps it horizontal).
        Vector3 direction = (target.position - enemy.position).normalized;

        // Spawn slightly in front of the enemy so it doesn't clip its own collider.
        Vector3 spawnPos = enemy.position + direction * 1.2f + Vector3.up * 1f;
        Quaternion spawnRot = Quaternion.LookRotation(direction);

        // Raycast from just in front of the spawn position toward the target to ensure we don't shoot another enemy.
        float distanceToTarget = Vector3.Distance(spawnPos, target.position);
        Vector3 rayOrigin = spawnPos + direction * 0.1f; // small offset to avoid hitting shooter's colliders

        // Debug visual: draw the ray so you can see it in the Scene/Game view (when Gizmos are on).
        Debug.DrawLine(rayOrigin, rayOrigin + direction * distanceToTarget, Color.red, 1f);

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, distanceToTarget))
        {
            // Prefer component check; fall back to tag check if component not found.
            var hitEnemy = hit.collider.GetComponentInParent<EnemyBase>();
            var shooterEnemy = enemy.GetComponentInParent<EnemyBase>();

            if (hitEnemy != null)
            {
                if (shooterEnemy == null || hitEnemy != shooterEnemy)
                {
                    //Debug.Log($"[{_attackName}] Aborting shot: another enemy ({hitEnemy.name}) in the way.");
                    return;
                }
            }
            else if (hit.collider.CompareTag("Enemy"))
            {
                // Ensure the hit transform is not the shooter (handles child colliders).
                if (hit.collider.transform != enemy && !hit.collider.transform.IsChildOf(enemy))
                {
                    //Debug.Log($"[{_attackName}] Aborting shot: an object tagged 'Enemy' is in the way.");
                    return;
                }
            }
        }

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, spawnRot);

        // Drive the projectile via Rigidbody if one exists.
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }

        // Pass damage to the projectile so it knows how hard to hit.
        IProjectile proj = projectile.GetComponent<IProjectile>();
        if (proj != null)
        {
            proj.Init(damage, projectileLifetime);
        }

        // Safety net: destroy the projectile after its lifetime.
        Destroy(projectile, projectileLifetime);
        RuntimeManager.PlayOneShot(spearHitEvent, projectile.transform.position);
    }
}
