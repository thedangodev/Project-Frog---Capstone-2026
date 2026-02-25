using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;

    private Rigidbody rb;

    private Vector3 moveInput;
    private Vector3 dashDirection;

    private bool isDashing;
    private bool movementStoppedExternally;

    private float dashTimer;
    private float dashCooldownTimer;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        // Update dash cooldown
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (movementStoppedExternally)
            return;

        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float verticalMove = Input.GetAxisRaw("Vertical");

        // No movement input during dash, otherwise create movement vector using horizontalMove and verticalMove
        moveInput = isDashing ? Vector3.zero : new Vector3(horizontalMove, 0f, verticalMove).normalized;

        // Check for valid dash input
        if (!isDashing && dashCooldownTimer <= 0f && Input.GetButtonDown("Jump"))
            StartDash();
    }

    private void FixedUpdate()
    {
        if (movementStoppedExternally)
            return;

        // Dash movement
        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.MovePosition(rb.position + dashDirection * (dashDistance / dashDuration) * Time.fixedDeltaTime);
            if (dashTimer <= 0f)
                EndDash();
            return;
        }

        // If no dash, move normally
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);

        // Rotate player to the move direction
        if (moveInput.sqrMagnitude > 0.0001f)
            transform.forward = moveInput;
    }

    public void StopMovement()
    {
        // Method to be called externally to stop player movment (ex: during attack animation)
        movementStoppedExternally = true;
        moveInput = Vector3.zero;
    }

    public void ResumeMovement()
    {
        // Method to be called externally to resume player movement (ex: after the end of a ranged attack animation)
        movementStoppedExternally = false;
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        // Set the dash direction to the move direction. If there is no move direction, set the dash direction to the direction the player is facing
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;
    }

    private void EndDash()
    {
        isDashing = false;
        dashCooldownTimer = dashCooldown;
    }
}