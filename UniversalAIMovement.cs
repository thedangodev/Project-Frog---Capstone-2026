using UnityEngine;
using UnityEngine.AI;

public class UniversalAIMovement : MonoBehaviour
{
	public enum AIState { Patrol, Chase, Search }

	[Header("References")]
	public NavMeshAgent agent;
	public Transform player;
	public Transform[] patrolPoints;

	[Header("Detection")]
	public float viewDistance = 15f;
	[Range(0, 360)] public float viewAngle = 120f;
	public float hearingRadius = 8f;
	public LayerMask obstacleMask;

	[Header("Movement")]
	public float patrolSpeed = 2f;
	public float chaseSpeed = 4.5f;
	public float waitAtPointTime = 2f;

	[Header("Search")]
	public float searchTime = 4f;

	private AIState currentState;
	private int patrolIndex;
	private float waitTimer;
	private float searchTimer;
	private Vector3 lastKnownPlayerPos;

	void Start()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();
		currentState = AIState.Patrol;
		agent.speed = patrolSpeed;
		SetNextPatrolPoint();
	}

	void Update()
	{
		switch (currentState)
		{
			case AIState.Patrol:
				Patrol();
				DetectPlayer();
				break;

			case AIState.Chase:
				Chase();
				break;

			case AIState.Search:
				Search();
				break;
		}
	}

	#region States

	void Patrol()
	{
		if (!agent.pathPending && agent.remainingDistance < 0.5f)
		{
			waitTimer += Time.deltaTime;
			if (waitTimer >= waitAtPointTime)
			{
				SetNextPatrolPoint();
				waitTimer = 0f;
			}
		}
	}

	void Chase()
	{
		agent.SetDestination(player.position);

		if (!CanSeePlayer() && !CanHearPlayer())
		{
			lastKnownPlayerPos = player.position;
			currentState = AIState.Search;
			searchTimer = 0f;
			agent.SetDestination(lastKnownPlayerPos);
		}
	}

	void Search()
	{
		searchTimer += Time.deltaTime;

		if (CanSeePlayer())
		{
			SwitchToChase();
			return;
		}

		if (searchTimer >= searchTime)
		{
			SwitchToPatrol();
		}
	}

	#endregion

	#region Detection

	void DetectPlayer()
	{
		if (CanSeePlayer() || CanHearPlayer())
		{
			SwitchToChase();
		}
	}

	bool CanSeePlayer()
	{
		Vector3 dirToPlayer = (player.position - transform.position).normalized;
		float angle = Vector3.Angle(transform.forward, dirToPlayer);

		if (angle < viewAngle / 2f)
		{
			float dist = Vector3.Distance(transform.position, player.position);
			if (dist <= viewDistance)
			{
				if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask))
					return true;
			}
		}
		return false;
	}

	bool CanHearPlayer()
	{
		return Vector3.Distance(transform.position, player.position) <= hearingRadius;
	}

	#endregion

	#region Helpers

	void SetNextPatrolPoint()
	{
		if (patrolPoints.Length == 0) return;

		agent.speed = patrolSpeed;
		agent.SetDestination(patrolPoints[patrolIndex].position);
		patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
	}

	void SwitchToChase()
	{
		currentState = AIState.Chase;
		agent.speed = chaseSpeed;
	}

	void SwitchToPatrol()
	{
		currentState = AIState.Patrol;
		agent.speed = patrolSpeed;
		SetNextPatrolPoint();
	}

	#endregion

	#region Gizmos

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, hearingRadius);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, viewDistance);

		Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
		Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

		Gizmos.DrawRay(transform.position, left * viewDistance);
		Gizmos.DrawRay(transform.position, right * viewDistance);
	}

	#endregion
}
