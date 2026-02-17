using UnityEngine;

public class TongueHit : MonoBehaviour
{
    [SerializeField] private float healAmount = 20f;

    private FrogTongue tongue;
    private PlayerHealth playerHealth;
    private bool hasHit = false;

    private void Awake()
    {
        tongue = GetComponentInParent<FrogTongue>();
        playerHealth = FindObjectOfType<PlayerHealth>(); // player health
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Example: enemy has Health
        Health enemyHealth = other.GetComponent<Health>();

        if (enemyHealth != null && enemyHealth != playerHealth)
        {
            hasHit = true;

            // Damage enemy
            enemyHealth.TakeDmg(25f);

            // Heal player
            playerHealth.Heal(healAmount);

            // Retract tongue immediately
            tongue.EndTongue();
        }
    }

    private void OnEnable()
    {
        hasHit = false;
    }
}