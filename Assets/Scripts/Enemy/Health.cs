using UnityEngine;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(Healthbar))]
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    private Healthbar healthbar;

    public bool IsDead { get; private set; }

    private TopDownControllerWithDash controller;

    private void Awake()
    {
        healthbar = GetComponent<Healthbar>();
        currentHealth = maxHealth;
        IsDead = false;

        controller = GetComponent<TopDownControllerWithDash>();
    }



    public void TakeDmg(float dmg)
    {
        if (IsDead) return;

        // IMMORTAL MODE controlled by player controller
        if (controller != null && controller.IsImmortal)
            return;

        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        healthbar.UpdateHealthBar(maxHealth, currentHealth);

        if (currentHealth == 0f)
            Die();
    }



    private void Die()
    {
        IsDead = true;
        Destroy(gameObject);
    }
}