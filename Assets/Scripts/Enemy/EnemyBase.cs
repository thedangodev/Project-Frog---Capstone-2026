using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Processors;

// Enemy BaseClass
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    protected bool enableNav = true;

    protected NavMeshAgent agent;

    protected bool canAttack = true;

    [SerializeField] protected GameObject attackHitbox;
    [SerializeField] private Health health;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
     
       
        enableNav = true;
    }

    protected virtual void Update()
    {
        if (health.IsDead) return;

    }

    void IDamageable.TakeDmg(float dmg)
    {
        health.TakeDmg(dmg);
    }

    #region Navigation

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
