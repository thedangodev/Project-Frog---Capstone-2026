using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    private Rigidbody rb;
    private bool isActive = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        if (!isActive || player == null)
            return;

        MoveTowardPlayer();
    }

    private void MoveTowardPlayer()
    {
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;

        float distance = direction.magnitude;
        if (distance <= stoppingDistance)
            return;

        Vector3 moveDirection = direction.normalized;
        Vector3 targetPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 10f * Time.fixedDeltaTime));
        }
    }

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        rb.isKinematic = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (other.CompareTag("Player"))
        {
            Die();
        }
    }

    private void Die()
    {
        EnemySpawnWaves spawner = FindObjectOfType<EnemySpawnWaves>();
        if (spawner != null)
        {
            spawner.OnEnemyKilled(gameObject);
        }

        Destroy(gameObject);
    }
}
