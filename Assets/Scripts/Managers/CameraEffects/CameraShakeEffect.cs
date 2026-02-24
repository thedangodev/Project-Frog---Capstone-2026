using UnityEngine;

/// <summary>
/// Camera shake effect - can be triggered by damage, explosions, etc.
/// This implementation produces a subtle back-and-forth shake on the X axis.
/// </summary>
public class CameraShakeEffect : CameraEffectBase
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    [SerializeField] private float shakeFrequency = 5f; // Hz - how many oscillations per second

    private float shakeTimer = 0f;
    private float elapsed = 0f;

    public override Vector3 ApplyEffect(float deltaTime)
    {
        if (shakeTimer > 0f && shakeDuration > 0f)
        {
            shakeTimer -= deltaTime;
            elapsed += deltaTime;

            // Damping so shake eases out
            float dampingFactor = Mathf.Clamp01(shakeTimer / shakeDuration);

            // Convert frequency (Hz) to radians/sec
            float omega = shakeFrequency * 2f * Mathf.PI;

            // Sine-based oscillation on X axis only
            float x = Mathf.Sin(elapsed * omega) * shakeMagnitude * dampingFactor;

            return new Vector3(x, 0f, 0f);
        }

        // Reset elapsed when not shaking so subsequent shakes start cleanly
        if (shakeTimer <= 0f && elapsed != 0f)
            elapsed = 0f;

        return Vector3.zero;
    }

    /// <summary>
    /// Triggers a camera shake with the configured duration & magnitude.
    /// </summary>
    public void TriggerShake()
    {
        shakeTimer = shakeDuration;
        elapsed = 0f;
    }

    /// <summary>
    /// Triggers a camera shake with custom parameters.
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        if (duration > 0f) shakeDuration = duration;
        if (magnitude >= 0f) shakeMagnitude = magnitude;
        shakeTimer = shakeDuration;
        elapsed = 0f;
    }
}