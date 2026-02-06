using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyFrogSkeleton : EnemyBase
{
    [Header("Attack config")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private GameObject attackHitbox;
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
                Attack();
            }
            return;
        }
        MoveTo(player.position);
      
    }
    #region attack
    private void Attack()
    {
        canAttack = false;
        attackHitbox.SetActive(true);
        //hook up to anim for cooldown//Replace this section
        StartCoroutine(AttackCooldown());
        //
    }
    private void AttackFinish()
    {
        canAttack = true;
        attackHitbox.SetActive(false);

    }
    protected virtual IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(2f);
        AttackFinish();
    }
    #endregion
}
