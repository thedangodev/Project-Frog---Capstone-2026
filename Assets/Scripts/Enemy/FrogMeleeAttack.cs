using System.Collections;
using UnityEngine;
using FMODUnity;

public class FrogMeleeAttack : EnemyAttack
{
    [Header("Frog Melee Configuration")]
    [SerializeField] private GameObject attackHitBox;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float hitBoxLifeTime = 0.1f;
    [SerializeField] private float meleeDamage = 10f;
    [SerializeField] private float meleeKnockbackDistance = 2f;

    [Header("Animation Event")]
    [Tooltip("Max seconds to wait for the animation event before giving up (safety net).")]
    [SerializeField] private float attackEventTimeout = 2f;

    [Header("FMod Events")]
    [SerializeField] private EventReference attackSoundEvent;

    // Set true by the animation event 'attackEvent'
    private bool attackEventFired = false;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected override void OnExecuteAttack(Vector3 targetPosition)
    {
        StartCoroutine(MeleeRoutine());
    }

    // Called by the Animation Event named 'attackEvent' on the attack clip.
    public void attackEvent()
    {
        attackEventFired = true;
    }

    private IEnumerator MeleeRoutine()
    {
        IsAttacking = true;

        RuntimeManager.PlayOneShot(attackSoundEvent, transform.position);

        attackEventFired = false;
        animator.SetTrigger("Attack");

        // Wait until the animation event fires (or timeout as a safety net).
        float waited = 0f;
        while (!attackEventFired && waited < attackEventTimeout)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (attackHitBox != null && attackPoint != null)
        {
            GameObject currentHitBox = Instantiate(attackHitBox, attackPoint.position, attackPoint.rotation);
            Collider hbCollider = currentHitBox.GetComponent<Collider>();
            float checkRadius = 0.6f;
            if (hbCollider != null)
            {
                Vector3 scale = currentHitBox.transform.lossyScale;
                if (hbCollider is SphereCollider sc)
                {
                    checkRadius = sc.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
                else if (hbCollider is CapsuleCollider cc)
                {
                    checkRadius = Mathf.Max(cc.radius, cc.height * 0.5f) * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
                else if (hbCollider is BoxCollider bc)
                {
                    checkRadius = bc.size.magnitude * 0.5f * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
                }
            }

            Collider[] hits = Physics.OverlapSphere(attackPoint.position, checkRadius, ~0, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                if (hit.gameObject.CompareTag("Player"))
                {
                    var playerTake = hit.GetComponentInParent<PlayerTakeDamage>() ?? hit.GetComponent<PlayerTakeDamage>();
                    if (playerTake != null)
                    {
                        Vector3 dir = (hit.transform.position - transform.position);
                        dir.y = Mathf.Max(dir.y, 0.2f);
                        playerTake.TryApplyDamageAndKnockback(meleeDamage, dir.normalized, meleeKnockbackDistance);
                        continue;
                    }
                    var health = hit.GetComponentInParent<Health>() ?? hit.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDmg(meleeDamage);
                        continue;
                    }
                }
            }
            Destroy(currentHitBox, hitBoxLifeTime);
        }

        yield return new WaitForSeconds(hitBoxLifeTime);
        IsAttacking = false;
    }
}