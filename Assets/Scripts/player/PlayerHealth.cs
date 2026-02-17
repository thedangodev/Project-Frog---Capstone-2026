using UnityEngine;

[RequireComponent(typeof(Healthbar))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private Healthbar healthbar;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        healthbar = GetComponent<Healthbar>();
        currentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        // Subtract currentHealth by damageAmmount
        currentHealth -= dmg;

        // Ensure currentHealth stays between 0 and maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Update healthbar visual
        healthbar.UpdateHealthBar(maxHealth, currentHealth);

        if (currentHealth == 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
public void Heal(float amount)
{
    if (IsDead) return;

    currentHealth += amount;
    currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

    healthbar.UpdateHealthBar(maxHealth, currentHealth);
}
public void TongueHeal(float amount)
{
    if (IsDead) return;

    currentHealth += amount;

    // Make sure health does not exceed maxHealth
    currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

    // Update the health bar UI
    healthbar.UpdateHealthBar(maxHealth, currentHealth);
}

}