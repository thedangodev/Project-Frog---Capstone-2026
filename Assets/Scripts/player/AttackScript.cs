using UnityEngine;

public class AttackScript : MonoBehaviour
{
    [Header("Charge Settings")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float minChargeTime = 0f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldown = 0.5f;
    private float lastFireTime = -999f;

    private float chargeTimer;
    private bool isCharging;

    [SerializeField] private PlayerTongueAttack frogTongue;

    private void Awake()
    {
        if (frogTongue == null)
        {
            frogTongue = GetComponentInChildren<PlayerTongueAttack>();
            if (frogTongue == null)
            {
                Debug.LogError("FrogTongue reference not found on this GameObject");
            }
        }
    }

    private bool CanShoot()
    {
        if (frogTongue == null)
            return true;

        return !frogTongue.extending && !frogTongue.retracting;
    }

    void Update()
    {
        // --- SHOOT INPUT ---
        if (Input.GetButtonDown("Fire1"))
        {
            StartCharging();
        }

        if (Input.GetButtonUp("Fire1"))
        {
            ReleaseCharging();
        }

        // --- TONGUE INPUT ---
        if (frogTongue != null)
        {
            if (Input.GetButtonDown("Fire2"))
            {
                frogTongue.BeginTongue();
            }

            if (Input.GetButtonUp("Fire2"))
            {
                frogTongue.EndTongue();
            }
        }

        // --- CHARGING LOGIC ---
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);
        }
    }

    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = minChargeTime;

        if (!CanShoot()) return;
    }

    private void ReleaseCharging()
    {
        if (!isCharging) return;
        if (!CanShoot()) return;
        if (Time.time < lastFireTime + fireCooldown) return;

        isCharging = false;

        float chargePercent = Mathf.Clamp01(chargeTimer / maxChargeTime);

        FireProjectile(chargePercent);

        lastFireTime = Time.time;
    }

    private void FireProjectile(float chargePercent)
    {
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(chargePercent);
        }
    }
}