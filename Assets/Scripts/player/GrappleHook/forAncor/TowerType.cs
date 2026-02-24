using UnityEngine;

[System.Serializable]
public class FireTowerFields
{
    public float damage = 10f;

    [Header("Burn Settings")]
    public float burnDuration = 2f;      // How long burn lasts
    public float burnTickRate = 0.2f;    // How often damage ticks
}

[System.Serializable]
public class IceTowerFields
{
    public float damage = 8f;

    [Header("Ice Settings")]
    public float slowMultiplier = 0.5f;  // 50% movement speed
    public float slowDuration = 2f;
    public float damageMultiplier = 2f;  // x2 damage
}
[System.Serializable]
public class IceTowerFields2
{
    public float damage = 8f;

    [Header("Ice Settings")]
    public float slowMultiplier = 0.5f;  // 50% movement speed
    public float slowDuration = 2f;
    public float damageMultiplier = 2f;  // x2 damage
}

[System.Serializable]
public class WindTowerFields
{
    public float damage = 12f;

    [Header("Wind Settings")]
    public float damageMultiplier = 0.7f; // 30% less damage (0.7x)
}
public enum TowerType
{
    Fire,
    Ice,
    Wind
}
//public enum TowerType
//{
//    Explosive,
//    SlowAndDamageBoost,
//    ExtraProjectiles
//}
d