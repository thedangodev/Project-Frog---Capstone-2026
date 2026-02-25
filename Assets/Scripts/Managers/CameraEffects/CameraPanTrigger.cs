using UnityEngine;

/// Attach script to trigger colliders you want to act as camera pan triggers.
/// Make sure the GameObject is tagged "CameraPan".
/// Assign GameObject transform - POI (Point of Interest) in the inspector
[RequireComponent(typeof(Collider))]
public class CameraPanTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Point of interest to pan the camera to when the player enters this trigger.")]
    [SerializeField] private Transform pointOfInterest;

    [Tooltip("Pan time (seconds) for one-way pan. Camera will return using same time.")]
    [SerializeField] private float panTime = 1.0f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CompareTag("CameraPan"))
            return;

        // Only the player triggers the pan.
        if (!other.CompareTag("Player"))
            return;

        if (pointOfInterest == null)
        {
            Debug.LogWarning($"CameraPanTrigger on '{gameObject.name}' has no PointOfInterest assigned.");
            return;
        }

        // Find CameraPanEffect on the main camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        var panEffect = mainCam.GetComponentInChildren<CameraPanEffect>(true);
        if (panEffect == null)
            return;

        // Trigger the pan
        panEffect.TriggerPan(pointOfInterest, panTime);
    }
}