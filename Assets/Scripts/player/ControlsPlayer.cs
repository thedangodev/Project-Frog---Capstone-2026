using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownControllerWithDash : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;        // normal movement speed
    [SerializeField] private float gravity = 9.81f;        // gravity strength

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundSnapOffset = 0.01f;

    [Header("Collision")]
    [SerializeField] private bool forceConvexCollider = true;

    [Header("Dash Settings")]
    [SerializeField] private string dashInputName = "Dash"; // Input Manager button
    [SerializeField] private float dashMultiplier = 2f;     // speed multiplier while dashing
    [SerializeField] private float dashDuration = 3f;       // how long dash lasts
    [SerializeField] private float dashCooldown = 5f;       // cooldown after dash ends

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;

    private Vector3 inputDir = Vector3.zero;
    private Vector3 dashDirection = Vector3.forward;
    private float verticalVelocity = 0f;
    [SerializeField] private bool isGrounded = false;

    [SerializeField] private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

    private Renderer rend;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (rend == null) rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        col = GetComponent<Collider>();
    }

    void Update()
    {
        // Read raw movement input each frame but only apply to inputDir when not dashing
        float hx = Input.GetAxisRaw("Horizontal");
        float hz = Input.GetAxisRaw("Vertical");
        Vector3 rawInput = new Vector3(hx, 0f, hz).normalized;

        if (!isDashing)
        {
            // update movement direction when not dashing
            inputDir = rawInput;
        }

        // Dash input detection - start dash using current inputDir (or forward when no input)
        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            if (!string.IsNullOrEmpty(dashInputName) && Input.GetButtonDown(dashInputName))
            {
                // lock dash direction at start
                dashDirection = (inputDir.sqrMagnitude > 0.01f) ? inputDir : transform.forward;
                isDashing = true;
                dashTimer = dashDuration;
                lastDashTime = Time.time;
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float dt = Time.fixedDeltaTime;

        // speed & dash bookkeeping
        float currentSpeed = UpdateDashAndGetSpeed(dt);

        // horizontal movement: when dashing use locked dashDirection, otherwise use inputDir
        Vector3 horizontalMove = (isDashing ? dashDirection : inputDir) * currentSpeed;

        // gravity integration
        verticalVelocity += -gravity * dt;

        // bottom point + ray origin
        Vector3 bottomPointWorld = GetBottomPointWorld();
        Vector3 rayOrigin = bottomPointWorld + Vector3.up * 0.01f;

        // grounding check
        PerformGroundCheck(rayOrigin);

        // compose movement
        Vector3 move = new Vector3(horizontalMove.x, verticalVelocity, horizontalMove.z) * dt;

        // collision sweep (may adjust position and grounding)
        Vector3 newPos = CollisionSweep(rb.position, move, rayOrigin);

        // snap to ground if grounded
        newPos = SnapToGroundIfNeeded(newPos, bottomPointWorld, rayOrigin);

        // apply movement
        rb.MovePosition(newPos);
    }

    // --- Extracted helper methods ---

    private float UpdateDashAndGetSpeed(float dt)
    {
        float currentSpeed = moveSpeed;
        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            dashTimer -= dt;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
        return currentSpeed;
    }

    private Vector3 GetBottomPointWorld()
    {
        if (rend != null)
        {
            Bounds b = rend.bounds;
            return new Vector3(b.center.x, b.min.y, b.center.z);
        }
        else if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.min.y, b.center.z);
        }
        else
        {
            return rb.position;
        }
    }

    private void PerformGroundCheck(Vector3 rayOrigin)
    {
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 0.02f, groundMask, QueryTriggerInteraction.Ignore))
        {
            float distanceToGround = rayOrigin.y - hit.point.y;
            if (distanceToGround <= groundCheckDistance + 0.001f && verticalVelocity <= 0f)
            {
                isGrounded = true;
                verticalVelocity = 0f;
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    // Performs the sweep test and returns the adjusted new position.
    private Vector3 CollisionSweep(Vector3 currentPos, Vector3 move, Vector3 rayOrigin)
    {
        Vector3 newPos = currentPos + move;

        if (col != null && move.sqrMagnitude > 0f)
        {
            Vector3 direction = move.normalized;
            float distance = move.magnitude;
            RaycastHit sweepHit;

            if (Physics.Raycast(rayOrigin, direction, out sweepHit, distance + 0.01f))
            {
                float hitDist = sweepHit.distance;
                newPos = currentPos + direction * Mathf.Max(0f, hitDist - 0.01f);

                if (Vector3.Dot(sweepHit.normal, Vector3.up) > 0.5f)
                {
                    verticalVelocity = 0f;
                    isGrounded = true;
                }
            }
        }

        return newPos;
    }

    private Vector3 SnapToGroundIfNeeded(Vector3 newPos, Vector3 bottomPointWorld, Vector3 rayOrigin)
    {
        if (isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore))
            {
                float bottomOffset = rb.position.y - bottomPointWorld.y;
                float targetY = hit.point.y + groundSnapOffset + bottomOffset;
                newPos.y = Mathf.Max(newPos.y, targetY);
            }
        }
        return newPos;
    }

}