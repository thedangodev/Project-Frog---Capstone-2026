using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using Assets.Scripts.Player;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerAnchor))]
public class PlayerMovement : MonoBehaviour, IMovement
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private string hitBoxName = "Hitbox";
    [SerializeField] private float inputSmoothSpeed = 20f;
    [SerializeField] private float panDashLockTimer;
    private Dictionary<object, float> speedModifiers = new Dictionary<object, float>();
    private float CurrentSpeed
    {
        get
        {
            float finalMult = 1f;
            foreach (var mult in speedModifiers.Values)
                finalMult *= mult;
            return moveSpeed * finalMult;
        }
    }

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private ParticleSystem dashEffect;
    [SerializeField] private float dashEffectBackOffset = 1f;
    [SerializeField] private float dashEffectHeightOffset = 0.5f;


    [Header("FMod Events")]
    //[SerializeField] private EventReference fireAnchorEvent;
    //[SerializeField] private EventReference iceAnchorEvent;
    //[SerializeField] private EventReference windAnchorEvent;
    [SerializeField] private EventReference dashActivationEvent;
    [SerializeField] private EventReference voicedDashActivationEvent;

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction dashAction;

    private bool usingGamepad;
    private bool isSubToInputManager;

    private Rigidbody rb;

    private PlayerAnchor playerAnchor;
    private UIPlayerHUD playerHUD;
    private CapsuleCollider capsuleCollider;
    private PlayerCrosshair playerCrosshair;
    private PlayerAttacks playerAttacks;

    private Vector3 moveInput;
    private Vector3 dashDirection;
    private Vector3 lookDirection;

    private bool isDashing;
    private bool isInMud = false;
    private bool isMovementStopped;
    private bool isTethered;
    private bool movementStoppedExternally;

    private float dashTimer;
    private float dashCooldownTimer;

    // Tether-break stun state. Independent of the StopMovement/ResumeMovement external lock so the two systems compose instead of stomping each other. Set by TetherDamageDealer on a Golem break.
    private float stunTimer;
    private bool isStunned;

    public bool IsDashing => isDashing;
    public float DashCooldownProgress => dashCooldownTimer > 0f ? 1f - (dashCooldownTimer / dashCooldown) : 1f;

    private float currentMaxRadius;
    private Vector3 anchorPosition;
    private readonly float currentMinRadius = 4f;

    private const string GamepadSchemeNameLower = "gamepad";
    private PlayerAnimation playerAnimation;

    private void Awake()
    {
        Transform hitBox = transform.Find(hitBoxName);
        capsuleCollider = hitBox.GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        playerAnchor = GetComponent<PlayerAnchor>();
        playerHUD = FindAnyObjectByType<UIPlayerHUD>();
        playerCrosshair = FindAnyObjectByType<PlayerCrosshair>();
        playerAttacks = GetComponent<PlayerAttacks>();

        lookDirection = transform.forward;

        playerInput = GetComponent<PlayerInput>();
        playerAnimation = GetComponentInChildren<PlayerAnimation>();

        // Enable PlayerMK by default, will switch to Gamepad if input is detected
        foreach (var map in playerInput.actions.actionMaps)
            map.Disable();

        playerInput.SwitchCurrentActionMap("PlayerMK");
        SetActionMap("PlayerMK");
        usingGamepad = false;

        SubToInputManager();
    }

    private void OnEnable()
    {
        SubToInputManager();
    }

    private void OnDisable()
    {
        UnsubFromInputManager();

    }

    private void OnDestroy()
    {
        UnsubFromInputManager();
    }

    private void SubToInputManager()
    {
        if (InputManager.Instance != null && !isSubToInputManager)
        {
            InputManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;
            isSubToInputManager = true;
            SyncWithInputManager();
        }
    }

    private void UnsubFromInputManager()
    {
        if (InputManager.Instance != null && isSubToInputManager)
        {
            InputManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
            isSubToInputManager = false;
        }
    }

    private void OnInputDeviceChanged(InputManager.InputDevice newDevice)
    {
        bool shouldUseGamepad = (newDevice == InputManager.InputDevice.Gamepad);
        SwitchInputMode(shouldUseGamepad);

    }

    private void SyncWithInputManager()
    {
        if (InputManager.Instance == null)
            return;

        bool shouldUseGamepad = InputManager.Instance.IsUsingGamepad();
        SwitchInputMode(shouldUseGamepad);
    }

    private void SwitchInputMode(bool shouldUseGamepad)
    {
        if (shouldUseGamepad == usingGamepad)
            return;

        usingGamepad = shouldUseGamepad;
        string targetMap = usingGamepad ? "PlayerGamepad" : "PlayerMK";

        playerInput.SwitchCurrentActionMap(targetMap);
        SetActionMap(targetMap);

        //Debug.Log($"[PlayerMovement] Switched to {targetMap}");
    }

    private void SetActionMap(string mapName)
    {
        var map = playerInput.actions.FindActionMap(mapName);
        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        dashAction = map.FindAction("Dash");
    }
    public float speed;
    public float GetMovementSpeed()
    {
        // No movement allowed.
        if (isDashing ||
            isMovementStopped ||
            movementStoppedExternally ||
            isStunned)
        {
            speed = 0f;
            return speed;
        }

        // Read the SAME Move input used by PlayerMovement.
        Vector2 move = moveAction.ReadValue<Vector2>();

        // Nothing pressed = 0.
        if (move == Vector2.zero)
        {
            speed = 0f;
            return speed;
        }

        // WASD or controller is being pressed.
        speed = Mathf.Clamp01(move.magnitude);

        return speed;
    }
    private void Update()
    {
        if (CameraPanEffect.GlobalPanActive)
        {
            // While panning, always keep the lock at 5 seconds.
            panDashLockTimer = 1;

        }
        else if (panDashLockTimer > 0f)
        {
            panDashLockTimer = panDashLockTimer - 0.5f;

        }

        UpdateTetherStatus();

        // Update dash cooldown
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        float progress = 1f - (dashCooldownTimer / dashCooldown);
        playerHUD?.UpdateDashCooldown(progress);

        // Tick down the tether-break stun.
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                stunTimer = 0f;
            }
        }
        if (!isDashing && dashCooldownTimer <= 0f && dashAction.WasPressedThisFrame() && !isInMud && !CameraPanEffect.GlobalPanActive && panDashLockTimer <= 0f)
            StartDash();

        if (isMovementStopped || movementStoppedExternally || isStunned || CameraPanEffect.GlobalPanActive)
            return;

        // While dashing, ignore movement input and lock rotation to the dash direction.
        // This prevents the dash from being cancelled or redirected by new input.
        if (isDashing)
        {
            if (dashDirection.sqrMagnitude > 0.0001f)
                rb.MoveRotation(Quaternion.LookRotation(dashDirection));
            return;
        }

        // READ INPUT
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector3 rawInput = new Vector3(move.x, 0f, move.y);

        // Clamp magnitude to 1 (keyboard diagonals can exceed it) but preserve analog range for controllers
        if (rawInput.sqrMagnitude > 1f)
            rawInput.Normalize();
        Vector3 targetInput = rawInput.sqrMagnitude > 0.001f ? rawInput : Vector3.zero;

        // Smooth input to prevent analog stick jitter from causing dead-stops
        moveInput = Vector3.Lerp(moveInput, targetInput, Time.deltaTime * inputSmoothSpeed);

        //READ LOOK INPUT
        Vector2 look = lookAction.ReadValue<Vector2>();

        // SEND LOOK INPUT TO CROSSHAIR
        if (playerCrosshair != null)
        {
            playerCrosshair.SetControllerMode(usingGamepad);
            playerCrosshair.UpdateControllerLook(look);
        }

        if (usingGamepad && playerCrosshair != null)
        {
            Vector3 dir = playerCrosshair.GetLookDirection();

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.y = 0f;
                lookDirection = dir.normalized;
                bool isFiring = playerAttacks != null && playerAttacks.isFiringPrime;
                if (isFiring)
                {
                    rb.MoveRotation(Quaternion.LookRotation(lookDirection));
                }
            }
        }


    }

    private void FixedUpdate()
    {
        GetMovementSpeed();
        if (isMovementStopped || movementStoppedExternally || isStunned || CameraPanEffect.GlobalPanActive)
        {
            rb.MoveRotation(Quaternion.LookRotation(lookDirection));
            return;
        }

        Vector3 moveVector;

        if (isDashing)
        {
            moveVector = dashDirection * (dashDistance / dashDuration) * Time.fixedDeltaTime;
            dashTimer -= Time.fixedDeltaTime;

            CollisionUtility.MoveWithCapsuleCollision(
                rb,
                capsuleCollider,
                moveVector,
                collisionLayers
            );

            if (dashTimer <= 0f)
                EndDash();
        }
        else
        {
            moveVector = moveInput * CurrentSpeed * Time.fixedDeltaTime;
            //moveVector = ClampToShrinkingAnchorWall(rb.position, moveVector);

            CollisionUtility.MoveWithCapsuleCollision(
                rb,
                capsuleCollider,
                moveVector,
                collisionLayers
            );

            // Don't let walk-facing override the aim-facing during an attack window.
            bool suppressWalkRotation = playerAttacks != null && (playerAttacks.IsAttacking || playerAttacks.isFiringPrime);

            if (!usingGamepad && moveInput.sqrMagnitude > 0.0001f && !suppressWalkRotation)
                rb.MoveRotation(Quaternion.LookRotation(moveInput.normalized));
        }
    }
    private void UpdateTetherStatus()
    {
        if (playerAnchor != null)
            isTethered = playerAnchor.IsTethered;

        if (isTethered && playerAnchor.CurrentAnchor != null)
        {
            anchorPosition = playerAnchor.CurrentAnchor.transform.position;
            float distanceToAnchor = Vector3.Distance(rb.position, anchorPosition);
            if (currentMaxRadius == 0f || distanceToAnchor < currentMaxRadius)
                currentMaxRadius = Mathf.Max(distanceToAnchor, currentMinRadius);
        }
        else
        {
            currentMaxRadius = 0f;
        }
    }

    #region OG Shrinking radius
    //-------------------------------//
    // Original shrinking tether radius method //
    //------------------------------//

    //private Vector3 ClampToShrinkingAnchorWall(Vector3 currentPos, Vector3 moveVector)
    //{
    //    if (!isTethered)
    //        return moveVector;

    //    Vector3 proposedPos = currentPos + moveVector;
    //    Vector3 offset = proposedPos - anchorPosition;
    //    float distance = offset.magnitude;

    //    if (distance > currentMaxRadius)
    //    {
    //        Vector3 toCenter = offset.normalized;
    //        Vector3 tangentMove = moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
    //        float overshoot = distance - currentMaxRadius;
    //        tangentMove *= Mathf.Clamp01(1f - overshoot / moveVector.magnitude);
    //        return tangentMove;
    //    }

    //    Vector3 currentOffset = currentPos - anchorPosition;
    //    bool insideMinRadius = currentOffset.magnitude < currentMinRadius;

    //    if (insideMinRadius && distance > currentMinRadius)
    //    {
    //        Vector3 toCenter = offset.normalized;
    //        return moveVector - Vector3.Dot(moveVector, toCenter) * toCenter;
    //    }

    //    return moveVector;
    //}
    #endregion

    // Stops player movement. Intended to be called externally.
    public void StopMovement(Vector3? forward = null)
    {
        isMovementStopped = true;
        movementStoppedExternally = true;
        moveInput = Vector3.zero;

        if (forward != null)
            lookDirection = forward.Value;
    }

    // Resumes player movement. Intended to be called externally.
    public void ResumeMovement()
    {
        isMovementStopped = false;
        movementStoppedExternally = false;
    }

    // Temporarily disables movement for `duration` seconds. Called by TetherDamageDealer when a Golem breaks the tether. Independent of StopMovement/ResumeMovement so it won't fight other movement locks.
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;
        // Take the longer of any existing stun and the new one so overlapping breaks don't cut it short.
        stunTimer = Mathf.Max(stunTimer, duration);
        isStunned = true;
        moveInput = Vector3.zero;
    }

    // Snaps the player to face a world-space direction. Called by PlayerAttacks on fire so the player turns to aim. 
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        lookDirection = direction.normalized;
        rb.MoveRotation(Quaternion.LookRotation(lookDirection));
    }

    private void StartDash()
    {
        if (CameraPanEffect.GlobalPanActive)
            return;
        playerAnchor.ReleaseTether();
        isDashing = true;
        dashTimer = dashDuration;

        // Capture and lock the dash direction at the moment the dash starts.
        // Normalize to ensure consistent speed and prevent fractional input from changing it.
        dashDirection = (moveInput.sqrMagnitude > 0.01f) ? moveInput.normalized : transform.forward;
        moveInput = Vector3.zero; // make sure normal movement input doesn't interfere

        // Lock facing to dash direction immediately
        rb.MoveRotation(Quaternion.LookRotation(dashDirection));

        if (dashEffect != null)
        {
            // Spawn the trail effect behind the player, facing opposite the dash direction
            Vector3 spawnPosition = transform.position - dashDirection * dashEffectBackOffset + Vector3.up * dashEffectHeightOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(-dashDirection);

            Instantiate(dashEffect, spawnPosition, spawnRotation); ;
        }

        RuntimeManager.PlayOneShot(dashActivationEvent, transform.position);
        RuntimeManager.PlayOneShot(voicedDashActivationEvent, transform.position);

        // Debug.Log("start dash");
        PlayerDashVFX.Instance.StartDashVFX();
        playerAnimation.PlayDash();
    }

    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;
        playerHUD?.UpdateDashCooldown(0f);

        // Debug.Log("end dash");
        PlayerDashVFX.Instance.EndDashVFX();
        //playerAnimation.StopDash();
    }

    public void SetInMud(bool value)
    {
        isInMud = value;
        if (isInMud && isDashing)
        {
            EndDash();
        }
    }

    public void AddSpeedModifier(object source, float multiplier)
    {
        if (!speedModifiers.ContainsKey(source))
            speedModifiers.Add(source, multiplier);
    }

    public void RemoveSpeedModifier(object source)
    {
        if (speedModifiers.ContainsKey(source))
            speedModifiers.Remove(source);
    }

    private void OnDrawGizmos()
    {
        if (!isTethered)
            return;

        if (currentMaxRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(anchorPosition, currentMaxRadius);
        }
    }

    #region animation blend helpers
    // Animator helpers for movement speed changes in movement blend tree
    public float GetMovementFraction()
    {
        if (moveSpeed <= 0f) return 0f;
        float currentMax = CurrentSpeed;
        float inputFraction = speed;
        float actualSpeed = currentMax * inputFraction;
        return Mathf.Clamp01(actualSpeed / moveSpeed);
    }

    public float GetForwardFraction()
    {
        return Mathf.Clamp(moveInput.z, -1f, 1f);
    }

    public float GetStrafeFraction()
    {
        return Mathf.Clamp(moveInput.x, -1f, 1f);
    }
    #endregion
}