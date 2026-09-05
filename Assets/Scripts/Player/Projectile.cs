using UnityEngine;
using System.Collections;
using System.Linq;

public class Projectile : MonoBehaviour, IProjectile
{
    [SerializeField] protected float baseSpeed = 10f;
    [SerializeField] protected float baseDamage = 10f;
    [SerializeField] protected float maxScale = 2f;

    [Header("Hit VFX (assign prefabs)")]
    [SerializeField] private GameObject fireHitVfx;
    [SerializeField] private GameObject iceHitVfx;
    [SerializeField] private GameObject windHitVfx;
    [SerializeField] private GameObject defaultHitVfx;

    public float speed;
    public float damage;
    public float chargePercent; // 0.0 to 1.0, passed from PlayerAttacks / PlayerChargeAttack

    public string effectType;
    public float effectDuration;
    public float effectValue;
    public bool isPlayerProjectile = false;

    // Context passed from PlayerAttacks / PlayerChargeAttack
    public GameObject player;
    public AnchorElement currentElement;
    public float pointBlankRange = 10f;

    // Wind Upgrade
    private bool isHoming = false;
    private bool skipAutoHoming = true; // prevents auto homing in awake    
    private float turnSpeed = 10f;
    private EnemyBase target;

    // Ice Upgrade
    public bool isPiercingProjectile = false;
    private int pierceCount = 0;
    public float pierceMultiplier = 1f;

    // Default is 0 so basic shots do not apply knockback. Charged attacks will add knockback based on charge time
    [Tooltip("Knockback distance applied to enemies when hit by player projectiles. 0 = no knockback.")]
    public float knockbackDistance = 0f;

    // Which reaction animation the struck enemy should play.
    // Defaults to Stagger so regular shots need no change; charged attacks set this to Knockback.
    [Tooltip("Reaction animation played by the enemy on hit. Stagger = regular, Knockback = charged.")]
    public HitReaction hitReaction = HitReaction.Stagger;

    // Cache arena colliders globally to avoid repeated Find calls
    private static Collider[] cachedArenaColliders = null;
    private static int cachedArenaLayer = int.MinValue; // sentinel

