using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Enemy knockback with collision-safe movement.
// Uses CollisionUtility so enemies cannot be pushed through walls/objects.
[RequireComponent(typeof(Rigidbody))]
public class EnemyKnockback : MonoBehaviour
{
    [Header("Knockback")]
    [Tooltip("Knockback speed in meters per second.")]
    [SerializeField] private float knockbackSpeed = 20f;

    [Tooltip("Power of the ease-out curve.")]
    [SerializeField] private float knockbackEasePower = 2f;

    [Tooltip("Multiplier on incoming knockback distance.")]
    [SerializeField] private float knockbackResistance = 1f;


    [Header("Projectile Knockback")]
    [SerializeField] private float projectileKnockbackDistance = 2f;

    [SerializeField] private bool useProjectileTravelDirection = true;

    [Header("Debug")]
    [Tooltip("Temporary multiplier for knockback — set very high to make knockback obvious while debugging.")]
    [SerializeField] private float debugKnockbackMultiplier = 50f;


    [Header("Collision")]
    [Tooltip("Layers the enemy is blocked by while being knocked back.")]
    [SerializeField] private LayerMask collisionLayers;


    [Tooltip("Capsule used for collision-safe movement.")]
    [SerializeField] private CapsuleCollider capsule;

    [Header("Fallback capsule (used when no CapsuleCollider assigned)")]
    [Tooltip("Height used for fallback capsule casts when a CapsuleCollider is not assigned.")]
    [SerializeField] private float fallbackCapsuleHeight = 1.6f;
    [Tooltip("Radius used for fallback capsule casts when a CapsuleCollider is not assigned.")]
    [SerializeField] private float fallbackCapsuleRadius = 0.3f;

    public bool IsBeingKnockedBack { get; private set; }

    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine knockbackCoroutine;
    private MovementComponent movementComp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        agent = GetComponent<NavMeshAgent>();

        movementComp = GetComponent<MovementComponent>();

        if (capsule == null)
            capsule = GetComponentInChildren<CapsuleCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryProjectileKnockback(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryProjectileKnockback(collision.collider);
    }

    private void TryProjectileKnockback(Collider other)
    {
        if (other == null)
            return;

        IProjectile projectile = other.GetComponentInParent<IProjectile>();

        if (projectile == null)
            return;

        Vector3 direction;

        if (useProjectileTravelDirection && other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.sqrMagnitude > 0.001f)
        {
            direction = other.attachedRigidbody.linearVelocity;
        }
        else
        {
            direction = transform.position - other.transform.position;
        }

        direction = ConstrainForwardAxis(direction);

        ApplyKnockback(direction, projectileKnockbackDistance);
    }

    private Vector3 ConstrainForwardAxis(Vector3 rawDirection)
    {
        rawDirection.y = 0f;

        if (rawDirection.sqrMagnitude < 0.000001f)
            return rawDirection;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float dot = Vector3.Dot(rawDirection.normalized, forward);

        // preserve magnitude - Locks direction to forward or backwards knockback
        return forward * Mathf.Sign(dot) * rawDirection.magnitude;
    }

