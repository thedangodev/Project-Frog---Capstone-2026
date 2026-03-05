using UnityEngine;

public interface IMovement
{
    void AddSpeedModifier(object source, float multiplier);
    void RemoveSpeedModifier(object source);
}
