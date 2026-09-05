using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Enemy BaseClass

[RequireComponent(typeof(MovementComponent))]
public abstract class EnemyBase : MonoBehaviour, IDamageable, IMovement
{
    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected MovementComponent movement;
    [SerializeField] protected EnemyAttack attackComponent;
    [SerializeField] protected GameObject attackHitbox;
    [SerializeField] private protected Health health;

    private Rigidbody rb;
    protected NavMeshAgent agent => movement?.Agent;

    protected bool enableNav = true;

    public bool IsAttacking => attackComponent != null && attackComponent.IsAttacking;
    public bool CanAttack => attackComponent != null && attackComponent.CanAttack;

    private bool isActive;
    private float originalAgentSpeed;
    private float environmentalSpeedModifier = 1f;
    private float statusSlowMultiplier = 1f;

    private EnemyFlash enemyFlash;

    // Per-source speed modifiers, stacked multiplicatively (matches PlayerMovement contract)
    private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

    private Coroutine slowCourotine;
    private Coroutine freezeCoroutine;

    public bool IsBurning { get; private set; }
    public bool IsFrozen { get; private set; }
    public bool IsSlowed { get; private set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<MovementComponent>();
        enemyFlash = GetComponent<EnemyFlash>();

        if (attackComponent == null)
        {
            attackComponent = GetComponent<EnemyAttack>();
        }

        //if (movement == null)
        //{
        //    Debug.LogError($"{name} is missing a MovementComponent.", this);
        //}

        if (agent != null)
        {
            originalAgentSpeed = agent.speed > 0f ? agent.speed : 3.5f;
        }

        if (enemyFlash == null)
        {
            enemyFlash = GetComponentInChildren<EnemyFlash>();
        }

        // Initialize health component
        if (health == null)
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                //Debug.LogWarning($"No Health component found on {gameObject.name}. Adding one automatically.");
                health = gameObject.AddComponent<Health>();
            }
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) playerObject = GameObject.Find("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                //Debug.Log($"Found player: {player.name}");
            }
            //else
            //{
            //    Debug.LogError("No GameObject with 'Player' tag found!");
            //}
        }
        enableNav = true;
    }

    public virtual void Start()
    {
        if (agent != null && agent.speed > 0f)
        {
            originalAgentSpeed = agent.speed;
        }
    }

    public void Activate(Transform playerTransform)
    {
        player = playerTransform;
        isActive = true;
        if (rb != null) rb.isKinematic = false;
    }

    protected virtual void Update()
    {
        if (health != null && health.IsDead) return;

        UpdateActualSpeed();
    }

    // Execute an Attack (Skeleton Frog Melee)
    public virtual void TryExecuteAttack()
    {
        if (health != null && health.IsDead) return;
        if (IsFrozen) return;

        if (attackComponent != null)
        {
            attackComponent.TriggerAttack();
        }
    }

    // Execute a Targeted Attack (Rock Golem)
    public virtual void TryExecuteAttack(Vector3 targetPosition)
    {
        if (health != null && health.IsDead) return;
        if (IsFrozen) return;

        if (attackComponent != null)
        {
            attackComponent.TriggerAttack(targetPosition);
        }
    }

    #region Health
    void IDamageable.TakeDmg(float dmg)
    {
        TakeDamage(dmg);
    }

    void IDamageable.TakeDmg(float dmg, string effectType, float effectDuration, float effectValue)
    {
        TakeDamage(dmg, effectType, effectDuration, effectValue);
    }

    public void TakeDamage(float dmg)
    {
        if (health != null)
        {
            health.TakeDmg(dmg);
        }

        if (enemyFlash != null && (dmg <health.CurrentHealth))
        {
            enemyFlash.Flash();
        }
    }

    public void TakeDamage(float dmg, string effectType, float effectDuration, float effectValue)
    {
        if (health != null)
            health.TakeDmg(dmg);

        if (enemyFlash != null && (dmg < health.CurrentHealth))
            enemyFlash.Flash();

        if (effectType == "Burn")
        {
            float burnDamage = dmg;

            if (WildfireUpgrade.Instance != null)
            {
                float bonus = WildfireUpgrade.Instance.GetBurnBonus();
                float before = burnDamage;

                burnDamage *= 1f + bonus / 100f;
                Debug.Log($"[Wildfire] Burn tick boosted: +{bonus}% → {before} → {burnDamage}");
            }

            health.ApplyBurn(effectDuration, effectValue, burnDamage);
        }
    }

    public void TakeDamagePercent(float percent)
    {
        if (health == null) return;

        float damageAmount = (percent / 100f) * health.maxHealth;
        health.TakeDmg(damageAmount);

        if (enemyFlash != null && (damageAmount < health.CurrentHealth))
        {
            enemyFlash.Flash();
        }
    }
    #endregion

    #region Navigation
    //Do not use, use movement component instead//
    public virtual void MoveTo(Vector3 destination)
    {
        if (!enableNav || movement == null || IsFrozen || (health != null && health.IsDead)) return;

        movement.MoveTo(destination);
    }
    public virtual void StopMovement()
    {
        if (!enableNav || movement == null) return;
        movement.Stop();
    }
    public virtual void ResumeMovement()
    {
        if (!enableNav || movement == null || IsFrozen || (health != null && health.IsDead)) return;
        movement.SetMovementEnabled(true);
    }

    public virtual void DisableNavigation()
    {
        enableNav = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    }

    public virtual void EnableNavigation()
    {
        enableNav = true;

        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
        }
    }

    #endregion

    // ===============================
    // STATUS EFFECTS FOR UPGRADES
    // ===============================

    // Apply a slow effect
    public void ApplySlow(float duration, float slowMultiplier = 0.5f)
    {
        if (slowCourotine != null) StopCoroutine(slowCourotine);
        slowCourotine = StartCoroutine(SlowRoutine(duration, slowMultiplier));
    }

    public virtual void PlayHitReaction(HitReaction reaction) { }

    private IEnumerator SlowRoutine(float duration, float slowMultiplier)
    {
        IsSlowed = true;
        statusSlowMultiplier = slowMultiplier;   // Missing from EnemBase, this ensures the OverchargeSlowEffect.cs logic is implemented correctly.
        UpdateActualSpeed();

        yield return new WaitForSeconds(duration);

        IsSlowed = false;
        statusSlowMultiplier = 1f;
        UpdateActualSpeed();
        slowCourotine = null;
    }

    // Apply freeze
    public void Freeze(float duration)
    {
        /*
        if (IsFrozen) return;
        StartCoroutine(FreezeRoutine(duration));
        */

        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        IsFrozen = true;
        UpdateActualSpeed();
        StopMovement(); // freeze movement

        yield return new WaitForSeconds(duration);

        //ResumeMovement();

        IsFrozen = false;
        UpdateActualSpeed();

        if (!IsAttacking && (health == null || !health.IsDead))
        {
            ResumeMovement();
        }

        freezeCoroutine = null;
    }

    // Burning DOT is handled by Health, but we track the state here
    public void SetBurning(bool state)
    {
        IsBurning = state;
    }

    public void FlashBurnTick()
    {
        if (enemyFlash != null)
            enemyFlash.Flash();
    }

    // Cleanse all effects (used by Extinguisher, Shatter, etc.)
    public void Cleanse()
    {
        if (slowCourotine != null) StopCoroutine(slowCourotine);
        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);

        IsBurning = false;
        IsFrozen = false;
        IsSlowed = false;

        slowCourotine = null;
        freezeCoroutine = null;

        UpdateActualSpeed();

        if (health == null || !health.IsDead)
        {
            ResumeMovement();
        }
    }

    public void SetEnvironmentalSpeedModifier(float multiplier)
    {
        environmentalSpeedModifier = Mathf.Max(0f, multiplier);
        UpdateActualSpeed();
    }

    // Argument is a MULTIPLIER (0.5 = half speed), stacked multiplicatively.
    // Matches PlayerMovement so a single value behaves identically for both.
    public void AddSpeedModifier(object source, float multiplier)
    {
        if (source == null) return;

        speedModifiers[source] = multiplier;
        RecalculateEnvironmentalSpeed();
    }

    public void RemoveSpeedModifier(object source)
    {
        if (source == null) return;

        if (speedModifiers.Remove(source))
            RecalculateEnvironmentalSpeed();
    }

    private void RecalculateEnvironmentalSpeed()
    {
        float finalMult = 1f;
        foreach (var mult in speedModifiers.Values)
            finalMult *= mult;

        SetEnvironmentalSpeedModifier(finalMult);
    }

    private void UpdateActualSpeed()
    {
        if (agent == null) return;

        if (IsFrozen)
        {
            agent.speed = 0f;
            return;
        }


        float targetSpeed = originalAgentSpeed * statusSlowMultiplier * environmentalSpeedModifier;

        if (!Mathf.Approximately(agent.speed, targetSpeed))
        {
            agent.speed = targetSpeed;
        }

        //Debug.Log($"[EnemyBase] {gameObject.name} envMod={environmentalSpeedModifier} orig={originalAgentSpeed} agent.speed={agent.speed} actualVel={agent.velocity.magnitude}");
    }

    public void ReleaseSlot()
    {
        movement.ReleaseTargetSlot();
    }
    public float MovementSpeed
    {
        get
        {
            if (agent == null || !agent.enabled)
                return 0f;

            if (agent.speed <= 0.001f)
                return 0f;

            return Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
        }
    }
}