using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("How long after taking damage until the player can be damaged again (i-frames/cooldown).")]
    [SerializeField] private float knockbackRefresh = 4f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.5f;

    [Tooltip("How long the smooth push lasts (also locks input for this duration).")]
    [SerializeField] private float knockbackDuration = 0.15f; // renamed from knockbackTime

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private int flashCount = 2;

    [Header("Input Lock (Optional)")]
    [Tooltip("Drag the script/component that reads player inputs (movement/attack) here so it can be disabled during knockback.")]
    [SerializeField] private MonoBehaviour inputProviderToDisable;

    private Health playerHealth;

    private Renderer cachedRenderer;
    private Color originalColor;

    private CharacterController characterController;
    private Rigidbody rb;

    private bool isTouchingEnemy = false;
    private float nextAllowedDamageTime = 0f;

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
            TryApplyDamageAndEffects();
        }
    }

    private void ContinueDamage()
    {
        if (isTouchingEnemy)
            TryApplyDamageAndEffects();
    }

    private void StopDamage()
    {
        isTouchingEnemy = false;
    }

    private void TryApplyDamageAndEffects()
    {
        // Knockback Refresh gate: prevents taking damage again until refresh time passes.
        if (Time.time < nextAllowedDamageTime)
            return;

        nextAllowedDamageTime = Time.time + Mathf.Max(0f, knockbackRefresh);

        playerHealth.TakeDmg(damageAmount);

        // Default knock direction: opposite facing
        Vector3 knockDir = -transform.forward;

        StartKnockback(knockDir, knockbackDistance, knockbackDuration);
        StartFlash();
    }

    // -------------------------
    //  KNOCKBACK (SMOOTH + INPUT LOCK)
    // -------------------------
    private void StartKnockback(Vector3 direction, float distance, float duration)
    {
        if (knockbackCo != null) StopCoroutine(knockbackCo);
        knockbackCo = StartCoroutine(KnockbackRoutine(direction.normalized, distance, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 dir, float distance, float duration)
    {
        SetInputLocked(true);

        float t = Mathf.Max(0.01f, duration);

        // Rigidbody: smooth MovePosition for consistent "push"
        if (rb != null && !rb.isKinematic)
        {
            Vector3 start = rb.position;
            Vector3 target = start + dir * distance;

            float elapsed = 0f;
            while (elapsed < t)
            {
                float alpha = elapsed / t;
                rb.MovePosition(Vector3.Lerp(start, target, alpha));

                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.MovePosition(target);
            SetInputLocked(false);
            yield break;
        }

        // CharacterController: smooth Move() over time
        if (characterController != null)
        {
            float elapsed = 0f;
            while (elapsed < t)
            {
                float step = Time.deltaTime / t;
                Vector3 delta = dir * (distance * step);
                characterController.Move(delta);

                elapsed += Time.deltaTime;
                yield return null;
            }

            SetInputLocked(false);
            yield break;
        }

        // Fallback: smooth transform move
        {
            Vector3 start = transform.position;
            Vector3 target = start + dir * distance;

            float elapsed = 0f;
            while (elapsed < t)
            {
                float alpha = elapsed / t;
                transform.position = Vector3.Lerp(start, target, alpha);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = target;
            SetInputLocked(false);
        }
    }

    private void SetInputLocked(bool locked)
    {
        // Disable the component that reads input during knockback.
        if (inputProviderToDisable != null)
            inputProviderToDisable.enabled = !locked;
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