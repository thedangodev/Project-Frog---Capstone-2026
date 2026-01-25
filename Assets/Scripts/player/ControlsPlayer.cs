using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TopDownControllerWithDash : MonoBehaviour
{

    public Health health;


    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float gravity = 9.81f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundSnapOffset = 0.01f;

    [Header("Collision Skin")]
    [SerializeField] private float enemyCollisionSkin = 3f;

    [Header("Dash Settings")]
    [SerializeField] private string dashInputName = "Dash";
    [SerializeField] private float dashMultiplier = 2f;
    [SerializeField] private float dashDuration = 3f;
    [SerializeField] private float dashCooldown = 5f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;

    private Vector3 inputDir = Vector3.zero;
    private Vector3 dashDirection = Vector3.forward;
    private float verticalVelocity = 0f;
    private bool isGrounded = false;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;

    private Renderer rend;

    private Vector3 blockedNormal = Vector3.zero;
    private bool isBlocked = false;
    public bool IsImmortal = false;

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
        health = GetComponent<Health>();

        col = GetComponent<Collider>();
    }

    void Update()
    {
        float hx = Input.GetAxisRaw("Horizontal");
        float hz = Input.GetAxisRaw("Vertical");
        Vector3 rawInput = new Vector3(hx, 0f, hz).normalized;

        if (!isDashing)
            inputDir = rawInput;

        if (!isDashing && Time.time >= lastDashTime + dashCooldown)
        {
            if (!string.IsNullOrEmpty(dashInputName) && Input.GetButtonDown(dashInputName))
            {
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

        float currentSpeed = UpdateDashAndGetSpeed(dt);

        Vector3 horizontalMove = (isDashing ? dashDirection : inputDir) * currentSpeed;

        // --- Collision skin (only when NOT dashing) ---
        if (!isDashing && horizontalMove.sqrMagnitude > 0.01f)
        {
            RaycastHit hit;
            Vector3 origin = rb.position + Vector3.up * 0.5f;
            Vector3 dir = horizontalMove.normalized;

            if (Physics.Raycast(origin, dir, out hit, enemyCollisionSkin))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    horizontalMove = Vector3.zero;
                }
            }
        }

        // --- Block movement into enemy normal (only when NOT dashing) ---
        if (isBlocked && !isDashing)
        {
            if (horizontalMove.sqrMagnitude > 0.01f)
            {
                if (Vector3.Dot(horizontalMove.normalized, -blockedNormal) > 0.5f)
                {
                    horizontalMove = Vector3.zero;
                }
            }
        }

        verticalVelocity += -gravity * dt;

        Vector3 bottomPointWorld = GetBottomPointWorld();
        Vector3 rayOrigin = bottomPointWorld + Vector3.up * 0.01f;

        PerformGroundCheck(rayOrigin);

        Vector3 move = new Vector3(horizontalMove.x, verticalVelocity, horizontalMove.z) * dt;

        // --- NEW: Sweep is disabled during dash ---
        Vector3 newPos = rb.position + move;

        if (!isDashing)
        {
            newPos = CollisionSweep(rb.position, move, rayOrigin);
        }

        newPos = SnapToGroundIfNeeded(newPos, bottomPointWorld, rayOrigin);

        rb.MovePosition(newPos);
    }

    private float UpdateDashAndGetSpeed(float dt)
    {
        float currentSpeed = moveSpeed;

        if (isDashing)
        {
            currentSpeed *= dashMultiplier;
            dashTimer -= dt;

            // Dash ended
            if (dashTimer <= 0f)
            {
                isDashing = false;
                IsImmortal = false;   // ← turn off immortality
            }
            else
            {
                IsImmortal = true;    // ← stay immortal during dash
            }
        }
        else
        {
            IsImmortal = false;       // ← not dashing = not immortal
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            blockedNormal = collision.contacts[0].normal;
            isBlocked = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            isBlocked = false;
            blockedNormal = Vector3.zero;
        }
    }
}
