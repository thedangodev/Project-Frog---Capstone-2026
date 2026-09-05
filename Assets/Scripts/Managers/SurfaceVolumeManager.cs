using UnityEngine;

// Put on a trigger collider placed over a painted-terrain area (e.g. a mud pit) to override the raycast-detected surface while the player is inside it. Requires a SurfaceTypeManager on the same object set to the override surface. -E.M

[RequireComponent(typeof(SurfaceTypeManager))]
public class SurfaceVolumeManager : MonoBehaviour
{
    private SurfaceTypeManager surfaceType;

    private void Awake() => surfaceType = GetComponent<SurfaceTypeManager>();

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[SurfaceVolume] ENTER fired by '{other.name}' (layer {other.gameObject.layer})");

        if (other.GetComponentInParent<PlayerFootstepTypes>() is PlayerFootstepTypes fs)
        {
            //Debug.Log($"[SurfaceVolume] Override SET to {surfaceType.SurfaceLabel}");
            fs.SetSurfaceOverride(surfaceType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerFootstepTypes>() is PlayerFootstepTypes fs)
            fs.ClearSurfaceOverride(surfaceType);
    }
}