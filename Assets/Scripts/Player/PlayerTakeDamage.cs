using System.Collections;
using UnityEngine;

/// <summary>
/// Handles taking damage, i-frames, knockback, and flashing visuals for the player.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerImmortality))]
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("How long after taking damage until the player can be damaged again (i-frames/cooldown).")]
    [SerializeField] private float immortalityTime = 1f;

    [Tooltip("Knockback speed in meters per second.")]
    [SerializeField] private float knockbackSpeed = 20f;

    [Tooltip("Power of the knockback ease-out curve. Higher = snappier start, slower end.")]
    [SerializeField] private float knockbackEasePower = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Red flashes per second during immortality time.")]
    [SerializeField] private float flashFrequency = 10f;

    // References
    private Health playerHealth;
    private PlayerMovement playerMovement;
    private PlayerImmortality playerImmortality;
    private Rigidbody rb;
    private Renderer[] cachedRenderers;
    private Color[] originalColors;

    // I-frame timing
    private float nextAllowedDamageTime = 0f;

    // Coroutine handles to safely stop overlapping effects
    private Coroutine flashCorountine;
    private Coroutine knockbackCoroutine;

    private CameraController cameraController;
    private CameraShakeEffect directCameraShake; // fallback if controller not present

    private void Awake()
    {
        // Cache references
        playerHealth = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        playerImmortality = GetComponent<PlayerImmortality>();

        // Cache all renderers to flash all parts of the player
        cachedRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
            originalColors[i] = cachedRenderers[i].material.color;
    }

    /// <summary>
    /// Attempts to apply damage and knockback. Will not apply if within i-frames.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    /// <param name="knockDirection">Direction to knock the player.</param>
    /// <param name="knockbackDistance">Distance the player should be knocked back.</param>
    public void TryApplyDamageAndKnockback(float damageAmount, Vector3 knockDirection, float knockbackDistance)
    {
        // Check player immortality
        if (playerImmortality.IsImmortal)
            return;

        // Check i-frame
        if (Time.time < nextAllowedDamageTime)
            return;

        // Start i-frames
        nextAllowedDamageTime = Time.time + Mathf.Max(0f, immortalityTime);

        // Apply health damage
        playerHealth.TakeDmg(damageAmount);

        // Start knockback and flash coroutines
        StartKnockback(knockDirection, knockbackDistance);
        StartFlash();
    }

    /// <summary>
    /// Starts knockback coroutine, stopping any existing knockback.
    /// </summary>
    private void StartKnockback(Vector3 direction, float distance)
    {
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized, distance));
    }

     /// <summary>
     /// Moves the player using Rigidbody.MovePosition with ease-out, based on distance and knockback speed.
     /// Runs in FixedUpdate for physics consistency.
     /// </summary>
     private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
     {
         // Prevent player movement during knockback
         playerMovement.StopMovement();

         Vector3 start = rb.position;
         Vector3 target = start + dir * distance;

         // Duration is based on distance and speed
         float duration = Mathf.Max(0.01f, distance / knockbackSpeed);
       float elapsed = 0f;

       while (elapsed < duration)
       {
           float t = elapsed / duration;

           // Ease-out interpolation (fast start, slow end)
           float easedT = 1f - Mathf.Pow(1f - t, knockbackEasePower);

           // Move Rigidbody to interpolated position
           rb.MovePosition(Vector3.Lerp(start, target, easedT));

           elapsed += Time.fixedDeltaTime;

           // Wait for the next physics step
           yield return new WaitForFixedUpdate();
       }

       // Snap exactly to target to prevent drift
       rb.MovePosition(target);

       // Resume player movement
       playerMovement.ResumeMovement();
   }

    /// <summary>
    /// Starts flashing the player’s renderers. Stops existing flash coroutine if running.
    /// </summary>
    private void StartFlash()
    {
        if (cachedRenderers.Length == 0 || flashFrequency <= 0f)
            return;

        if (flashCorountine != null)
            StopCoroutine(flashCorountine);

        flashCorountine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Flashes all cached renderers red/normal during i-frame period.
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        // Time between color toggles
        float interval = 1f / flashFrequency / 2f;
        bool isRed = false;

        while (Time.time < nextAllowedDamageTime)
        {
            isRed = !isRed;

            for (int i = 0; i < cachedRenderers.Length; i++)
                cachedRenderers[i].material.color = isRed ? Color.red : originalColors[i];

            yield return new WaitForSeconds(interval);
        }

        // Ensure colors are restored at the end
        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRenderers[i].material.color = originalColors[i];

        flashCorountine = null;
    }
}
