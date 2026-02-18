using UnityEngine;

public class PlayerStats : MonoBehaviour
{
	public static PlayerStats Instance;

	public float damageMultiplier = 1f;
	public int extraProjectiles = 0;

	void Awake()
	{
		Instance = this;
	}

	public void ResetStats()
	{
		damageMultiplier = 1f;
		extraProjectiles = 0;
	}
}
