using UnityEngine;

//Nick: Attach this to the player
//Attach the GrappleTower script to the anchor
//The player will need a line renderer for the rope, make one

public class PlayerGrapple : MonoBehaviour
{
	[Header("Grapple Settings")]
	public float grappleRange = 30f;
	public LayerMask grappleLayer;
	public Transform firePoint;

	[Header("Visuals")]
	public LineRenderer rope;
	public GameObject hookPreview;
	public float previewSize = 0.3f;

	CharacterController controller;
	Camera cam;

	bool isGrappling;
	Vector3 grapplePoint;
	float chainLength;
	GrappleTower currentTower;

	void Start()
	{
		controller = GetComponent<CharacterController>();
		cam = Camera.main;

		rope.positionCount = 2;
		rope.enabled = false;
		hookPreview.SetActive(true);
	}

	void Update()
	{
		HandlePreview();
		HandleInput();

		if (isGrappling)
		{
			ConstrainMovement();
			UpdateRope();
		}
	}

	void HandleInput()
	{
		// Use old input system "Fire3" instead of mouse or controller-specific calls.
		if (Input.GetButtonDown("Fire3"))
		{
			if (!isGrappling)
				TryGrapple();
			else
				ReleaseGrapple();
		}
	}

	void TryGrapple()
	{
		// Stop aiming with the mouse. Instead pick the closest GameObject with tag "tower"
		// within grappleRange and on the configured layer(s).
		Collider[] colliders = Physics.OverlapSphere(transform.position, grappleRange, grappleLayer);
		Collider closest = null;
		float minDist = float.MaxValue;

		foreach (var c in colliders)
		{
			if (!c.CompareTag("tower")) continue;

			float d = Vector3.SqrMagnitude(c.transform.position - transform.position);
			if (d < minDist)
			{
				minDist = d;
				closest = c;
			}
		}

		if (closest == null) return;

		// Prefer GrappleTower on the collider or its parent
		GrappleTower tower = closest.GetComponent<GrappleTower>() ?? closest.GetComponentInParent<GrappleTower>();
		if (!tower) return;

		isGrappling = true;
		// Use the closest point on the collider so the grapple point is accurate for complex colliders
		grapplePoint = closest.ClosestPoint(transform.position);
		chainLength = Vector3.Distance(transform.position, grapplePoint);
		currentTower = tower;

		rope.enabled = true;
		ApplyTowerEffects(tower);
	}

	void ReleaseGrapple()
	{
		isGrappling = false;
		rope.enabled = false;
		currentTower = null;

		RemoveTowerEffects();
	}

	void ConstrainMovement()
	{
		Vector3 dir = transform.position - grapplePoint;
		float dist = dir.magnitude;

		if (dist > chainLength)
		{
			Vector3 constrainedPos = grapplePoint + dir.normalized * chainLength;
			transform.position = constrainedPos;
		}
	}

	void UpdateRope()
	{
		rope.SetPosition(0, firePoint.position);
		rope.SetPosition(1, grapplePoint);
	}

	void HandlePreview()
	{
		// Show preview on the closest tower within range instead of following the mouse.
		Collider[] colliders = Physics.OverlapSphere(transform.position, grappleRange, grappleLayer);
		Collider closest = null;
		float minDist = float.MaxValue;

		foreach (var c in colliders)
		{
			if (!c.CompareTag("tower")) continue;

			float d = Vector3.SqrMagnitude(c.transform.position - transform.position);
			if (d < minDist)
			{
				minDist = d;
				closest = c;
			}
		}

		if (closest != null)
		{
			hookPreview.SetActive(true);
			hookPreview.transform.position = closest.ClosestPoint(transform.position);
			hookPreview.transform.localScale = Vector3.one * previewSize;
		}
		else
		{
			hookPreview.SetActive(false);
		}
	}

	#region Tower Effects

	void ApplyTowerEffects(GrappleTower tower)
	{
		switch (tower.towerType)
		{
			case TowerType.Explosive:
				InvokeRepeating(nameof(ExplosivePulse), 1f, 1f);
				break;

			case TowerType.SlowAndDamageBoost:
				PlayerStats.Instance.damageMultiplier = tower.playerDamageMultiplier;
				break;

			case TowerType.ExtraProjectiles:
				PlayerStats.Instance.extraProjectiles += tower.extraProjectiles;
				break;
		}
	}

	void RemoveTowerEffects()
	{
		CancelInvoke(nameof(ExplosivePulse));
		PlayerStats.Instance.ResetStats();
	}

	void ExplosivePulse()
	{
		Collider[] hits = Physics.OverlapSphere(grapplePoint, currentTower.explosionRadius);

		foreach (var hit in hits)
		{
			// Use the new EnemyBase type. EnemyBase implements IDamageable, so cast to IDamageable
			// to apply damage. For knockback, use the enemy's Rigidbody if present.
			if (hit.TryGetComponent<EnemyBase>(out var enemy))
			{
				// Apply damage via IDamageable (EnemyBase implements it explicitly)
				if (enemy is IDamageable dmgable)
				{
					dmgable.TakeDmg(currentTower.dotDamage);
				}
				else
				{
					// Fallback: try EnemyHealth component if present
					var enemyHealth = enemy.GetComponent<EnemyHealth>();
					if (enemyHealth != null)
						enemyHealth.TakeDamage(currentTower.dotDamage);
				}

				// Apply knockback using Rigidbody (if available)
				var rb = enemy.GetComponent<Rigidbody>();
				if (rb != null)
				{
					Vector3 forceDir = (hit.transform.position - grapplePoint).normalized;
					float knockbackStrength = 5f; // tuned value; adjust as needed
					rb.AddForce(forceDir * knockbackStrength, ForceMode.Impulse);
				}
			}
		}
	}

	#endregion

	// Draw grapple radius gizmo in the editor when this object is selected.
	void OnDrawGizmosSelected()
	{
		// translucent fill
		Gizmos.color = new Color(0f, 0.6f, 1f, 0.08f);
		Gizmos.DrawSphere(transform.position, grappleRange);

		// clear wire
		Gizmos.color = new Color(0f, 0.9f, 1f, 1f);
		Gizmos.DrawWireSphere(transform.position, grappleRange);
	}
}
