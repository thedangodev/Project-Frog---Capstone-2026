using System;
using System.Collections;
using UnityEngine;
using FMODUnity;
using Assets.Scripts.Player;

public class Health : MonoBehaviour, IDamageable
{
    [Header("FMod Events")]
    [SerializeField] private EventReference damageTakenEvent;
    [SerializeField] private EventReference damageTakenNarratedEvent;
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference deathNarrationEvent;

    private Healthbar healthbar;
    private UIPlayerHUD playerHUD;

    public float maxHealth = 100f;

    private float _currentHealth = 100f;

    public bool IsDead { get; private set; }
    public event Action<GameObject> OnDestroyed;

    //Burning DOT
    private bool isBurning;
    private Coroutine burnRoutine;
    private EnemyBase enemy;

    private float deathAnimationDuration = 1.5f;
    private PlayerAnimation playerAnimation;

    private void Awake()
    {
        healthbar = GetComponentInChildren<Healthbar>();
        enemy = GetComponent<EnemyBase>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        if (CompareTag("Player"))
            playerHUD = FindAnyObjectByType<UIPlayerHUD>();

        _currentHealth = maxHealth;
        IsDead = false;
    }

    public event Action<float> OnHealthChanged;

    public float CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            // Clamp value to valid HP
            float clampedValue = Mathf.Clamp(value, 0, maxHealth);

            // Do nothing if health doesn't change
            if (_currentHealth == clampedValue) return;

            _currentHealth = clampedValue;

            // Update UI
            if (healthbar != null)
                healthbar.UpdateHealthBar(maxHealth, _currentHealth);

            if (playerHUD != null)
                playerHUD.UpdateHealth(_currentHealth / maxHealth);

            OnHealthChanged?.Invoke(_currentHealth);
        }
    }

    // ============================================================
    // BASIC DAMAGE
    // ============================================================
    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        Debug.Log($"[Health] {gameObject.name} took {dmg} damage. HP before: {_currentHealth}");
        // Subtract CurrentHealth by damageAmmount
        CurrentHealth -= dmg;
        Debug.Log($"[Health] {gameObject.name} HP after: {CurrentHealth}");

        RuntimeManager.PlayOneShot(damageTakenEvent, transform.position);

        RuntimeManager.PlayOneShot(damageTakenNarratedEvent, transform.position);

        if (CurrentHealth <= 0f)
        {
            Die();

            RuntimeManager.PlayOneShot(deathEvent, transform.position);
            RuntimeManager.PlayOneShot(deathNarrationEvent, transform.position);
        }
    }

    public bool IsMaxHP()
    {
        return CurrentHealth >= maxHealth;
    }

    // ============================================================
    // DAMAGE WITH EFFECT (Burn, Freeze, etc.)
    // ============================================================
    public void TakeDmg(float dmg, string effectType, float effectDuration, float effectValue)
    {
        Debug.Log($"[Health] Damage with effect: {effectType} | Base dmg: {dmg}");
        TakeDmg(dmg);

        if (effectType == "Burn")
        {
            Debug.Log($"[Burn] Applying burn: duration={effectDuration}, tickRate={effectValue}, baseDamage={dmg}");
            ApplyBurn(effectDuration, effectValue, dmg);
        }
        else if (effectType == "Freeze")
        {
            Debug.Log($"[Freeze] Enemy frozen for {effectDuration}s");
            enemy?.Freeze(effectDuration);
        }
        else if (effectType == "Slow")
        {
            Debug.Log($"[Slow] Enemy slowed for {effectDuration}s");
            enemy?.ApplySlow(effectDuration);
        }
    }

    // ============================================================
    // HEALING
    // ============================================================
    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        if (healthbar != null)
            healthbar.UpdateHealthBar(maxHealth, CurrentHealth);
        if (playerHUD != null)
            playerHUD?.UpdateHealth(CurrentHealth / maxHealth);
    }

    // ============================================================
    // DEATH
    // ============================================================
    private void Die()
    {
        IsDead = true;

        if (CompareTag("Player"))
        {
            // Start coroutine for player death anim sequence
            StartCoroutine(PlayerDeathSequence());
        }
        else
        {
            //Debug.Log("Enemy died");
            enemy.ReleaseSlot();
            OnDestroyed?.Invoke(gameObject);

            // Hand off to the fade-out component, which fades opacity then destroys.
            // Fallback: if the prefab is missing the component, destroy immediately so a dead enemy can't linger as an invisible, immortal obstacle.
            var fade = GetComponent<EnemyFadeOut>();
            if (fade != null)
                fade.Die();
            else
                Destroy(gameObject);
        }
    }

    private IEnumerator PlayerDeathSequence()
    {
        playerAnimation.PlayDeath();
        yield return new WaitForSeconds(deathAnimationDuration);

        // Show death overlay
        UIDeathOverlay deathOverlay = FindFirstObjectByType<UIDeathOverlay>();
        if (deathOverlay != null)
            deathOverlay.ShowDeathOverlay();
        else
            //Debug.LogError("No PlayerDeathOverlay found in scene.");

            gameObject.SetActive(false);
    }

    // ============================================================
    // BURN LOGIC (Wildfire integrated)
    // ============================================================
    public void ApplyBurn(float duration, float tickRate, float baseDamage)
    {
        if (burnRoutine != null)
            StopCoroutine(burnRoutine);

        burnRoutine = StartCoroutine(BurnRoutine(duration, tickRate, baseDamage));
    }

    private IEnumerator BurnRoutine(float duration, float tickRate, float baseDamage)
    {
        isBurning = true;
        enemy?.SetBurning(true);

        float timer = 0f;

        while (timer < duration)
        {
            float finalTickDamage = baseDamage * 0.5f;
            Debug.Log($"[Burn] Tick damage: {finalTickDamage} | Timer: {timer}/{duration}");

            TakeDmg(finalTickDamage);

            enemy?.FlashBurnTick();

            timer += tickRate;
            yield return new WaitForSeconds(tickRate);
        }

        isBurning = false;
        enemy?.SetBurning(false);
    }
}