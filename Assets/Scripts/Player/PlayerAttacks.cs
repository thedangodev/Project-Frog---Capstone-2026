using Assets.Scripts.Player;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerTongueAttack))]
public class PlayerAttacks : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Attack Settings")]
    [SerializeField] public float attacksPerSecond = 2f;
    [SerializeField] private float attackWindowDuration = 0.5f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float tongueAutoAimRange = 10f;
    [SerializeField] private float basicShotSlowMultiplier = 0.5f;
    [SerializeField] private float basicShotSlowDuration = 0.5f;

    [Header("Wind Upgrades")]
    [SerializeField] public float pointBlankRange = 10f;

    [Header("Aiming Correction")]
    [SerializeField] private float aimCorrectionStrength = 1.0f;
    [SerializeField] private float targetHeightOffset = 1.0f;

    [Header("FMod Events")]
    [SerializeField] private EventReference basicShotEvent;
    [SerializeField] private EventReference basicShotEvent2;
    [SerializeField] private EventReference chargeShotEvent;

    public event System.Action<bool> OnShotFired;

    [Header("Animation Timing")]
    [SerializeField] private float attackWindupTime = 0.5f;   // time before projectile fires
    [SerializeField] private float attackRecoveryTime = 0.2f; // optional recovery window

    //public bool isTethered;
    public float LastChargeValue { get; private set; }
    public event System.Action<float> OnChargeShotFired;

    private float fireCooldown => 1f / attacksPerSecond;
    private float lastFireTime = -999f;
    private float basicShotSlowTimer = 0f;
    private float chargeTimer;
    private bool isCharging;
    private float attackWindowTimer;

    private Camera mainCamera;
    private PlayerMovement playerMovement;
    private PlayerTongueAttack playerTongueAttack;
    private PlayerChargeAttack playerChargeAttack;
    private PlayerAnchor playerAnchor;
    private PlayerInput playerInput;
    private PlayerCrosshair playerCrosshair;
    private PlayerAnimation playerAnimation;

    // Input actions
    private InputAction attackAction;          // Fire1 → LMB / Right Trigger
    private InputAction secondaryAttackAction; // Fire2 → RMB / Left Trigger
    private InputAction aimAction;             // Mouse position / Right Stick

    // map tracking + trigger fallbacks
    private string currentActionMapName;
    private float prevAttackValue;
    private float prevSecondaryValue;
    private const float triggerThreshold = 0.5f;

    private bool isBasicShotSlowed = false;
    private bool pendingSecondaryRelease = false;
    public bool IsAttacking => isCharging || playerTongueAttack.IsActive || attackWindowTimer > 0f;
    public bool isFiringPrime => playerAnimation != null && playerAnimation.isHoldingPrimaryAttack;

    private void Awake()
    {
        mainCamera = Camera.main;
        playerMovement = GetComponent<PlayerMovement>();
        playerTongueAttack = GetComponentInChildren<PlayerTongueAttack>();
        playerChargeAttack = GetComponent<PlayerChargeAttack>();
        playerAnchor = GetComponent<PlayerAnchor>();
        playerCrosshair = FindAnyObjectByType<PlayerCrosshair>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        if (playerAnimation != null )
        {
            playerAnimation.OnPrimeProjectileSpawn.AddListener(FirePrimaryProjectile);
            playerAnimation.OnSecProjectileSpawn.AddListener(FireSecondaryProjectile);
            playerAnimation.OnTongueRelease.AddListener(ReleaseTongue);
        }

        playerTongueAttack.OnTongueFinished += playerMovement.ResumeMovement;

        playerInput = GetComponent<PlayerInput>();
        Debug.Assert(playerInput != null, $"[{gameObject.name}] missing PlayerInput!", this);

        RebindActionsFromCurrentMap();
    }

    private void OnDestroy()
    {
        if (playerTongueAttack != null)
            playerTongueAttack.OnTongueFinished -= playerMovement.ResumeMovement;

        if (playerAnimation != null)
        {
            playerAnimation.OnPrimeProjectileSpawn.RemoveListener(FirePrimaryProjectile);
            playerAnimation.OnSecProjectileSpawn.RemoveListener(FireSecondaryProjectile);
            playerAnimation.OnTongueRelease.RemoveListener(ReleaseTongue);
        }
    }

    // ============================================================
    // INPUT HANDLING
    // ============================================================
    private void RebindActionsFromCurrentMap()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
            return;

        currentActionMapName = playerInput.currentActionMap.name;

        attackAction = playerInput.currentActionMap.FindAction("Attack");
        secondaryAttackAction = playerInput.currentActionMap.FindAction("SecondaryAttack");
        aimAction = playerInput.currentActionMap.FindAction("Look");

        Debug.Assert(attackAction != null, $"[{gameObject.name}] Attack action not found on map {currentActionMapName}!", this);
        Debug.Assert(secondaryAttackAction != null, $"[{gameObject.name}] SecondaryAttack action not found on map {currentActionMapName}!", this);
        Debug.Assert(aimAction != null, $"[{gameObject.name}] Look action not found on map {currentActionMapName}!", this);

        // reset previous values for trigger fallbacks
        prevAttackValue = 0f;
        prevSecondaryValue = 0f;
    }

    private void Update()
    {
        // return early to prevent projectile firing in UI
        if (Time.timeScale == 0f)
            return;

        if (playerInput != null &&
            playerInput.currentActionMap != null &&
            playerInput.currentActionMap.name != currentActionMapName)
        {
            RebindActionsFromCurrentMap();
        }

        bool attackHeld = ReadButton(attackAction, ref prevAttackValue, out bool attackPressed);
        bool secondaryHeld = ReadButton(secondaryAttackAction, ref prevSecondaryValue, out bool secondaryPressed, out bool secondaryReleased);

        // PRIMARY ATTACK - Block if dashing
        if (attackHeld && !playerMovement.IsDashing)
        {
            TryBasicShot();
            ApplyBasicShotSlow();
        }
        else if (!attackHeld && playerAnimation != null)
        {
            playerAnimation.StopPrimaryAttack();
        }

        // Tick down basic shot slow timer
        if (isBasicShotSlowed)
        {
            basicShotSlowTimer -= Time.deltaTime;

            if (basicShotSlowTimer <= 0f)
            {
                isBasicShotSlowed = false;
                playerMovement.RemoveSpeedModifier(this);
            }
        }

        // SECONDARY ATTACK - Block if dashing
        if (!playerMovement.IsDashing)
        {
            if (playerAnchor.IsTethered)
            {
                if (secondaryPressed && !playerChargeAttack.IsCharging)
                {
                    playerMovement.StopMovement(GetAimDirection());
                    playerChargeAttack.BeginCharge(playerAnchor.AttachedAnchor);
                    playerAnimation.PlaySecondaryAttack();
                }

                if (secondaryHeld)
                {
                    playerChargeAttack.UpdateCharge();
                    
                }


                if (secondaryReleased)
                {
                    //playerChargeAttack.ReleaseCharge(firePoint.position, GetAimDirection());
                    //OnShotFired?.Invoke(true);
                    //RuntimeManager.PlayOneShot(chargeShotEvent, transform.position);

                    //playerMovement.ResumeMovement();
                    //playerAnimation.StopSecondaryAttack();
                    //FireSecondaryProjectile();
                    pendingSecondaryRelease = true;
                }
            }
            else
            {
                if (secondaryPressed)
                    TryTongue();

                if (secondaryReleased)
                    playerTongueAttack.BeginTongueRetract();
            }
        }

        if (isCharging)
            chargeTimer = Mathf.Clamp(chargeTimer + Time.deltaTime, 0f, maxChargeTime);

        if (attackWindowTimer > 0f)
        {
            attackWindowTimer = Mathf.Max(0f, attackWindowTimer - Time.deltaTime);
            if (attackWindowTimer == 0f)
                playerMovement.ResumeMovement();
        }
    }

    // ============================================================
    // PRIMARY ATTACK
    // ============================================================
    private void TryBasicShot()
    {
        if (playerTongueAttack.IsActive) return;
        Vector3 aimDirection = GetAimDirection();

        if (playerAnimation != null && playerAnimation.isHoldingPrimaryAttack)
        {
            // make player face direction of fire while holding shoot input
            playerMovement.FaceDirection(aimDirection);
            if (playerAnimation.PausedThisFrame)
                return;
            if (Time.time >= lastFireTime + fireCooldown)
            {
                FirePrimaryProjectile();
            }
            return;
        }

        if (Time.time < lastFireTime + fireCooldown) return;

        attackWindowTimer = attackWindowDuration;
        //OnShotFired?.Invoke(false);
        ApplyBasicShotSlow();

        //Shoot(0f, aimDirection); // now handled in FirePrimaryProjectile method
        lastFireTime = Time.time;

        playerMovement.FaceDirection(aimDirection);
        playerAnimation.PlayPrimaryAttack();

        //RuntimeManager.PlayOneShot(basicShotEvent, transform.position);
    }


    /// <summary>
    /// Those should be the same as TryBasicShot, but with a coroutine to handle windup and recovery times for animation timing.
    /// </summary>

    //private void TryBasicShot()
    //{
    //    if (playerTongueAttack.IsActive) return;
    //    if (Time.time < lastFireTime + fireCooldown) return;

    //    StartCoroutine(PerformBasicShot());
    //}

    //private IEnumerator PerformBasicShot()
    //{
    //    Vector3 aimDirection = GetAimDirection();
    //    attackWindowTimer = attackWindowDuration;

    //    // Insert Begin attack animation
    //    // animator.SetTrigger("AttackStart");

    //    // Apply slowdown immediately
    //    ApplyBasicShotSlow();

    //    // Wind-up delay (animation timing)
    //    yield return new WaitForSeconds(attackWindupTime);

    //    // Fire projectile
    //    Shoot(0f, aimDirection);
    //    lastFireTime = Time.time;

    //    RuntimeManager.PlayOneShot(basicShotEvent, transform.position);

    //    // InsertEnd attack animation
    //    // animator.SetTrigger("AttackEnd");

    //    // Recovery delay (animation timing)
    //    if (attackRecoveryTime > 0f)
    //        yield return new WaitForSeconds(attackRecoveryTime);
    //}

    private void ApplyBasicShotSlow()
    {
        // Only refresh timer if not already slowed
        if (!isBasicShotSlowed)
        {
            playerMovement.AddSpeedModifier(this, basicShotSlowMultiplier);
            isBasicShotSlowed = true;
        }

        //basicShotSlowTimer = Mathf.Max(basicShotSlowTimer, basicShotSlowDuration);
        basicShotSlowTimer = basicShotSlowDuration;
    }
    public bool IsPrimaryInputHeld()
    {
        if (attackAction == null) return false;
        bool held = attackAction.IsPressed();
        float val = 0f;
        try { val = attackAction.ReadValue<float>(); } catch { }
        if (!held && val >= triggerThreshold)
            held = true;
        return held;
    }

    private void Shoot(float chargePercent, Vector3? direction = null)
    {
        Vector3 finalDirection;
        
        if (direction.HasValue && direction.Value != Vector3.zero)
        {
            finalDirection = direction.Value;
        }
        else
        {
            finalDirection = GetAimDirection();
        }

        // Apply perspective correction
        finalDirection = ApplyPerspectiveCorrection(finalDirection);

        // ---------------------------------------------------------
        // WIND PRIMARY MULTISHOT
        // ---------------------------------------------------------
        if (playerAnchor != null && playerAnchor.IsTethered && playerAnchor.AttachedAnchor.Element == AnchorElement.Wind)
        {
            int baseProjectiles = 4;
            int extra = MultishotUpgrade.Instance != null ? MultishotUpgrade.Instance.GetExtraDarts() : 0;
            int totalProjectiles = baseProjectiles + extra;

            float spreadAngle = 5f;
            List<GameObject> spawnedProjectiles = new List<GameObject>();

            for (int i = 0; i < totalProjectiles; i++)
            {
                float angle = spreadAngle * (i - totalProjectiles / 2f);
                Vector3 spreadDir = Quaternion.Euler(0, angle, 0) * finalDirection;
                Vector3 spawnPos = firePoint.position + spreadDir * 0.3f;

                GameObject obj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(spreadDir));
                var projMulti = obj.GetComponent<Projectile>();

                if (projMulti != null)
                {
                    projMulti.isPlayerProjectile = true;
                    projMulti.player = this.gameObject;                   
                    projMulti.currentElement = AnchorElement.Wind;         
                    projMulti.pointBlankRange = pointBlankRange;
                    projMulti.Initialize(chargePercent);
                }

                IgnorePlayerCollision(obj);

                foreach (var other in spawnedProjectiles)
                {
                    Collider[] colsA = obj.GetComponentsInChildren<Collider>();
                    Collider[] colsB = other.GetComponentsInChildren<Collider>();
                    foreach (var c1 in colsA)
                        foreach (var c2 in colsB)
                            Physics.IgnoreCollision(c1, c2);
                }

                spawnedProjectiles.Add(obj);
            }

            return;
        }

        // ---------------------------------------------------------
        // NORMAL SINGLE SHOT
        // ---------------------------------------------------------

        Quaternion rotation = Quaternion.LookRotation(finalDirection);
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, rotation);

        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            // CRITICAL: Set isPlayerProjectile FIRST before anything else
            proj.isPlayerProjectile = true;
            proj.player = this.gameObject;                                
            proj.currentElement = playerAnchor.IsTethered
                ? playerAnchor.AttachedAnchor.Element
                : AnchorElement.Broken;

            proj.pointBlankRange = pointBlankRange;
            proj.Initialize(chargePercent);
            //proj.damage = 2f;
        }

        // -----------------
        // PRIMARY FIRE BURN
        // -----------------
        if (proj.currentElement == AnchorElement.Fire)
        {
            var fireData = playerAnchor.AttachedAnchor.BaseData as AnchorFireData;
            if (fireData != null)
            {
                proj.effectType = "Burn";
                proj.effectDuration = fireData.BurnDuration;
                proj.effectValue = fireData.BurnTickRate;
            }
        }

        // Always ignore collision with player, regardless of Projectile component
        IgnorePlayerCollision(projObj);
    }

    private void FirePrimaryProjectile()
    {
        Vector3 aimDirection = GetAimDirection();
        Shoot(0f, aimDirection);
        OnShotFired?.Invoke(false);
        RuntimeManager.PlayOneShot(basicShotEvent, transform.position);
        RuntimeManager.PlayOneShot(basicShotEvent2, transform.position);
        lastFireTime = Time.time;
    }

    private void FireSecondaryProjectile()
    {
        if (playerChargeAttack != null && playerChargeAttack.IsCharging)
        {
            playerChargeAttack.ReleaseCharge(firePoint.position, GetAimDirection());
            OnShotFired?.Invoke(true);
            RuntimeManager.PlayOneShot(chargeShotEvent, transform.position);
            lastFireTime = Time.time;

            
        }
        else if (pendingSecondaryRelease)
        {
            playerChargeAttack?.CancelCharge();
        }
        playerMovement?.ResumeMovement();
        playerAnimation?.StopSecondaryAttack();

        pendingSecondaryRelease = false;

    }

    private void ReleaseTongue()
    {
        // animation event fires tongue at the right frame of animation
        if (playerTongueAttack != null)
        {
            playerTongueAttack.BeginTongueExtend();
        }
    }

    private Vector3 ApplyPerspectiveCorrection(Vector3 baseDirection)
    {
        if (playerCrosshair == null || !playerCrosshair.HasValidWorldTarget())
            return baseDirection;

        Vector3 worldTarget = playerCrosshair.GetWorldTargetPosition();
        
        // Add height offset to aim at enemy center mass instead of feet
        worldTarget.y += targetHeightOffset;

        // Calculate corrected direction from fire point to adjusted target
        Vector3 correctedDirection = (worldTarget - firePoint.position).normalized;

        // Blend between base direction and corrected direction
        Vector3 finalDirection = Vector3.Lerp(baseDirection, correctedDirection, aimCorrectionStrength);


        return finalDirection;
    }

    private void IgnorePlayerCollision(GameObject projObj)
    {
        Collider[] projCols = projObj.GetComponentsInChildren<Collider>();
        Collider[] playerCols = GetComponentsInChildren<Collider>();

        foreach (var pCol in projCols)
        {
            foreach (var col in playerCols)
            {
                Physics.IgnoreCollision(pCol, col);
            }
        }
    }

    // ============================================================
    // TONGUE ATTACK
    // ============================================================
    private void TryTongue()
    {
        if (isCharging) return;
        if (playerTongueAttack.IsActive) return;

        // play tongue attack animation
        playerAnimation.PlayTongueAttack();

        Vector3 aimDirection = GetTongueAimDirection();
        playerMovement.StopMovement(aimDirection);
        transform.rotation = Quaternion.LookRotation(aimDirection);
    }

    private Vector3 GetTongueAimDirection()
    {
        GameObject[] flies = GameObject.FindGameObjectsWithTag("Fly");

        Transform closestFly = null;
        float closestDistSqr = float.MaxValue;

        foreach (var flyObj in flies)
        {
            Vector3 toFly = flyObj.transform.position - transform.position;

            // ignore height difference
            toFly.y = 0f;

            float distSqr = toFly.sqrMagnitude;

            // must be inside auto-aim range
            if (distSqr > tongueAutoAimRange * tongueAutoAimRange)
                continue;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestFly = flyObj.transform;
            }
        }

        // If a fly is found → aim at it
        if (closestFly != null)
        {
            Vector3 dir = closestFly.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                return dir.normalized;
        }

        // fallback
        return GetAimDirection();
    }

    // ============================================================
    // INPUT HELPERS
    // ============================================================
    private bool ReadButton(InputAction action, ref float prevValue, out bool pressed)
    {
        return ReadButton(action, ref prevValue, out pressed, out _);
    }

    private bool ReadButton(InputAction action, ref float prevValue, out bool pressed, out bool released)
    {
        pressed = false;
        released = false;

        if (action == null)
            return false;

        bool held = action.IsPressed();
        pressed = action.WasPressedThisFrame();
        released = action.WasReleasedThisFrame();

        float val = 0f;
        try { val = action.ReadValue<float>(); } catch { }

        if (!held && val >= triggerThreshold)
            held = true;

        if (!pressed && val >= triggerThreshold && prevValue < triggerThreshold)
            pressed = true;

        if (!released && val < triggerThreshold && prevValue >= triggerThreshold)
            released = true;

        prevValue = val;
        return held;
    }

    // ============================================================
    // AIMING
    // ============================================================
    private Vector3 GetAimDirection()
    {
        if (playerCrosshair != null && playerCrosshair.HasValidWorldTarget())
        {
            Vector3 targetPos = playerCrosshair.GetWorldTargetPosition();
            Vector3 direction = targetPos - firePoint.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;
        }

        Vector2 aimValue = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        
        if (InputManager.Instance != null && InputManager.Instance.IsUsingGamepad())
        {
            if (aimValue.sqrMagnitude > 0.01f)
            {
                if (playerCrosshair != null)
                    playerCrosshair.UpdateControllerLook(aimValue);
                return new Vector3(aimValue.x, 0f, aimValue.y).normalized;
            }
        }

        else
        {
            if (Mouse.current == null)
                return transform.forward;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane plane = new Plane(Vector3.up, transform.position);

            if (plane.Raycast(ray, out float dist))
            {
                Vector3 dir = ray.GetPoint(dist) - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    return dir.normalized;
            }
        }

        return transform.forward;
    }
}