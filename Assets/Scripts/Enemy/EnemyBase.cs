using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Processors;

// Enemy BaseClass
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] protected Transform player;

    protected bool enableNav = true;
    protected NavMeshAgent agent;

    protected bool canAttack = true;

    [Header("References")]
    [SerializeField] protected Transform player;

    [SerializeField] protected GameObject attackHitbox;
    [SerializeField] private protected Health health;
    

    
    private bool isActive;
    private Rigidbody rb;

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        rb.isKinematic = false;
    }
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        
        // Initialize health component
        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogWarning($"No Health component found on {gameObject.name}. Adding one automatically.");
                health = gameObject.AddComponent<Health>();
                
                //// Add Healthbar if missing (required by Health)
                //if (GetComponent<Healthbar>() == null)
                //{
                //    gameObject.AddComponent<Healthbar>();
                //}
            }
        }
        
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                Debug.Log($"Found player: {player.name}");
            }
            else
            {
                Debug.LogError("No GameObject with 'Player' tag found!");
            }
        }
          enableNav = true;
    }

    protected virtual void Update()
    {
        if (health != null && health.IsDead) return;
    }

    #region Health
    void IDamageable.TakeDmg(float dmg)
    {
        if (health != null)
            health.TakeDmg(dmg);
    }
    #endregion

    #region Navigation
    private bool isActive;
    private Rigidbody rb;

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }
    public virtual void MoveTo(Vector3 destination)
    {
        if (!enableNav) return;

        agent.isStopped = false;
        agent.SetDestination(destination);
        
    }
    public virtual void StopMovement()
    {
        if (!enableNav) return;
        agent.isStopped = true;
    }
    public virtual void ResumeMovement()
    {
        if (!enableNav) return;
        agent.isStopped = false;
    }
    #endregion
   
}
