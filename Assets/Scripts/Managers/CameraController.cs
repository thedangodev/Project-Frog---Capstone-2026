using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 cameraAngle = new Vector3(45f, 0f, 0f);
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 20f, -20f);
    [SerializeField] private float followSpeed = 10f;

    [Header("Camera Effects")]
    [SerializeField] private List<CameraEffectBase> cameraEffects = new List<CameraEffectBase>();

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 effectOffset;

    /* private Vector3 offset = new Vector3(0, 20, -20); */ //can be used in the future

    private void Start()
    {
        // set initial camera angle
        baseRotation = Quaternion.Euler(cameraAngle);
        transform.rotation = baseRotation;

        // fallback - find target if not assigned in the inspector
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Auto-discover CameraEffectBase components on this GameObject and children
        if (cameraEffects == null || cameraEffects.Count == 0)
        {
            var found = GetComponentsInChildren<CameraEffectBase>(true);
            if (found != null && found.Length > 0)
                cameraEffects = new List<CameraEffectBase>(found);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (target == transform) return; // Avoids camera self-targeting at runtime

        Vector3 desiredPosition = target.position + cameraPosition;

        // smooth following
        basePosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        effectOffset = Vector3.zero;
        foreach (CameraEffectBase effect in cameraEffects)
        {
            if (effect != null && effect.enabled)
            {
                effectOffset += effect.ApplyEffect(Time.deltaTime);
            }
        }
        transform.position = basePosition + effectOffset; // applies final position with effects
    }

    // Adds effect at runtime
    public void AddEffect(CameraEffectBase effect)
    {
        if (effect != null && !cameraEffects.Contains(effect))
        {
            cameraEffects.Add(effect);
        }
    }

    // Removes effect at runtime
    public void RemoveEffect(CameraEffectBase effect)
    {
        if (effect != null)
        {
            cameraEffects.Remove(effect);
        }
    }

    // Get effect of specific type
    public T GetEffect<T>() where T : CameraEffectBase
    {
        foreach (CameraEffectBase effect in cameraEffects)
        {
            if (effect is T)
            {
                return effect as T;
            }
        }
        return null;
    }

    // Public accessor for effects
    public Vector3 GetBasePosition()
    {
        return basePosition;
    }

    // Controller API: trigger shake
    public void TriggerShake()
    {
        var shake = GetEffect<CameraShakeEffect>() ?? GetComponentInChildren<CameraShakeEffect>(true);
        if (shake != null)
        {
            AddEffect(shake);
            shake.TriggerShake();
            return;
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        var shake = GetEffect<CameraShakeEffect>() ?? GetComponentInChildren<CameraShakeEffect>(true);
        if (shake != null)
        {
            AddEffect(shake);
            shake.TriggerShake(duration, magnitude);
            return;
        }

        Debug.LogWarning("CameraController.TriggerShake(duration, magnitude): no CameraShakeEffect found.");
    }
}

// Base class for camera effects. - Can be derived from by Scriptable Objects and or MonoBehaviour scripts
[System.Serializable]
public abstract class CameraEffectBase : MonoBehaviour
{
    // Applies camera effect - returns positional offset to add to camera this frame.
    public abstract Vector3 ApplyEffect(float deltaTime);

    /* FUTURE CAMERA EFFECTS
     Camera Shake (Implemented)
     Camera Pan (Implemented)
     Raindrops
     Lens Flare
     Damage Vignette
    */
}
