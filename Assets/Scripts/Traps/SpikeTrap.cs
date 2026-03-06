using UnityEngine;

/// <summary>
/// SpikeTrap: Attach to a trap parent object. The script finds a child GameObject tagged
/// (default) "trap damage" that should contain an isTrigger Collider and forwards trigger
/// events to this component. When a Player enters the trigger the player will be damaged
/// and knocked back.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    public enum TargetMode
    {
        Player,
        Enemy,
        Both
    }

    [Header("Damage")]
    public float damageAmount = 20f;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.25f;

    [Header("Targets")]
    [Tooltip("Choose whether the trap hurts the Player, Enemies, or Both.")]
    public TargetMode targetMode = TargetMode.Player;

    [Header("Trigger")]
    [Tooltip("Child object tag to use as the trigger that activates this spike trap.")]
    public string triggerTag = "trap damage";

    GameObject _triggerChild;

    void Start()
    {
        // Find first child tagged as the trap trigger (e.g. "trap damage")
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject != gameObject && t.gameObject.CompareTag(triggerTag))
            {
                _triggerChild = t.gameObject;
                break;
            }
        }

        if (_triggerChild == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] No child with tag \"{triggerTag}\" found under {name}.");
            return;
        }

        var col = _triggerChild.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Child tagged \"{triggerTag}\" on {_triggerChild.name} has no Collider.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Collider on {_triggerChild.name} is not marked as isTrigger. Mark it as trigger for trap activation.");
        }

        // Add or get forwarding component so the child's trigger events are forwarded here
        var forwarder = _triggerChild.GetComponent<SpikeTrapTriggerForwarder>();
        if (forwarder == null)
        {
            forwarder = _triggerChild.AddComponent<SpikeTrapTriggerForwarder>();
        }

        forwarder.parent = this;
    }

    // Called by the trigger forwarder when something enters the child trigger
    internal void OnChildTriggerEnter(Collider other)
    {
        // If trap should damage player (or both), check for player
        if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                HandlePlayerHit(other);
            }
        }

        // If trap should damage enemies (or both), check for enemy
        if (targetMode == TargetMode.Enemy || targetMode == TargetMode.Both)
        {
            // Prefer EnemyBase component, otherwise try IDamageable or EnemyHealth, or tag "Enemy"
            if (other.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                HandleEnemyHit(other, enemyBase);
            }
            else
            {
                // Try parent objects as enemies are often on parents
                var parentEnemyBase = other.GetComponentInParent<EnemyBase>();
                if (parentEnemyBase != null)
                {
                    HandleEnemyHit(other, parentEnemyBase);
                }
                else
                {
                    // fallback: if object has IDamageable or EnemyHealth, treat as enemy
                    if (other.TryGetComponent<IDamageable>(out var dmgable))
                    {
                        ApplyDamageToIDamageable(dmgable);
                        ApplyKnockbackToCollider(other);
                    }
                    else
                    {
                        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.TakeDamage(damageAmount);
                            ApplyKnockbackToCollider(other);
                        }
                        else if (other.gameObject.CompareTag("Enemy"))
                        {
                            // Last resort: try to damage via any Health component on the enemy root
                            var fallback = other.GetComponentInParent<Health>();
                            if (fallback != null)
                            {
                                fallback.TakeDmg(damageAmount);
                                ApplyKnockbackToCollider(other);
                            }
                        }
                    }
                }
            }
        }
    }

    // Reattached: damage the player via PlayerMovement root instead of TopDownControllerWithDash.
    void HandlePlayerHit(Collider other)
    {
        // Prefer PlayerMovement on the player root
        var playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Player does not have a PlayerMovement component.");
            return;
        }

        // Try to find a Health component on the same root as PlayerMovement first
        var health = playerMovement.GetComponent<Health>() ?? other.GetComponentInParent<Health>();
        if (health != null)
        {
            health.TakeDmg(damageAmount);
        }
        else
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Player has no Health component to take damage.");
        }

        // Compute knockback direction (away from trap center, slightly upward)
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.2f); // give a little upward lift
        Vector3 knockback = dir * knockbackForce;

        // Apply knockback: prefer Rigidbody on player root. If kinematic, move it directly.
        var rb = playerMovement.GetComponent<Rigidbody>() ?? other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            if (rb.isKinematic)
            {
                // Move the kinematic rigidbody by a single displacement to simulate knockback.
                rb.MovePosition(rb.position + knockback);
            }
            else
            {
                rb.AddForce(knockback, ForceMode.Impulse);
            }
        }
        else
        {
            // Fallback: nudge root transform (last resort)
            other.transform.root.position += knockback;
        }

        // Optionally stop player movement briefly using public API if available
        // PlayerMovement provides StopMovement/ResumeMovement; call if you want to momentarily disable input.
        // playerMovement.StopMovement();
        // ... schedule ResumeMovement after knockbackDuration if desired.
    }

    void HandleEnemyHit(Collider other, EnemyBase enemyBase)
    {
        // Try to apply damage via IDamageable if available
        if (enemyBase is IDamageable dmgable)
        {
            dmgable.TakeDmg(damageAmount);
        }
        else
        {
            // Fallback to EnemyHealth if present
            var enemyHealth = enemyBase.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(damageAmount);
            else
            {
                // Last resort: try any Health on enemy root
                var fallback = enemyBase.GetComponentInParent<Health>();
                if (fallback != null)
                    fallback.TakeDmg(damageAmount);
            }
        }

        ApplyKnockbackToCollider(other);
    }

    void ApplyDamageToIDamageable(IDamageable dmgable)
    {
        dmgable.TakeDmg(damageAmount);
    }

    void ApplyKnockbackToCollider(Collider other)
    {
        // Compute knockback direction (away from trap center)
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.1f);
        Vector3 knockback = dir * knockbackForce;

        var rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            if (rb.isKinematic)
            {
                rb.MovePosition(rb.position + knockback);
            }
            else
            {
                rb.AddForce(knockback, ForceMode.Impulse);
            }
        }
        else
        {
            other.transform.root.position += knockback;
        }
    }

    void OnDisable()
    {
        // no-op; ensure any coroutines in player are unaffected
    }
}

/// <summary>
/// Lightweight forwarder put on the child trigger object that calls back into the parent SpikeTrap.
/// </summary>
public class SpikeTrapTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public SpikeTrap parent;

    void OnTriggerEnter(Collider other)
    {
        parent?.OnChildTriggerEnter(other);
    }
}
