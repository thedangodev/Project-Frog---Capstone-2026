using UnityEngine;
using UnityEngine.InputSystem;

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

    private PlayerInputActions input;
    private float chargeTimer;
    private bool isCharging;
    [SerializeField] private FrogTongue frogTongue;

    private void Awake()
    {
        input = new PlayerInputActions();

        input.Player.Shoot.started += ctx => StartCharging();
        input.Player.Shoot.canceled += ctx => ReleaseCharging();

        if (frogTongue == null)
        {
            frogTongue = GetComponentInChildren<FrogTongue>();
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

    private void OnEnable()
    {
        if (frogTongue != null)
        {
            input.Player.Tongue.started += ctx => frogTongue.BeginTongue();
            input.Player.Tongue.canceled += ctx => frogTongue.EndTongue();
        }

        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    void Update()
    {
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
