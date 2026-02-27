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

    [Header("Tongue Attack")]
    [SerializeField] private FrogTongue frogTongue;

    private bool isCharging = false;
    private float chargeTimer = 0f;

    private void Awake()
    {
        if (frogTongue == null)
        {
            frogTongue = GetComponentInChildren<FrogTongue>();
            if (frogTongue == null)
                Debug.LogError("FrogTongue reference not found on this GameObject");
        }
    }

    private void Update()
    {
        HandleProjectileInput();
        HandleTongueInput();

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);
        }
    }

    private void HandleProjectileInput()
    {
        if (Input.GetButtonDown("Fire1"))
            StartCharging();

        if (Input.GetButtonUp("Fire1"))
            ReleaseCharging();
    }

    private void HandleTongueInput()
    {
        if (frogTongue == null) return;

        if (Input.GetButtonDown("Fire2"))
            frogTongue.BeginTongue();

        if (Input.GetButtonUp("Fire2"))
            frogTongue.EndTongue();
    }

    private bool CanShoot()
    {
        if (frogTongue == null)
            return true;

        return !frogTongue.extending && !frogTongue.retracting;
    }

    private void StartCharging()
    {
        if (!CanShoot()) return;

        isCharging = true;
        chargeTimer = minChargeTime;
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
            projectile.Initialize(chargePercent);
    }
}