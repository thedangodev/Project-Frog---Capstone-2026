using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        //replace for actual hit logic
        Debug.Log("player hit");
    }
}
