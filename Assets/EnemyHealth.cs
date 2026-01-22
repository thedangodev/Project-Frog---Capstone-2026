using UnityEngine;

[RequireComponent(typeof(Healthbar))]
public class EnemyHealth : MonoBehaviour 
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

    public void TakeDamage(float damageAmount)
    {
        if (IsDead) return;

        // Subtract currentHealth by damageAmmount
        currentHealth -= damageAmount;

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
}