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
    private EnemyBase enemyBase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemyBase = GetComponent<EnemyBase>();
    

        rb.isKinematic = true;

        if (enemyBase != null)
        {
            enemyBase.enabled = false;
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


        if (enemyBase != null)
        {
            enemyBase.enabled = true;

            EnemySpawnWaves spawner = FindObjectOfType<EnemySpawnWaves>();
            if (spawner != null)
            {
                enemyBase.Activate(spawner.PlayerTransform);
            }
        }
    }
}
