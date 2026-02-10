using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerTakeDamage : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageInterval = 4f;

    private Health playerHealth;
    private bool isTouchingEnemy = false;
    private float nextDamageTime = 0f;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
    }

    // -------------------------
    //  TRIGGER SUPPORT
    // -------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (IsValidDamageSource(other))
            StartDamage();
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsValidDamageSource(other))
            ContinueDamage();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsEnemyRelated(other))
            StopDamage();
    }

    // -------------------------
    //  COLLISION SUPPORT
    // -------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (IsValidDamageSource(collision.collider))
            StartDamage();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsValidDamageSource(collision.collider))
            ContinueDamage();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsEnemyRelated(collision.collider))
            StopDamage();
    }

    // -------------------------
    //  DAMAGE LOGIC
    // -------------------------
    private void StartDamage()
    {
        if (!isTouchingEnemy)
        {
            isTouchingEnemy = true;
            playerHealth.TakeDmg(damageAmount);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void ContinueDamage()
    {
        if (isTouchingEnemy && Time.time >= nextDamageTime)
        {
            playerHealth.TakeDmg(damageAmount);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void StopDamage()
    {
        isTouchingEnemy = false;
    }

    // -------------------------
    //  FILTERING LOGIC
    // -------------------------
    private bool IsValidDamageSource(Collider col)
    {
        // Case 1: Direct hitbox
        if (col.CompareTag("EnemyHitbox") && col.enabled)
            return true;

        // Case 2: Enemy root, but ONLY if it has NO hitbox children
        if (col.CompareTag("Enemy"))
        {
            // Check for any active hitbox in children
            Collider[] hitboxes = col.GetComponentsInChildren<Collider>(true);
            foreach (var hb in hitboxes)
            {
                if (hb.CompareTag("EnemyHitbox") && hb.enabled)
                    return false; // hitbox exists → ignore root
            }

            return true; // no hitbox found → root damage allowed
        }

        return false;
    }

    private bool IsEnemyRelated(Collider col)
    {
        return col.CompareTag("Enemy") || col.CompareTag("EnemyHitbox");
    }
}
