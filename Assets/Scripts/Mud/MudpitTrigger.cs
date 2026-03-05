using UnityEngine;

public class MudpitTrigger : MonoBehaviour
{
    private MudPit mudpit;

    private void Awake()
    {
        mudpit = GetComponentInParent<MudPit>();
    }

    private void OnTriggerEnter(Collider other)
    {
        mudpit.HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        mudpit.HandleExit(other);
    }
}
