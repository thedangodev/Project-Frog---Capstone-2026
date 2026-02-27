using System.Collections;
using UnityEngine;

public class EnemyRockGolem : EnemyBase
{
    [Header("Attacks settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float normalAttackCooldown = 1.5f;
    [SerializeField] private float specialCooldown = 5f;
    [SerializeField] private RockGolemSpecial special;

    protected bool canSpecial = true;
    protected bool canBasic = true;

    protected override void Awake()
    {
        base.Awake();
   
    }
    private void Start()
    {
        if(special == null)
        {
            Debug.Log("Special not found");
            return;
        }

        special.OnSpecialFinished += () => StartCoroutine(SpecialCooldownRoutine());
    }
    protected override void Update()
    {
        base.Update();

        if (player == null) return;

        HandleBehaviour();
    }

    private void HandleBehaviour() //if outside of range, chase the player, otherwise attack the player
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            AttackPlayer();
        }
    }
    #region Behaviours
    protected void ChasePlayer()
    {
        MoveTo(player.position);  
    }

    protected void AttackPlayer()
    {
        StopMovement();

        if (CanUseSpecial())
        {
            SpecialAttack();
        }
        else if (CanUseBasic())
        {
            BasicAttack();
        }
    }
    #endregion

    #region attack availability checks
    private bool CanUseSpecial()
    {
        return canSpecial;
    }

    private bool CanUseBasic()
    {
        return canBasic;
    }

    public void SetSpecialAvailable(bool value)
    {
        canSpecial = value;
    }
    #endregion

    #region Attacks
    protected void SpecialAttack()
    {
        canSpecial = false;
        special.ActivateSpecial();
    }
    protected void BasicAttack()
    {

    }
    #endregion

    #region Cooldowns
    private IEnumerator SpecialCooldownRoutine()
    {
        yield return new WaitForSeconds(specialCooldown);
        canSpecial = true;
    }
    #endregion

}
