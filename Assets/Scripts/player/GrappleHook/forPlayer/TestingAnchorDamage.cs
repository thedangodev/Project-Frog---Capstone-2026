using System.Collections;
using Mono.Cecil;
using UnityEngine;

public class GrappleDamageTest1 : MonoBehaviour
{
    [Header("References")]
    public PlayerGrapple playerGrapple;

    [Header("Damage Settings")]
    public float damageRadius = 20f;
    public LayerMask enemyLayer;

    [Header("Effect Timing")]
    public float effectInterval = 10f;

    private float effectTimer;

    void Update()
    {
        if (playerGrapple == null || !playerGrapple.IsGrappling)
            return;

        GrappleTower tower = playerGrapple.currentTower;
        if (tower == null) return;

        effectTimer += Time.deltaTime;

        if (effectTimer >= effectInterval)
        {
            ApplyEffect(tower);
            effectTimer = 0f;
        }
    }

    private void ApplyEffect(GrappleTower tower)
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            damageRadius,
            enemyLayer
        );

        foreach (Collider hit in hits)
        {
            Health enemy = hit.GetComponent<Health>();
            if (enemy == null || enemy.IsDead) continue;

            switch (tower.towerType)
            {
                case TowerType.Fire:
                    StartCoroutine(ApplyBurn(enemy, tower.fireFields));
                    break;

                case TowerType.Ice:
                    ApplyIce(enemy, tower.iceFields);
                    break;

                case TowerType.Wind:
                    ApplyWind(enemy, tower.windFields);
                    break;
            }
        }
    }

    // ---------------- FIRE ----------------
    private IEnumerator ApplyBurn(Health enemy, FireTowerFields fire)
    {
        float timer = 0f;

        while (timer < fire.burnDuration && enemy != null && !enemy.IsDead)
        {
            enemy.TakeDmg(fire.damage);
            yield return new WaitForSeconds(fire.burnTickRate);
            timer += fire.burnTickRate;
        }
    }

    // ---------------- ICE ----------------
    private void ApplyIce(Health enemy, IceTowerFields ice)
    {
        float iceDamage = ice.damage * ice.damageMultiplier;
        enemy.TakeDmg(iceDamage);

        // Apply slow directly to EnemyFrogSkeleton
        EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.ApplySlow(ice.slowMultiplier, ice.slowDuration);
        }

    }


    // ---------------- WIND ----------------
    private void ApplyWind(Health enemy, WindTowerFields wind)
    {
        float windDamage = wind.damage * wind.damageMultiplier;
        enemy.TakeDmg(windDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, damageRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