    private void Awake()
    {
        // If homing upgrade is active, enable homing with delay
        if (!skipAutoHoming && HomingDartsUpgrade.Instance != null && HomingDartsUpgrade.Instance.IsEnabled())
            EnableHoming();

        // Ensure projectiles ignore collisions with colliders on the "ArenaColliders" layer
        const string arenaLayerName = "ArenaColliders";
        int layerIndex = LayerMask.NameToLayer(arenaLayerName);

        if (layerIndex >= 0)
        {
            // Populate cache once per domain if needed
            if (cachedArenaLayer != layerIndex || cachedArenaColliders == null)
            {
                cachedArenaLayer = layerIndex;
                cachedArenaColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None)
                    .Where(c => c != null && c.gameObject.layer == layerIndex)
                    .ToArray();
            }

            var myColliders = GetComponentsInChildren<Collider>();
            if (cachedArenaColliders != null && cachedArenaColliders.Length > 0 && myColliders != null)
            {
                foreach (var mc in myColliders)
                {
                    if (mc == null) continue;
                    foreach (var ac in cachedArenaColliders)
                    {
                        if (ac == null) continue;
                        Physics.IgnoreCollision(mc, ac, true);
                    }
                }
            }
        }
    }

    public virtual void Initialize(float chargePercent)
    {
        // Base speed & damage scaling
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);
        this.chargePercent = chargePercent;

        // Frostwind (Ice primary speed)
        if (currentElement == AnchorElement.Ice &&
            chargePercent <= 0f &&
            FrostwindUpgrade.Instance != null)
        {
            float bonus = FrostwindUpgrade.Instance.GetBonus();
            speed *= 1f + bonus / 100f;
            Debug.Log($"[Projectile] Frostwind bonus applied: {bonus}% speed increase. New speed: {speed}");
        }

        // Searing Shot (Fire primary dart damage)
        if (effectType == "Burn" && SearingShotUpgrade.Instance != null)
        {
            float bonus = SearingShotUpgrade.Instance.GetDartBonus();
            damage *= 1f + bonus / 100f;
        }

        // Visual charge scaling
        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent);
        transform.localScale = Vector3.one * scale;

        Destroy(gameObject, 3f);
    }

    public void Init(float damage, float lifetime)
    {
        this.damage = damage;
        this.speed = baseSpeed;
        Destroy(gameObject, lifetime);
    }

    // ============================
    // HOMING LOGIC
    // ============================
    public void EnableHoming(float turnSpeed = 10f)
    {
        isHoming = true;
        this.turnSpeed = turnSpeed;
        target = FindNearestEnemy();
    }

    public void EnableHomingDelayed(float delay, float turnSpeed = 10f)
    {
        StartCoroutine(EnableHomingAfterDelay(delay, turnSpeed));
    }

    private IEnumerator EnableHomingAfterDelay(float delay, float turnSpeed)
    {
        yield return new WaitForSeconds(delay);
        EnableHoming(turnSpeed);
    }

    private EnemyBase FindNearestEnemy()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase closest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e;
            }
        }
        return closest;
    }

    protected virtual void Update()
    {
        if (isHoming && target != null)
        {
            Vector3 dir = (target.transform.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // Play configured VFX prefab at projectile position. If prefab is null, nothing happens.
    private void PlayHitVfx(GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;

        GameObject go = Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        // Try to determine particle durations to auto-destroy the spawned VFX
        float maxLifetime = 0f;
        var systems = go.GetComponentsInChildren<ParticleSystem>();
        foreach (var s in systems)
        {
            var main = s.main;
            float lifetime = main.duration;
            // Add startLifetime (handle MinMaxCurve)
            var startLifetime = main.startLifetime;
            lifetime += (startLifetime.mode == ParticleSystemCurveMode.Constant) ? startLifetime.constant : startLifetime.constantMax;
            if (lifetime > maxLifetime) maxLifetime = lifetime;
        }

        // Fallback destroy time if no particle systems found
        if (maxLifetime <= 0f) maxLifetime = 3f;

        Destroy(go, Mathf.Max(0.5f, maxLifetime));
    }

    // ============================
    // COLLISION
    // ============================
    private void OnTriggerEnter(Collider other)
    {
        // Ignore trigger colliders that aren't enemies or player
        if (!other.CompareTag("Enemy") && !other.CompareTag("Player"))
            return;

        // ============================
        // DAMAGE PLAYER IF ENEMY PROJECTILE
        // ============================
        if (!isPlayerProjectile && other.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<PlayerTakeDamage>();
            if (player != null)
            {
                Vector3 knockDir = (other.transform.position - transform.position).normalized;
                knockDir.y = 0f;
                player.TryApplyDamageAndKnockback(damage, knockDir, 5f);
            }

            Destroy(gameObject);
            return;
        }

        // ============================
        // IGNORE PLAYER IF PLAYER PROJECTILE
        // ============================
        if (isPlayerProjectile && other.CompareTag("Player"))
            return;

        // ============================
        // DAMAGE ENEMY IF PLAYER PROJECTILE

        var enemy = other.GetComponent<EnemyBase>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyBase>();

        if (enemy != null && isPlayerProjectile)
        {
            float finalDamage = damage;

            // ============================
            // POINT BLANK SHOT BONUS
            // ============================
            if (currentElement == AnchorElement.Wind &&
                PointBlankShotUpgrade.Instance != null &&
                player != null)
            {
                float bonusPercent = PointBlankShotUpgrade.Instance.GetBonus();
                Vector3 impactPoint = other.ClosestPoint(transform.position);
                float dist = Vector3.Distance(player.transform.position, impactPoint);

                if (dist <= pointBlankRange)
                {
                    finalDamage *= 1f + (bonusPercent / 100f);
                }
            }

            // ============================
            // SEARING SHOT (Fire primary bonus)
            // ============================
            if (currentElement == AnchorElement.Fire &&
                SearingShotUpgrade.Instance != null &&
                isPlayerProjectile &&
                chargePercent <= 0f)   // <--- PRIMARY ONLY
            {
                float bonus = SearingShotUpgrade.Instance.GetDartBonus();
                float before = finalDamage;

                finalDamage *= 1f + bonus / 100f;
            }

            // ----------------------
            // PYRONOVA AOE EXPLOSION
            // ----------------------
            if (currentElement == AnchorElement.Fire && chargePercent > 0f)
            {
                float aoeRadius = PyronovaUpgrade.Instance.GetAoeRadius();
                float aoePercent = PyronovaUpgrade.Instance.GetAoeDamagePercent();

                float aoeDamage = finalDamage * (aoePercent / 100f);

                Collider[] hits = Physics.OverlapSphere(enemy.transform.position, aoeRadius);

                foreach (var hit in hits)
                {
                    var aoeEnemy = hit.GetComponent<EnemyBase>();
                    if (aoeEnemy != null && aoeEnemy != enemy)
                    {
                        aoeEnemy.TakeDamage(aoeDamage);
                    }
                }
            }

            bool wasSlowed = enemy.IsSlowed;
            bool wasFrozen = enemy.IsFrozen;

            // ICE PRIMARY — SLOW
            if (currentElement == AnchorElement.Ice && chargePercent <= 0f)
            {
                enemy.ApplySlow(3f, 0.70f); // 30% slow
            }

            // ICE CHARGE — FREEZE
            if (currentElement == AnchorElement.Ice && chargePercent > 0f)
            {
                enemy.Freeze(3f);
            }

            // Cryo Fragility (bonus if slowed)
            if (currentElement == AnchorElement.Ice &&
                CryoFragilityUpgrade.Instance != null &&
                CryoFragilityUpgrade.Instance.GetBonus() > 0f &&
                (wasSlowed || wasFrozen))
            {
                float bonus = CryoFragilityUpgrade.Instance.GetBonus();
                finalDamage *= 1f + bonus / 100f;
            }

            // Shatter
            if (currentElement == AnchorElement.Ice &&
                ShatterUpgrade.Instance != null &&
                ShatterUpgrade.Instance.IsEnabled())
            {
                ShatterUpgrade.Instance.TryApplyShatter(enemy, wasFrozen);
            }

            // Apply damage + effect
            if (!isPiercingProjectile)
            {
                if (!string.IsNullOrEmpty(effectType))
                    enemy.TakeDamage(finalDamage, effectType, effectDuration, effectValue);
                else
                    enemy.TakeDamage(finalDamage);
            }

            // -------------------------
            // EXTINGUISHER BONUS DAMAGE 
            // -------------------------
            if (currentElement == AnchorElement.Fire &&
                ExtinguisherUpgrade.Instance != null &&
                ExtinguisherUpgrade.Instance.IsEnabled() &&
                enemy.IsBurning)
            {
                float extra = ExtinguisherUpgrade.Instance.GetBonusDamage();
                enemy.TakeDamage(extra);
            }

            // -------------------------
            // HIT REACTION ANIMATION
            // Tell the frog (if this enemy is one) which reaction to play.
            // Stagger for regular shots, KnockbackReact for charged shots.
            // -------------------------
            //var frog = enemy.GetComponentInParent<EnemyFrogSkeleton>();
            //if (frog != null)
            //{
            //    //Debug.Log($"[Projectile] Hit reaction = {hitReaction} on instance {GetInstanceID()} (charge% {chargePercent})");
            //    frog.PlayHitReaction(hitReaction);
            //}
            enemy.PlayHitReaction(hitReaction);

            // Apply knockback to enemy only if knockbackDistance > 0
            if (knockbackDistance > 0f)
            {
                Vector3 pushDir = (enemy.transform.position - transform.position);
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude > 0.0001f)
                {
                    pushDir.Normalize();
                    var enemyKnock = enemy.GetComponentInParent<EnemyKnockback>();
                    if (enemyKnock != null)
                    {
                        enemyKnock.ApplyKnockback(pushDir, knockbackDistance);
                    }
                    else
                    {
                        var rb = other.attachedRigidbody ?? enemy.GetComponentInParent<Rigidbody>();
                        if (rb != null)
                        {
                            if (rb.isKinematic)
                                rb.MovePosition(rb.position + pushDir * knockbackDistance);
                            else
                                rb.AddForce(pushDir * knockbackDistance, ForceMode.Impulse);
                        }
                        else
                        {
                            var root = other.transform.root;
                            root.position += pushDir * knockbackDistance;
                        }
                    }
                }
            }

            // Play corresponding VFX only when hitting an enemy
            if (!string.IsNullOrEmpty(effectType))
            {
                if (effectType == "Burn")
                    PlayHitVfx(fireHitVfx);
                else if (effectType == "Freeze" || effectType.ToLower().Contains("ice"))
                    PlayHitVfx(iceHitVfx);
                else if (effectType.ToLower().Contains("wind") || effectType.ToLower().Contains("knock"))
                    PlayHitVfx(windHitVfx);
                else
                    PlayHitVfx(defaultHitVfx);
            }
            else
            {
                // No power-up: optional default dart VFX (may be left null)
                PlayHitVfx(defaultHitVfx);
            }

            // ---------------
            // LETHAL PIERCING
            // ---------------
            if (isPiercingProjectile)
            {
                float baseMultiplier;

                if (LethalPiercingUpgrade.Instance != null &&
                    LethalPiercingUpgrade.Instance.GetBonus() > 0f)
                {
                    baseMultiplier = LethalPiercingUpgrade.Instance.GetBonus() / 100f;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }

                // Apply multiplier BEFORE damage is applied
                finalDamage *= pierceMultiplier;

                // Apply damage now
                enemy.TakeDamage(finalDamage);

                // Update multiplier for next hit
                pierceMultiplier *= baseMultiplier;

                pierceCount++;

                return;
            }

            Destroy(gameObject);
            return;
        }

        // ============================================================
        // DAMAGE ANYTHING WITH HEALTH (TARGET DUMMY SUPPORT)
        // ============================================================
        var hp = other.GetComponent<Health>();
        if (hp != null && isPlayerProjectile)
        {
            Debug.Log($"[Projectile] Hit {other.name} for {damage} dmg");

            if (!string.IsNullOrEmpty(effectType))
                hp.TakeDmg(damage, effectType, effectDuration, effectValue);
            else
                hp.TakeDmg(damage);

            Destroy(gameObject);
            return;
        }
        // Destroy on hitting walls or other objects
        Destroy(gameObject);
    }
}