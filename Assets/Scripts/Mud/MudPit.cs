using System.Collections.Generic;
using UnityEngine;


//Attach this to Empty parent gameObject & attach helper to child with colliders
public class MudPit : MonoBehaviour
{
    [Header("Slow strength")]
    [SerializeField] private float speedMult;

    // Use to track how many colliders of THIS mudpit each player/enemy is inside
    private Dictionary<IMovement, int> insideCounts = new Dictionary<IMovement, int>();

    public void HandleEnter(Collider other)
    {
        IMovement victim = other.GetComponent<IMovement>();
        if (victim == null) return;

        if (!insideCounts.ContainsKey(victim))
        {
            insideCounts[victim] = 0;
        }

        insideCounts[victim]++;

        // Only apply debuff on first collider entered
        if (insideCounts[victim] == 1)
        {
            victim.AddSpeedModifier(this, speedMult);
        }
    }


    public void HandleExit(Collider other)
    {
        IMovement victim = other.GetComponent<IMovement>();
        if (victim == null) return;

        if (!insideCounts.ContainsKey(victim)) return;

        insideCounts[victim]--;

        // Only remove debuff if not still inside another collider form same mudpit
        if (insideCounts[victim] <= 0)
        {
            victim.RemoveSpeedModifier(this);
            insideCounts.Remove(victim);
        }
    }
}
