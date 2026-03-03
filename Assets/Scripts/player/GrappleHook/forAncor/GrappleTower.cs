using UnityEngine;

public class GrappleTower : MonoBehaviour
{
    public TowerType towerType;

    [Header("Grapple Settings")]
    [SerializeField] private float grappleRange = 15f;

    public float GrappleRange => grappleRange;

    public FireTowerFields fireFields = new FireTowerFields();
    public IceTowerFields iceFields = new IceTowerFields();
    public WindTowerFields windFields = new WindTowerFields();

    // Optional: visualize range in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grappleRange);
    }
}