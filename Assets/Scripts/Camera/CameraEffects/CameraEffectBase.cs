using UnityEngine;

/// <summary>
/// Base class for all positional camera effects.
/// 
/// Effects must return a Vector3 offset.
/// This offset will be added to the camera’s base position.
/// </summary>
public abstract class CameraEffectBase : MonoBehaviour
{
    public abstract Vector3 ApplyEffect(float deltaTime);
}