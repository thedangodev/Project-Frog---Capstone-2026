using UnityEngine;

/// <summary>
/// SpikeTrap: Attach to a trap parent object. The script finds a child GameObject tagged
/// (default) "trap damage" that should contain an isTrigger Collider and forwards trigger
/// events to this component. When a Player enters the trigger the player will be damaged
/// and knocked back.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    [Header("Damage")]
    public float damageAmount = 20f;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.25f;

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
        if (!other.gameObject.CompareTag("Player")) return;

        var playerController = other.GetComponentInParent<TopDownControllerWithDash>();
        if (playerController == null)
        {
            Debug.LogWarning($"[{nameof(SpikeTrap)}] Player does not have a TopDownControllerWithDash component.");
            return;
        }

        // Apply damage using the player's Health component if available
        if (playerController.health != null)
        {
            playerController.health.TakeDmg(damageAmount);
        }
        else
        {
            var fallbackHealth = other.GetComponentInParent<Health>();
            if (fallbackHealth != null)
                fallbackHealth.TakeDmg(damageAmount);
            else
                Debug.LogWarning($"[{nameof(SpikeTrap)}] Player has no Health component to take damage.");
        }

        // Compute knockback direction (away from trap center, slightly upward)
        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = Mathf.Max(dir.y, 0.2f); // give a little upward lift
        Vector3 knockback = dir * knockbackForce;

        // Apply knockback: prefer Rigidbody on player root. If kinematic, move it directly.
        var rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            if (rb.isKinematic)
            {
                // Move the kinematic rigidbody by a single displacement to simulate knockback.
                // Using MovePosition keeps physics consistent with how the player is moved elsewhere.
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

        // Note: knockbackDuration is not used here because TopDownControllerWithDash does not expose
        // an ApplyKnockback method. If you want a time-based knockback effect, add a public method
        // to TopDownControllerWithDash to accept a force and duration and call it here.
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
