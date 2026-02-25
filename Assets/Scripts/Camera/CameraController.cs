using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls a fixed-rotation follow camera with modular camera effects. Smoothly follow a target and applie stacked positional camera effects (shake, pan, etc.)
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 cameraRotation = new Vector3(35f, 0f, 0f);
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 5f, -20f);
    [SerializeField] private float followSpeed = 10f;

    [Header("Camera Effects")]
    // Active camera effects (shake, pan, etc.)
    [SerializeField] private List<CameraEffectBase> cameraEffects = new();

    // The camera’s calculated follow position before effects are applied
    // Effects use this as their reference position
    private Vector3 basePosition;

    // Final combined offset from all active effects
    private Vector3 effectOffset;

    private void Start()
    {
        // Apply fixed rotation immediately
        transform.rotation = Quaternion.Euler(cameraRotation);

        // Auto-find player if target not manually assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Recalculate fixed rotation (in case values change in inspector)
        Quaternion targetRotation = Quaternion.Euler(cameraRotation);

        // Rotate the offset so cameraPosition respects cameraRotation
        Vector3 rotatedOffset = targetRotation * cameraPosition;

        // Desired position relative to the target
        Vector3 desiredPosition = target.position + rotatedOffset;

        // Smoothly move toward the desired position
        basePosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        // Reset effect offset accumulator
        effectOffset = Vector3.zero;

        // Apply all active camera effects
        foreach (var effect in cameraEffects)
        {
            if (effect != null && effect.enabled)
                effectOffset += effect.ApplyEffect(Time.deltaTime);
        }

        // Final position = smooth follow position + effect offsets
        transform.position = basePosition + effectOffset;

        // Maintain fixed rotation
        transform.rotation = targetRotation;
    }


    #region Public API, used by camera effects
    public Vector3 GetBasePosition()
    {
        return basePosition;
    }

    /// <summary>
    /// Adds an effect to the active effect list.
    /// </summary>
    public void AddEffect(CameraEffectBase effect)
    {
        if (effect != null && !cameraEffects.Contains(effect))
            cameraEffects.Add(effect);
    }

    /// <summary>
    /// Removes an effect from the active effect list.
    /// </summary>
    public void RemoveEffect(CameraEffectBase effect)
    {
        if (effect != null)
            cameraEffects.Remove(effect);
    }

    /// <summary>
    /// Returns the first effect of type T if it exists.
    /// </summary>
    public T GetEffect<T>() where T : CameraEffectBase
    {
        foreach (CameraEffectBase effect in cameraEffects)
        {
            if (effect is T typedEffect)
                return typedEffect;
        }

        return null;
    }

    /// <summary>
    /// Triggers shake using default values defined in CameraShakeEffect.
    /// </summary>
    public void TriggerShake()
    {
        var shake = GetEffect<CameraShakeEffect>()
                    ?? GetComponentInChildren<CameraShakeEffect>(true);

        if (shake != null)
        {
            AddEffect(shake);
            shake.TriggerShake();
        }
    }

    /// <summary>
    /// Triggers shake with custom duration and magnitude.
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        var shake = GetEffect<CameraShakeEffect>()
                    ?? GetComponentInChildren<CameraShakeEffect>(true);

        if (shake != null)
        {
            AddEffect(shake);
            shake.TriggerShake(duration, magnitude);
            return;
        }

        Debug.LogWarning(
            "CameraController.TriggerShake(duration, magnitude): no CameraShakeEffect found."
        );
    }
    #endregion
}