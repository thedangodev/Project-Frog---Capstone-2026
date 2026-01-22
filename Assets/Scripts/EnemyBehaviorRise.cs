using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBehaviorRise : MonoBehaviour
{
    [Header("Rise Settings")]
    [SerializeField] private float riseHeight = 1.5f;
    [SerializeField] private float riseSpeed = 2f;

    private Vector3 targetPosition;
    private bool isRising = true;

    private Rigidbody rb;
    private EnemyAI enemyAI;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyAI = GetComponent<EnemyAI>();

        rb.isKinematic = true;

        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
    }

    private void Start()
    {
        targetPosition = transform.position + Vector3.up * riseHeight;
    }

    private void Update()
    {
        if (!isRising)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            riseSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= 0.01f)
        {
            FinishRising();
        }
    }

    private void FinishRising()
    {
        isRising = false;


        if (enemyAI != null)
        {
            enemyAI.enabled = true;

            EnemySpawnWaves spawner = FindObjectOfType<EnemySpawnWaves>();
            if (spawner != null)
            {
                enemyAI.Activate(spawner.PlayerTransform);
            }
        }
    }
}
