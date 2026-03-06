using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
	[SerializeField] private float baseSpeed = 10f; //Adjust as nessisary 
	[SerializeField] private float baseDamage = 10f; //Adjust as nessisary 
	[SerializeField] private float maxScale = 2f; //Adjust as nessisary 

	public float speed;
	public float damage;

	public enum TargetMode
	{
		Player,
		Enemy,
		Both
	}

	[Header("Collision")]
	[Tooltip("Choose whether this projectile damages Player, Enemy, or Both.")]
	public TargetMode targetMode = TargetMode.Enemy;

	[Tooltip("If true the projectile is destroyed when it hits a valid target.")]
	public bool destroyOnHit = true;

	public void Initialize(float chargePercent)
	{
		speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
		damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

		float scale = Mathf.Lerp(0.25f, maxScale, chargePercent); //Only exists to help visualize the charge's effect on the projectile in the absence of damage
		transform.localScale = Vector3.one * scale;

		Destroy(gameObject, 3f);
	}

	private void Awake()
	{
		// Ensure there is a trigger collider; if not, add a default one (Box) and set trigger.
		var col = GetComponent<Collider>();
		if (col == null)
		{
			col = gameObject.AddComponent<BoxCollider>();
		}
		col.isTrigger = true;
	}

	private void Update()
	{
		transform.position += transform.forward * speed * Time.deltaTime;
	}

	private void OnTriggerEnter(Collider other)
	{
		HandleHit(other);
	}

	private void OnCollisionEnter(Collision collision)
	{
		HandleHit(collision.collider);
	}

	void HandleHit(Collider other)
	{
		if (other == null) return;

		bool dealtDamage = false;

		// Player
		if (targetMode == TargetMode.Player || targetMode == TargetMode.Both)
		{
			if (other.gameObject.CompareTag("Player"))
			{
				// Prefer Health on the PlayerMovement root, otherwise find any Health on parents
				var playerMovement = other.GetComponentInParent<PlayerMovement>();
				Health health = null;
				if (playerMovement != null)
					health = playerMovement.GetComponent<Health>() ?? playerMovement.GetComponentInChildren<Health>();
				if (health == null)
					health = other.GetComponentInParent<Health>();

				if (health != null)
				{
					health.TakeDmg(damage);
				}
				else
				{
					Debug.LogWarning($"[{nameof(Projectile)}] Hit Player but no Health component found on {other.name}.");
				}

				dealtDamage = true;
			}
		}

		// Enemy
		if ((targetMode == TargetMode.Enemy || targetMode == TargetMode.Both) && !dealtDamage)
		{
			// Prefer EnemyBase, then IDamageable, EnemyHealth, then tag fallback
			if (other.TryGetComponent<EnemyBase>(out var enemyBase))
			{
				if (enemyBase is IDamageable dmgable)
				{
					dmgable.TakeDmg(damage);
				}
				else
				{
					var enemyHealth = enemyBase.GetComponent<EnemyHealth>();
					if (enemyHealth != null)
						enemyHealth.TakeDamage(damage);
					else
					{
						var fallback = enemyBase.GetComponentInParent<Health>();
						if (fallback != null)
							fallback.TakeDmg(damage);
						else
							Debug.LogWarning($"[{nameof(Projectile)}] Hit Enemy but no damageable component found on {other.name}.");
					}
				}

				dealtDamage = true;
			}
			else
			{
				// Try parent EnemyBase
				var parentEnemy = other.GetComponentInParent<EnemyBase>();
				if (parentEnemy != null)
				{
					if (parentEnemy is IDamageable pdmg)
						pdmg.TakeDmg(damage);
					else
					{
						var eh = parentEnemy.GetComponent<EnemyHealth>();
						if (eh != null) eh.TakeDamage(damage);
						else
						{
							var fallback = parentEnemy.GetComponentInParent<Health>();
							if (fallback != null) fallback.TakeDmg(damage);
							else Debug.LogWarning($"[{nameof(Projectile)}] Hit Enemy but no damageable component found on {other.name}.");
						}
					}
					dealtDamage = true;
				}
				else
				{
					// Generic IDamageable fallback
					if (other.TryGetComponent<IDamageable>(out var anyDmg))
					{
						anyDmg.TakeDmg(damage);
						dealtDamage = true;
					}
					else if (other.gameObject.CompareTag("Enemy"))
					{
						var fallbackHealth = other.GetComponentInParent<Health>();
						if (fallbackHealth != null)
						{
							fallbackHealth.TakeDmg(damage);
							dealtDamage = true;
						}
					}
				}
			}
		}

		if (dealtDamage && destroyOnHit)
		{
			Destroy(gameObject);
		}
	}
}
