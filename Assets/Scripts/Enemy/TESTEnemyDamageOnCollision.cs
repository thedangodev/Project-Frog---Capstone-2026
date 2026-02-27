using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TESTEnemyDamageOnCollision : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float knockbackDistance = 5f;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Enemy triggered with: " + other.name);
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        PlayerTakeDamage playerTakeDamage = other.GetComponentInParent<PlayerTakeDamage>();

        if (playerTakeDamage == null) return;

        Vector3 knockDirection = other.transform.position - transform.position;
        knockDirection.y = 0f;
        knockDirection = knockDirection.normalized;

        playerTakeDamage.TryApplyDamageAndKnockback(damageAmount, knockDirection, knockbackDistance);
    }
}