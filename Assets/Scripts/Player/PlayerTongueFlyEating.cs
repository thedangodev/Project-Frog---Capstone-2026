using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerTongueFlyEating : MonoBehaviour
{
    [SerializeField] private float healAmountPerFly;
    
    private PlayerTongueAttack tongue;
    private Health playerHealth;

    private int numberOfFliesAttached = 0; // Fly counter for how many times the player heals when retracting

    private void Awake()
    {
        tongue = GetComponent<PlayerTongueAttack>();
        playerHealth = GetComponent<Health>();

        // Subscribe to the tongue's finish event so we can heal after retraction
        tongue.OnTongueFinished += HealPlayer;
    }

    private void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        tongue.OnTongueFinished -= HealPlayer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (tongue.IsActive && other.CompareTag("Fly"))
        {
            AttachFly();
            Destroy(other.gameObject);
        }
    }

    private void AttachFly()
    {
        numberOfFliesAttached++;
    }

    private void HealPlayer()
    {
        playerHealth.Heal(healAmountPerFly * numberOfFliesAttached);

        // Reset fly counter
        numberOfFliesAttached = 0;
    }
}