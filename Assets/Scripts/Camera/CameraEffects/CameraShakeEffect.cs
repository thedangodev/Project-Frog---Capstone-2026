using UnityEngine;

/// Camera shake effect - can be triggered by the player taking damage.
/// Implementation produces a subtle back-and-forth shake on the X axis & Y axis.

public class CameraShakeEffect : CameraEffectBase
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitudeX = 0.3f;
    [SerializeField] private float shakeFrequency = 5f; // Hz - Oscillations per second

    // Optional Y axis settings
    [SerializeField] private float shakeMagnitudeY = 0f;
    [SerializeField] private float shakeFrequencyY = 5f; // Hz for Y axis
    [SerializeField] private float shakePhaseY = 0f; 

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
            float omegaX = shakeFrequency * 2f * Mathf.PI;
            float omegaY = shakeFrequencyY * 2f * Mathf.PI;

            // Sine-based oscillation on X and Y axes
            float x = Mathf.Sin(elapsed * omegaX) * shakeMagnitudeX * dampingFactor;
            float y = Mathf.Sin(elapsed * omegaY + shakePhaseY) * shakeMagnitudeY * dampingFactor;

            return new Vector3(x, y, 0f);
        }

        // Reset elapsed when not shaking so subsequent shakes start cleanly
        if (shakeTimer <= 0f && elapsed != 0f)
            elapsed = 0f;

        return Vector3.zero;
    }


    public void TriggerShake()
    {
        shakeTimer = shakeDuration;
        elapsed = 0f;
    }

    public void TriggerShake(float duration, float magnitude)
    {
        if (duration > 0f) shakeDuration = duration;
        if (magnitude >= 0f) shakeMagnitudeX = magnitude;
        shakeTimer = shakeDuration;
        elapsed = 0f;
    }

    // Set both X and Y magnitudes when triggering
    public void TriggerShake(float duration, float magnitudeX, float magnitudeY)
    {
        if (duration > 0f) shakeDuration = duration;
        if (magnitudeX >= 0f) shakeMagnitudeX = magnitudeX;
        if (magnitudeY >= 0f) shakeMagnitudeY = magnitudeY;
        shakeTimer = shakeDuration;
        elapsed = 0f;
    }
}