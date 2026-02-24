using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class EnemyDamageHitbox : MonoBehaviour
{
    [SerializeField] private float damageAmount = 25f;

    private Collider hitboxCollider;

    // Tracks who was already damaged during this activation
    private readonly HashSet<GameObject> damagedThisEntry = new();

    private bool lastColliderState;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        lastColliderState = hitboxCollider.enabled;
    }

    private void Update()
    {
        // If collider just got disabled → reset hit memory
        if (lastColliderState && !hitboxCollider.enabled)
        {
            damagedThisEntry.Clear();
        }

        lastColliderState = hitboxCollider.enabled;
    }

    private void OnTriggerStay(Collider other)
    {
        GameObject root = other.transform.root.gameObject;

        if (!root.CompareTag("Player")) return;

        ApplyDamage(root);
    }

    private void ApplyDamage(GameObject target)
    {
        if (damagedThisEntry.Contains(target)) return;

        Health health = target.GetComponentInChildren<Health>();
        if (health != null && !health.IsDead)
        {
            health.TakeDmg(damageAmount);
            damagedThisEntry.Add(target);
            Debug.Log($"{target.name} took {damageAmount} damage.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject root = other.transform.root.gameObject;

        if (damagedThisEntry.Contains(root))
        {
            damagedThisEntry.Remove(root);
        }
    }
}