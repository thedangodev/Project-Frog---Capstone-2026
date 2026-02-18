using UnityEngine;

public class GrappleTower : MonoBehaviour
{
	public TowerType towerType;

	[Header("Explosive Tower")]
	public float explosionRadius = 3f;
	public float dotDamage = 5f;

	[Header("Slow Tower")]
	public float slowMultiplier = 0.5f;
	public float playerDamageMultiplier = 1.5f;

	[Header("Projectile Tower")]
	public int extraProjectiles = 2;
}