    public void ApplyKnockback(Vector3 direction, float distance)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.000001f)
            return;

        float finalDistance = distance * knockbackResistance * Mathf.Max(1f, debugKnockbackMultiplier);

        if (finalDistance <= 0f)
            return;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction.normalized, finalDistance));
    }

    private IEnumerator KnockbackRoutine(Vector3 dir, float distance)
    {
        IsBeingKnockedBack = true;

        if (movementComp != null) movementComp.SetMovementEnabled(false);

        bool hadAgent = agent != null && agent.enabled;

        //Debug.Log($"[EnemyKnockback] KnockbackRoutine START on '{name}' dir={dir} distance={distance:F2} hadAgent={hadAgent}", this);


        if (hadAgent)
        {
            agent.isStopped = true;
            agent.updatePosition = true;
        }

        float duration = Mathf.Max(0.01f, distance / knockbackSpeed);

        float elapsed = 0f;

        Vector3 start = rb.position;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float easedT = 1f - Mathf.Pow(1f - t, knockbackEasePower);

            Vector3 target = start + dir * (distance * easedT);

            Vector3 motion = target - rb.position;

            if (motion.sqrMagnitude > 0.000001f)
            {
                Vector3 oldPosition = rb.position;
                if (capsule != null)
                {
                    if (rb.isKinematic)
                    {
                        CollisionUtility.GetCapsule(rb, capsule, out Vector3 capStart, out Vector3 capEnd);

                        Vector3 testStart = capStart + motion;
                        Vector3 testEnd = capEnd + motion;

                        if (TryGetBlockingRaycast(oldPosition, motion.normalized, motion.magnitude, out Collider rayBlock, out RaycastHit rayHit)) { Debug.Log($"[EnemyKnockback] Raycast blocked by '{rayBlock?.name}' — stopping knockback.", this); break; }

                        Collider[] overlaps = Physics.OverlapCapsule(testStart, testEnd, capsule.radius, collisionLayers.value, QueryTriggerInteraction.Ignore);

                        if (!TryGetBlockingOverlap(overlaps, out Collider blocking))
                        {
                            if (hadAgent)
                            {
                                agent.Move(motion);
                                // sync physics/transform
                                if (!rb.isKinematic) rb.MovePosition(agent.nextPosition);
                                else transform.position = agent.nextPosition;
                            }
                            else
                            {
                                transform.position += motion;
                            }
                        }
                        else
                        {
                            //Debug.Log($"[EnemyKnockback] Overlap blocked by '{blocking?.name}' (root='{blocking?.transform?.root?.name}', attachedRb={(blocking?.attachedRigidbody != null ? blocking.attachedRigidbody.name : "null")})", this);

                            bool moved = false;

                            float remaining = motion.magnitude;

                            float step = Mathf.Min(0.5f, remaining);

                            for (int s = 0; s < 6 && step > 0.001f; s++)
                            {
                                Vector3 stepMotion = motion.normalized * step;

                                if (TryGetBlockingRaycast(oldPosition, motion.normalized, stepMotion.magnitude, out Collider stepRayBlock, out RaycastHit stepRayHit)) { Debug.Log($"[EnemyKnockback] incremental ray step blocked by '{stepRayBlock?.name}'", this); step *= 0.5f; continue; }

                                Vector3 sStart = capStart + stepMotion;

                                Vector3 sEnd = capEnd + stepMotion;

                                Collider[] ov = Physics.OverlapCapsule(sStart, sEnd, capsule.radius, collisionLayers.value, QueryTriggerInteraction.Ignore);

                                if (!TryGetBlockingOverlap(ov, out Collider stepBlocking))
                                {
                                    if (hadAgent)
                                    {
                                        agent.Move(stepMotion);
                                        if (!rb.isKinematic) rb.MovePosition(agent.nextPosition);
                                        else transform.position = agent.nextPosition;
                                    }
                                    else
                                    {
                                        transform.position += stepMotion;
                                    }
                                    moved = true;
                                    break;
                                }
                                else { Debug.Log($"[EnemyKnockback] incremental step blocked by '{stepBlocking?.name}'", this); }

                                step *= 0.5f;
                            }
                            if (!moved) { Debug.Log($"[EnemyKnockback] Knockback blocked by capsule overlap on '{name}' — stopping (blocking '{blocking?.name}').", this); break; }
                        }
                    }
                    else
                    {
                        // Non-kinematic - prefer using collision utility to respect physics when no agent.
                        if (hadAgent)
                        {
                            // If agent present, prefer using agent.Move and sync Rigidbody via MovePosition.
                            // Still use collision utility only when agent is not available.
                            // We attempt a simple agent movement; collision safety remains via the capsule overlap checks above.
                            agent.Move(motion);
                            if (!rb.isKinematic) rb.MovePosition(agent.nextPosition);
                            else transform.position = agent.nextPosition;
                        }
                        else
                        {
                            CollisionUtility.MoveWithCapsuleCollision(rb, capsule, motion, collisionLayers);
                        }
                    }
                }
                else
                {
                    float capHeight = Mathf.Max(0.01f, fallbackCapsuleHeight);

                    float capRadius = Mathf.Max(0.01f, fallbackCapsuleRadius);

                    Vector3 capsuleTop = rb.position + Vector3.up * (capHeight / 2f);

                    Vector3 capsuleBottom = rb.position - Vector3.up * (capHeight / 2f);

                    if (TryGetBlockingRaycast(oldPosition, motion.normalized, motion.magnitude, out Collider rcBlock, out RaycastHit rcHit)) { Debug.Log($"[EnemyKnockback] Fallback raycast blocked by '{rcBlock?.name}' — stopping knockback.", this); break; }

                    if (!Physics.CapsuleCast(capsuleTop, capsuleBottom, capRadius, motion.normalized, out RaycastHit hit, motion.magnitude, collisionLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hadAgent)
                        {
                            agent.Move(motion);
                            if (!rb.isKinematic) rb.MovePosition(agent.nextPosition);
                            else transform.position = agent.nextPosition;
                        }
                        else
                        {
                            if (rb.isKinematic) transform.position += motion; else rb.MovePosition(rb.position + motion);
                        }
                    }
                    else
                    {
                        if (rb.isKinematic)
                        {
                            bool moved = false;

                            float remaining = motion.magnitude;

                            float step = Mathf.Min(0.2f, remaining);

                            for (int s = 0; s < 6 && step > 0.001f; s++)
                            {
                                Vector3 stepMotion = motion.normalized * step;

                                if (TryGetBlockingRaycast(oldPosition, motion.normalized, stepMotion.magnitude, out Collider stepRcBlock, out RaycastHit stepRcHit)) { Debug.Log($"[EnemyKnockback] incremental fallback ray blocked by '{stepRcBlock?.name}'", this); step *= 0.5f; continue; }

                                Vector3 testTop = (rb.position + stepMotion) + Vector3.up * (capHeight / 2f);

                                Vector3 testBottom = (rb.position + stepMotion) - Vector3.up * (capHeight / 2f);

                                Collider[] overlaps = Physics.OverlapCapsule(testTop, testBottom, capRadius, collisionLayers, QueryTriggerInteraction.Ignore);

                                if (!TryGetBlockingOverlap(overlaps, out Collider blocking2))
                                {
                                    if (hadAgent)
                                    {
                                        agent.Move(stepMotion);
                                        if (!rb.isKinematic) rb.MovePosition(agent.nextPosition);
                                        else transform.position = agent.nextPosition;
                                    }
                                    else
                                    {
                                        transform.position += stepMotion;
                                    }
                                    moved = true;
                                    break;
                                }

                                else { Debug.Log($"[EnemyKnockback] incremental fallback step blocked by '{blocking2?.name}'", this); }

                                step *= 0.5f;
                            }
                            if (!moved) { Debug.Log($"[EnemyKnockback] Knockback blocked by capsule cast on '{name}' — hit '{hit.collider?.name}'. Stopping knockback.", this); break; }
                        }
                        else
                        {
                            //Debug.Log($"[EnemyKnockback] Knockback blocked by capsule cast on '{name}' — hit '{hit.collider?.name}'. Stopping knockback.", this);
                            break;
                        }
                    }
                }

                if (Vector3.Distance(oldPosition, rb.position) < 0.001f && !rb.isKinematic) { /* Debug.Log($"[EnemyKnockback] Knockback movement stalled on '{name}' (oldPos==newPos) — breaking.", this); break; } else { Vector3 newPos = rb.isKinematic ? transform.position : rb.position; Debug.Log($"[EnemyKnockback] '{name}' moved from {oldPosition} to {newPos} during knockback.", this); */ }
            }
            elapsed += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        if (hadAgent && agent != null)
        {
            Vector3 syncPos = rb.isKinematic ? transform.position : rb.position;
            agent.Warp(syncPos);
            agent.updatePosition = true;
            agent.isStopped = false;
        }

        if (movementComp != null) movementComp.SetMovementEnabled(true);

        Vector3 finalPos = rb.isKinematic ? transform.position : rb.position;

        //Debug.Log($"[EnemyKnockback] KnockbackRoutine END on '{name}' finalPos={finalPos}", this);

        IsBeingKnockedBack = false;

        knockbackCoroutine = null;
    }

    // Return true and the first blocking collider if any. Ignores:
    //  - null entries
    //  - trigger colliders
    //  - colliders whose attachedRigidbody == this.rb (same body)
    //  - colliders whose root == this.transform.root (same multi-part prefab)
    //  - colliders that are children/parents of this transform
    //  - colliders on the Terrain layer (useful ground layer)
    private bool TryGetBlockingOverlap(Collider[] overlaps, out Collider blocking)
    {
        blocking = null;
        if (overlaps == null || overlaps.Length == 0) return false;

        int terrainLayer = LayerMask.NameToLayer("Terrain");

        foreach (var col in overlaps)
        {
            if (col == null) continue;

            if (col.isTrigger) continue;

            if (col.transform == transform) continue;

            if (col.transform.IsChildOf(transform)) continue;

            if (transform.IsChildOf(col.transform)) continue;

            if (col.attachedRigidbody == rb) continue;

            if (col.transform.root == transform.root) continue;

            if (terrainLayer != -1 && col.gameObject.layer == terrainLayer) continue;

            blocking = col;
            return true;
        }

        return false;
    }

    private bool TryGetBlockingRaycast(Vector3 start, Vector3 direction, float distance, out Collider blocking, out RaycastHit outHit)
    {
        blocking = null;

        outHit = default;
        
        if (distance <= 0f) return false;
        
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        
        RaycastHit[] hits = Physics.RaycastAll(start + Vector3.up * 0.1f, direction, distance, collisionLayers, QueryTriggerInteraction.Ignore);
        
        if (hits == null || hits.Length == 0) return false;
        
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        foreach (var hit in hits)
        {
            var col = hit.collider;
            
            if (col == null) continue;
            
            if (col.isTrigger) continue;
            
            if (col.attachedRigidbody == rb) continue;
            
            if (col.transform.root == transform.root) continue;
            
            if (transform.IsChildOf(col.transform)) continue;
            
            if (terrainLayer != -1 && col.gameObject.layer == terrainLayer) continue;
            
            blocking = col;
            
            outHit = hit;
            
            return true;
        }
        return false;
    }
}