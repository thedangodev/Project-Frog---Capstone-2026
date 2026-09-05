using UnityEngine;
using FMODUnity;
using FMOD.Studio;

// Determines and plays the footstep sounds dependant on what surface the Player is currently walking on from the SurfaceTypeManager Enumeration. Also supports painted-surface trigger volumes that override the raycast-detected surface while the player is inside them. -E.M

[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootstepTypes : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] private EventReference footstepEvent;
    [SerializeField] private string surfaceParameter = "Surface";

    [Header("Stepping")]
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayHeight = 0.3f;
    [SerializeField] private float rayDistance = 2f;

    private PlayerMovement movement;
    private Vector3 lastStepPos;
    private SurfaceTypeManager overrideSurface;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        lastStepPos = transform.position;
    }

    private void Update()
    {
        // Only accumulate distance while actually walking.
        // GetMovementSpeed() returns 0 while dashing/stunned/stopped/idle.
        if (movement.GetMovementSpeed() <= 0.01f)
        {
            lastStepPos = transform.position; // reset so no step fires on resume
            return;
        }

        Vector3 flat = transform.position;
        flat.y = lastStepPos.y; // ignore vertical drift

        if (Vector3.Distance(flat, lastStepPos) >= stepDistance)
        {
            PlayFootstep();
            lastStepPos = transform.position;
        }
    }

    private void PlayFootstep()
    {
        EventInstance step = RuntimeManager.CreateInstance(footstepEvent);
        step.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        SurfaceTypeManager surface = overrideSurface;
        if (surface == null && TryGetSurface(out SurfaceTypeManager hit))
            surface = hit;

        Debug.Log($"[Footstep] overrideSurface = {(overrideSurface != null ? overrideSurface.SurfaceLabel : "null")}, playing = {(surface != null ? surface.SurfaceLabel : "null")}");

        if (surface != null)
            step.setParameterByNameWithLabel(surfaceParameter, surface.SurfaceLabel);

        step.start();
        step.release();
    }

    private bool TryGetSurface(out SurfaceTypeManager surfaceType)
    {
        surfaceType = null;
        Vector3 origin = transform.position + Vector3.up * rayHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayDistance, groundMask))
        {
            surfaceType = hit.collider.GetComponentInParent<SurfaceTypeManager>();
            return surfaceType != null;
        }

        return false;
    }

    // Called by SurfaceVolumeManager when the player enters a painted-surface trigger.
    public void SetSurfaceOverride(SurfaceTypeManager surface) => overrideSurface = surface;

    public void ClearSurfaceOverride(SurfaceTypeManager surface)
    {
        // Only clear if this is the volume that set it, so overlapping exits don't stomp each other.
        if (overrideSurface == surface) overrideSurface = null;
    }
}