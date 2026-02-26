using UnityEngine;

public class CombatStatistics : MonoBehaviour
{
    [Header("Movement")]
    public float playerSpeed = 6f;

    [Header("Tongue")]
    public float tongueMaxLength = 10f;
    public float tongueExtendSpeed = 20f;
    public float tongueRetractSpeed = 25f;

    [Header("Combat")]
    public float enemyDamage = 25f;
    public float tongueHealAmount = 20f;

    [Header("Ranged")]
    public float arrowSpeed = 40f;
    public float attackSpeed = 1f;
}