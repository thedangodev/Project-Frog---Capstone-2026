using Unity.VisualScripting;
using UnityEngine;

public class EnemyFrogSkeleton : EnemyBase
{
    [Header("Attack config")]
    [SerializeField] private float attackRange = 1f;


    [SerializeField]private Transform player;

    protected override void Awake()
    {
        base.Awake();
     
    }

    protected override void Update()
    {
        base.Update();

        if (player == null)
        {
            Debug.Log("player missing");
            return;
        }
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if(distanceToPlayer < attackRange)
        {
            StopMovement();
            if (canAttack)
            {
                // attack
            }
            return;
        }
        MoveTo(player.position);
      
    }


}
