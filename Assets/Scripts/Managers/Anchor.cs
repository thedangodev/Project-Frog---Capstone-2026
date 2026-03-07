using UnityEngine;

public enum AnchorType
{
    None,
    Fire,
    Ice,
    Wind
}

[ExecuteAlways]
public class Anchor : MonoBehaviour
{
    [Header("Anchor Settings")]
    public AnchorType anchorType = AnchorType.None;
    public float radius = 1.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = anchorType switch
        {
            AnchorType.Fire => Color.red,
            AnchorType.Ice => Color.cyan,
            AnchorType.Wind => Color.green,
            _ => Color.white
        };

        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AnchorProvider provider = other.GetComponent<AnchorProvider>();
            if (provider != null)
                provider.CurrentAnchor = anchorType;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AnchorProvider provider = other.GetComponent<AnchorProvider>();
            if (provider != null)
                provider.CurrentAnchor = AnchorType.None;
        }
    }
}
