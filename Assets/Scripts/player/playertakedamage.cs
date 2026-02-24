using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageInterval = 4f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.5f; // meters
    [SerializeField] private float knockbackTime = 0.05f;    // seconds (for smooth CC move)

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private int flashCount = 2;

    private Health playerHealth;

    private Renderer cachedRenderer;
    private Color originalColor;

    private CharacterController characterController;
    private Rigidbody rb;

    private bool isTouchingEnemy = false;
    private float nextDamageTime = 0f;

    private Coroutine flashCo;
    private Coroutine knockbackCo;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();

        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null)
            originalColor = cachedRenderer.material.color;
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
            ApplyDamageAndEffects();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void ContinueDamage()
    {
        if (isTouchingEnemy && Time.time >= nextDamageTime)
        {
            ApplyDamageAndEffects();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void StopDamage()
    {
        isTouchingEnemy = false;
    }

    private void ApplyDamageAndEffects()
    {
        playerHealth.TakeDmg(damageAmount);

        // Knockback opposite of facing (same behavior you had before)
        Vector3 knockDir = -transform.forward;

        // If you'd rather push away from the enemy/hitbox, we can change this later.
        StartKnockback(knockDir, knockbackDistance);

        StartFlash();
    }

    // -------------------------
    //  KNOCKBACK (NO PlayerController)
    // -------------------------
    private void StartKnockback(Vector3 direction, float distance)
    {
        if (knockbackCo != null) StopCoroutine(knockbackCo);
        knockbackCo = StartCoroutine(KnockbackRoutine(direction.normalized, distance));
    }

    private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
    {
        // Rigidbody approach (physics)
        if (rb != null && !rb.isKinematic)
        {
            // We want roughly "distance" worth of shove.
            // Convert into a velocity change: v = d / t
            float t = Mathf.Max(0.01f, knockbackTime);
            Vector3 velChange = (dir * (distance / t));

            rb.AddForce(velChange, ForceMode.VelocityChange);
            yield break;
        }

        // CharacterController approach (kinematic)
        if (characterController != null)
        {
            float t = Mathf.Max(0.01f, knockbackTime);
            float elapsed = 0f;

            while (elapsed < t)
            {
                float step = Time.deltaTime / t;
                Vector3 delta = dir * (distance * step);
                characterController.Move(delta);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        // Fallback (not ideal, but guarantees it works)
        transform.position += dir * distance;
    }

    // -------------------------
    //  FLASH (STACK-SAFE)
    // -------------------------
    private void StartFlash()
    {
        if (cachedRenderer == null) return;

        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Note: accessing .material creates an instance; fine for a single player.
        for (int i = 0; i < flashCount; i++)
        {
            cachedRenderer.material.color = Color.red;
            yield return new WaitForSeconds(flashDuration);

            cachedRenderer.material.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }

    // -------------------------
    //  FILTERING LOGIC
    // -------------------------
    private bool IsValidDamageSource(Collider col)
    {
        if (col.CompareTag("EnemyHitbox") && col.enabled)
            return true;

        if (col.CompareTag("Enemy"))
        {
            Collider[] hitboxes = col.GetComponentsInChildren<Collider>(true);
            foreach (var hb in hitboxes)
            {
                if (hb.CompareTag("EnemyHitbox") && hb.enabled)
                    return false;
            }
            return true;
        }

        return false;
    }

    private bool IsEnemyRelated(Collider col)
    {
        return col.CompareTag("Enemy") || col.CompareTag("EnemyHitbox");
    }
}