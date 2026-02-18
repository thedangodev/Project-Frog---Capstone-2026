using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to a trap parent object. The script finds a child GameObject tagged "trap"
/// (that should contain an isTrigger Collider) and forwards trigger events to this component.
/// When a GameObject tagged "Player" enters the child trigger the trap will start shooting
/// dart prefabs every `fireInterval` seconds; it stops when all players leave the trigger.
/// </summary>
public class TrapShoot : MonoBehaviour
{
    [Header("Dart")]
    public GameObject dartPrefab;
    public Transform shootPoint;
    public float dartSpeed = 20f;
    public float dartLifetime = 10f;

    [Header("Timing")]
    [Tooltip("Seconds between shots while trigger is active.")]
    public float fireInterval = 5f;

    // internal
    readonly HashSet<GameObject> _playersInTrigger = new();
    Coroutine _shootingRoutine;
    GameObject _trapTriggerChild;

    void Start()
    {
        // Find first child tagged "trap"
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject != gameObject && t.gameObject.CompareTag("trap"))
            {
                _trapTriggerChild = t.gameObject;
                break;
            }
        }

        if (_trapTriggerChild == null)
        {
            Debug.LogWarning($"[{nameof(TrapShoot)}] No child with tag \"trap\" found under {name}.");
            return;
        }

        // Ensure the child has a Collider set as trigger
        var col = _trapTriggerChild.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[{nameof(TrapShoot)}] Child tagged \"trap\" on {_trapTriggerChild.name} has no Collider.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[{nameof(TrapShoot)}] Collider on {_trapTriggerChild.name} is not marked as isTrigger. Mark it as trigger for trap activation.");
        }

        // Add or get forwarding component so the child's trigger events are forwarded here
        var forwarder = _trapTriggerChild.GetComponent<TrapTriggerForwarder>();
        if (forwarder == null)
        {
            forwarder = _trapTriggerChild.AddComponent<TrapTriggerForwarder>();
        }

        forwarder.parent = this;
    }

    // Called by the trigger forwarder when something enters the child trigger
    internal void OnChildTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        _playersInTrigger.Add(other.gameObject);

        if (_playersInTrigger.Count == 1)
        {
            StartShooting();
        }
    }

    // Called by the trigger forwarder when something exits the child trigger
    internal void OnChildTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        _playersInTrigger.Remove(other.gameObject);

        if (_playersInTrigger.Count == 0)
        {
            StopShooting();
        }
    }

    void StartShooting()
    {
        if (_shootingRoutine != null) return;
        _shootingRoutine = StartCoroutine(ShootingLoop());
    }

    void StopShooting()
    {
        if (_shootingRoutine != null)
        {
            StopCoroutine(_shootingRoutine);
            _shootingRoutine = null;
        }
    }

    IEnumerator ShootingLoop()
    {
        // Fire immediately on activation, then wait between shots
        while (true)
        {
            ShootOnce();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    void ShootOnce()
    {
        if (dartPrefab == null)
        {
            Debug.LogWarning($"[{nameof(TrapShoot)}] No dartPrefab assigned on {name}.");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogWarning($"[{nameof(TrapShoot)}] No shootPoint assigned on {name}.");
            return;
        }

        var dart = Instantiate(dartPrefab, shootPoint.position, shootPoint.rotation);
        var rb = dart.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootPoint.right * dartSpeed;
        }

        Destroy(dart, dartLifetime);
    }

    void OnDisable()
    {
        StopShooting();
    }
}

/// <summary>
/// Lightweight forwarder put on the child trigger object that calls back into the parent TrapShoot.
/// This class is intentionally in the same file to keep the trap implementation together.
/// </summary>
public class TrapTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public TrapShoot parent;

    void OnTriggerEnter(Collider other)
    {
        parent?.OnChildTriggerEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        parent?.OnChildTriggerExit(other);
    }
}
