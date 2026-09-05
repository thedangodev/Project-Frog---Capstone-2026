using UnityEngine;
using System;
using System.Collections.Generic;

public class OverchargeTrailCollider : MonoBehaviour
{
    public event Action<GameObject> OnEnemyHit;

    [Header("Collision Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float damageInterval = 1.0f; // for testing and prevents damage spamming
    [SerializeField] private GameObject colliderPrefab;
    [SerializeField] private float colliderRadius = 4f;
    [SerializeField] private int maxColliders = 12;
    [SerializeField] private float colliderSpacing = 0.3f;

    [Header("Layer")]
    [Tooltip("Dedicated layer for spawned trail colliders — must exist in Tags & Layers. Kept separate from the Player's layer so projectiles can be excluded from the trail without also passing through the real Player.")]
    [SerializeField] private string trailColliderLayer = "OverchargeTrail";

    [Tooltip("Projectile layer that trail colliders should never collide with.")]
    [SerializeField] private string projectileLayer = "Projectile";

    [Header("Element VFX")]
    [SerializeField] private ParticleSystem fireVFX;
    [SerializeField] private ParticleSystem iceVFX;
    [SerializeField] private ParticleSystem windVFX;

    private struct TrailEffects
    {
        public ParticleSystem fire;
        public ParticleSystem ice;
        public ParticleSystem wind;
    }
    private TrailEffects[] trailEffects;

    private TrailRenderer trailRenderer;
    private List<GameObject> activeColliders = new List<GameObject>();
    private Dictionary<GameObject, float> enemyDamageTimes = new Dictionary<GameObject, float>();
    private bool isEnabled = false;
    // The anchor type that determines which VFX to spawn on each collider
    private PlayerOvercharge.AnchorType currentAnchorType = PlayerOvercharge.AnchorType.None;

    // Cached layer index for spawned trail colliders.
    private int trailLayerIndex = -1;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.Log("[OverchargeTrailCollider] No TrailRenderer found on GameObject");
        }

        // Resolve layers and tell physics the trail layer ignores the projectile layer entirely.
        trailLayerIndex = LayerMask.NameToLayer(trailColliderLayer);
        int projIndex = LayerMask.NameToLayer(projectileLayer);

        if (trailLayerIndex == -1)
            Debug.LogWarning($"[OverchargeTrailCollider] Layer '{trailColliderLayer}' does not exist — create it in Tags & Layers. Trail colliders will fall back to this object's layer and can still be hit by projectiles.", this);
        if (projIndex == -1)
            Debug.LogWarning($"[OverchargeTrailCollider] Layer '{projectileLayer}' does not exist — projectiles will not be excluded from the trail.", this);

        if (trailLayerIndex != -1 && projIndex != -1)
            Physics.IgnoreLayerCollision(trailLayerIndex, projIndex, true);
    }

    private void Update()
    {
        if (isEnabled && trailRenderer != null)
        {
            UpdateTrailCollider();
        }
    }

    public void EnableCollider()
    {
        isEnabled = true;
        trailRenderer.Clear();
    }

    public void DisableCollider()
    {
        isEnabled = false;
        ClearAllColliders();
        enemyDamageTimes.Clear();
    }

    private void UpdateTrailCollider()
    {
        // Get trail positions
        int positionCount = trailRenderer.positionCount;
        if (positionCount < 2) return;

        // Calculate how many colliders needed
        int neededColliders = Mathf.Min(positionCount, maxColliders);

        // Create or reuse colliders
        while (activeColliders.Count < neededColliders)
        {
            GameObject newCollider = CreateColliderInstance();
            activeColliders.Add(newCollider);
        }

        // update collider postitions on trail
        for (int i = 0; i < neededColliders; i++)
        {
            int trailIndex = Mathf.FloorToInt((float)i / neededColliders * positionCount);
            trailIndex = Mathf.Min(trailIndex, positionCount - 1);

            Vector3 position = trailRenderer.GetPosition(trailIndex);
            activeColliders[i].transform.position = position;
            activeColliders[i].SetActive(true);
        }

        // Disable extra colliders
        for (int i = neededColliders; i < activeColliders.Count; i++)
        {
            activeColliders[i].SetActive(false);
        }
    }

    private GameObject CreateColliderInstance()
    {
        GameObject colliderObj = new GameObject("TrailCollider");
        colliderObj.transform.SetParent(transform);

        // Dedicated layer (NOT the Player's) so IgnoreLayerCollision can exclude projectiles from the trail without also making projectiles pass through the actual Player.
        colliderObj.layer = trailLayerIndex != -1 ? trailLayerIndex : gameObject.layer;

        // Opt this collider out of the mud pit.
        colliderObj.AddComponent<MudPitIgnore>();

        SphereCollider sphere = colliderObj.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = colliderRadius;

        TrailColliderTrigger trigger = colliderObj.AddComponent<TrailColliderTrigger>();
        trigger.enemyTag = enemyTag;
        trigger.OnEnemyEnter += HandleEnemyTrigger;

        // Instantiate the matching VFX prefab so it looks cool
        ParticleSystem vfxInstance = null;
        switch (currentAnchorType)
        {
            case PlayerOvercharge.AnchorType.Fire:
                if (fireVFX != null)
                    vfxInstance = Instantiate(fireVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.Ice:
                if (iceVFX != null)
                    vfxInstance = Instantiate(iceVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.Wind:
                if (windVFX != null)
                    vfxInstance = Instantiate(windVFX, colliderObj.transform);
                break;
            case PlayerOvercharge.AnchorType.None:
            default:
                break;
        }

        if (vfxInstance != null)
        {
            // Reset local transform so VFX aligns with collider
            vfxInstance.transform.localPosition = Vector3.zero;
            vfxInstance.transform.localRotation = Quaternion.identity;
        }

        return colliderObj;
    }

    // Called by PlayerOvercharge to set which anchor type is currently overcharged
    public void SetAnchorType(PlayerOvercharge.AnchorType anchorType)
    {
        currentAnchorType = anchorType;
    }

    private void HandleEnemyTrigger(GameObject enemy)
    {
        // Damage interval check per enemy
        if (!enemyDamageTimes.ContainsKey(enemy))
        {
            enemyDamageTimes[enemy] = 0f;
        }

        if (Time.time - enemyDamageTimes[enemy] >= damageInterval)
        {
            enemyDamageTimes[enemy] = Time.time;
            OnEnemyHit?.Invoke(enemy);
        }
    }

    private void ClearAllColliders()
    {
        foreach (GameObject collider in activeColliders)
        {
            if (collider != null)
            {
                Destroy(collider);
            }
        }
        activeColliders.Clear();
    }

    private void OnDestroy()
    {
        ClearAllColliders();
    }
}

// Helper component for individual trail colliders
public class TrailColliderTrigger : MonoBehaviour
{
    public string enemyTag = "Enemy";
    public event Action<GameObject> OnEnemyEnter;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(enemyTag))
        {
            OnEnemyEnter?.Invoke(other.gameObject);
        }
    }
}