using UnityEngine;


public class GrappleTower : MonoBehaviour
{
    public TowerType towerType;

    public FireTowerFields fireFields = new FireTowerFields();
    public IceTowerFields iceFields = new IceTowerFields();
    public WindTowerFields windFields = new WindTowerFields();
}
