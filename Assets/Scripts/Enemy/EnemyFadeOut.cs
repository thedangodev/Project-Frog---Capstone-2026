using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using FMODUnity;

// Drives the fade out system which calls Die() from Health.cs in order to play both cleanly fade Enemies from the scene, and - if applicable - play a death animation. -E.M
public class EnemyFadeOut : MonoBehaviour
{
    // How held weapons are handled when the enemy dies. Two choices available;
    public enum WeaponDeathMode
    {
        FadeWithBody,        // Weapon renderers are folded into the body fade and dissolve together.
        DisableImmediately   // Weapon is switched off the moment Die() runs.
    }

    [Header("Death Rig Swap (optional)")]
    [Tooltip("Optional. Mesh/rig to DISABLE on death (e.g. the live animated TPose rig). Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private GameObject meshToDisable;
    [Tooltip("Optional. Mesh/rig to ENABLE on death (e.g. the fall-apart corpse rig). Should share the same parent transform as the disabled mesh so it appears in place. Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private GameObject meshToEnable;
    [Tooltip("Optional. Animator whose Avatar is cleared on death (e.g. the live rig's Animator). Cleared only at death so the live rig's animations still play normally while alive. Leave empty on enemies that don't swap rigs.")]
    [SerializeField] private Animator avatarToClear;

    [Header("Death Animation")]
    [Tooltip("Left empty = auto-filled on Awake (from the swap-in mesh if one is assigned, otherwise from this object).")]
    [SerializeField] private Animator animator;
    private static readonly int IsDeadHash = Animator.StringToHash("isDead");

    [Header("Disable On Death")]
    [Tooltip("Behaviour scripts to switch off on death (AI, movement, attack) to prevent phantom movement. Left empty = auto-filled from EnemyBase.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    [Tooltip("Halted on death so residual velocity / last destination doesn't keep the enemy drifting. Left empty = auto-filled from this object.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Held Weapons")]
    [Tooltip("Weapons parented to bones (e.g. the SpearPrefab under RightHand). These sit outside the main renderer array, so list them here.")]
    [SerializeField] private GameObject[] weaponObjects;
    [Tooltip("FadeWithBody dissolves the weapon alongside the corpse. DisableImmediately just switches it off at the moment of death.")]
    [SerializeField] private WeaponDeathMode weaponDeathMode = WeaponDeathMode.FadeWithBody;
    [Tooltip("Weapons already hidden when the enemy dies (e.g. a spear mid-flight) stay hidden rather than popping back into the hand.")]
    [SerializeField] private bool ignoreAlreadyHiddenWeapons = true;

    [Header("Fade")]
    [SerializeField] private Material deathMaterial;
    [SerializeField] private float duration = 1.0f;
    [Tooltip("Left empty = auto-filled on Awake (from the swap-in mesh if one is assigned, otherwise from all child renderers).")]
    [SerializeField] private Renderer[] renderers;
    [Tooltip("Forces depth writing on the transparent death material so the far side of the mesh doesn't show through the near side.")]
    [SerializeField] private bool forceDepthWriteOnFade = true;
    [Tooltip("Stops the fading corpse casting a full-strength shadow after it has visually disappeared.")]
    [SerializeField] private bool disableShadowsOnFade = true;

    [Header("Disable On Fade")]
    [Tooltip("Left empty = auto-filled from all child colliders on Awake.")]
    [SerializeField] private Collider[] collidersToDisable;

    [Header("Health Bar Disable")]
    [Tooltip("If the Enemy currently fading has a health bar - disable that shizzle homeboy!")]
    [SerializeField] private GameObject healthBar;

    [Header("FMod Events")]
    [SerializeField] private EventReference enemyDeathEvent;

    // URP Lit uses _BaseColor; some shaders (or Built-in) use _Color. Resolve per-material.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    // Original emission colors, captured at fade start so we scale from the true value rather than compounding frame to frame.
    private Color[] baseEmission;
    private bool isFading;
    private bool isDead;   // guard so Die() only runs once
    private void Awake()
    {
        // If a swap-in mesh is assigned, target its animator/renderers (it may be inactive at Awake, so include inactive).
        // Otherwise fall back to this object, preserving original behaviour for enemies that don't swap rigs.
        if (animator == null)
            animator = meshToEnable != null
                ? meshToEnable.GetComponentInChildren<Animator>(true)
                : GetComponent<Animator>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (renderers == null || renderers.Length == 0)
            renderers = meshToEnable != null
                ? meshToEnable.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>();
        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();
        if (scriptsToDisable == null || scriptsToDisable.Length == 0)
        {
            var b = GetComponent<EnemyBase>();
            if (b != null) scriptsToDisable = new MonoBehaviour[] { b };
        }
    }
    // Call this from the health system when HP hits 0.
    // Stops AI + agent + colliders, plays the baked "fall apart" animation, waits for it to finish, then fades out and destroys.
    public void Die()
    {
        if (isDead) return;   // guard against double-death
        isDead = true;

        RuntimeManager.PlayOneShot(enemyDeathEvent, transform.position);

        // Disables the Enemy's health bar as soon as the Enemy's health reaches 0.
        if (healthBar != null)
            healthBar.SetActive(false);

        // Snapshot which weapons were visible BEFORE the AI scripts are switched off - disabling a component fires its OnDisable, which may re-show a thrown weapon.
        CacheVisibleWeapons();

        // Stop the AI from steering a dying enemy (kills the per-frame MoveToTarget at the source).
        foreach (var s in scriptsToDisable)
            if (s != null) s.enabled = false;

        // Halt the NavMeshAgent so residual velocity / last destination doesn't keep the enemy drifting.
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        ApplyWeaponDeathState();

        // Done BEFORE the isDead trigger so the death animation fires on the swapped-in mesh, not the live one.
        // Both meshes should share the same parent transform, so the swap-in appears in the same position/rotation.
        // Skipped entirely when the fields are left empty (enemies that don't swap rigs are unaffected).

        // Clear the live rig's Avatar at death (not before) so its animations run normally while alive, but stop driving the shared skeleton once we hand off to the corpse rig.
        if (avatarToClear != null)
            avatarToClear.avatar = null;

        if (meshToDisable != null)
            meshToDisable.SetActive(false);
        if (meshToEnable != null)
            meshToEnable.SetActive(true);

        // Disable colliders immediately so a dying enemy stops blocking movement / registering on the tether.
        foreach (var c in collidersToDisable)
            if (c != null) c.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger(IsDeadHash);
            StartCoroutine(DeathSequence());
        }
        else
        {
            // No animator wired - skip straight to the fade.
            BeginFade();
        }
    }

    // Weapons that were visible at the moment of death. Anything hidden at that point (a spear already thrown) is left out so it doesn't reappear on the corpse.
    private readonly List<GameObject> weaponsAtDeath = new List<GameObject>();

    private void CacheVisibleWeapons()
    {
        weaponsAtDeath.Clear();
        if (weaponObjects == null) return;

        foreach (var w in weaponObjects)
        {
            if (w == null) continue;
            if (ignoreAlreadyHiddenWeapons && !w.activeInHierarchy) continue;
            weaponsAtDeath.Add(w);
        }
    }

    private void ApplyWeaponDeathState()
    {
        if (weaponDeathMode != WeaponDeathMode.DisableImmediately) return;

        foreach (var w in weaponsAtDeath)
            if (w != null) w.SetActive(false);

        weaponsAtDeath.Clear();
    }

    // Folds the weapon's renderers into the main array so they go through the exact same material swap, alpha ramp and cleanup as the body. Called just before the fade starts.
    private void MergeWeaponRenderers()
    {
        if (weaponDeathMode != WeaponDeathMode.FadeWithBody) return;
        if (weaponsAtDeath.Count == 0) return;

        var merged = new List<Renderer>(renderers ?? new Renderer[0]);

        foreach (var w in weaponsAtDeath)
        {
            if (w == null) continue;

            // Re-show the weapon if the rig swap or an OnDisable turned it off between death and fade - it was visible when the enemy died, so it should dissolve.
            if (!w.activeSelf) w.SetActive(true);

            foreach (var r in w.GetComponentsInChildren<Renderer>(true))
                if (r != null && !merged.Contains(r))
                    merged.Add(r);
        }

        renderers = merged.ToArray();
    }

    private IEnumerator DeathSequence()
    {
        // Wait for the transition into the death state to begin AND complete - while a transition is running, GetCurrentAnimatorStateInfo still reports the OUTGOING state, so reading length too early returns the locomotion clip's length.
        yield return null;
        while (animator.IsInTransition(0))
            yield return null;

        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);
        BeginFade();
    }
    public void BeginFade()
    {
        if (isFading) return;   // guard against double-trigger
        isFading = true;

        // Pull held weapons into the renderer array first so they share the whole pipeline.
        MergeWeaponRenderers();

        // Swap to the transparent death material FIRST - CaptureEmission reads from the live material, so doing this after would snapshot the wrong emission values.
        ChangeToDeathMaterial();
        CaptureEmission();
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }
    // Snapshot each renderer's starting emission color once, so ApplyAlpha can scale from the original toward black instead of reading an already-dimmed value each frame.
    private void CaptureEmission()
    {
        baseEmission = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null && r.material.HasProperty(EmissionColorId))
                baseEmission[i] = r.material.GetColor(EmissionColorId);
        }
    }
    private IEnumerator FadeRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - t / duration);
            ApplyAlpha(alpha);
            yield return null;
        }
        ApplyAlpha(0f);

        // Safety net: base-color alpha and emission are both zeroed, but hard-disable renderers so any residual specular/reflection is gone before destroy.
        foreach (var r in renderers)
            if (r != null) r.enabled = false;
        Destroy(gameObject);
    }
    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            // Every slot on the renderer, not just slot 0 - this rig has multi-submesh meshes.
            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                // Fade base color alpha.
                int propId;
                if (mat.HasProperty(BaseColorId)) propId = BaseColorId;
                else if (mat.HasProperty(ColorId)) propId = ColorId;
                else propId = 0;
                if (propId != 0)
                {
                    Color c = mat.GetColor(propId);
                    c.a = alpha;
                    mat.SetColor(propId, c);
                }

                // Fade emission toward black by the same factor so the glow dies with the surface.
                if (baseEmission != null && mat.HasProperty(EmissionColorId))
                    mat.SetColor(EmissionColorId, baseEmission[i] * alpha);
            }
        }
    }


    // Replaces EVERY material slot on every tracked renderer with the transparent death material. The single-slot version only swapped submesh 0, leaving the rest opaque.
    private void ChangeToDeathMaterial()
    {
        if (deathMaterial == null)
        {
            Debug.LogError($"[EnemyFadeOut] No deathMaterial assigned on {gameObject.name}. The enemy will pop out instead of fading.");
            return;
        }

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = deathMaterial;
            r.materials = mats;   // assigning the array instantiates per-renderer copies

            if (forceDepthWriteOnFade)
            {
                // Transparent URP materials disable ZWrite, which lets the far side of the mesh render through the near side (the "x-ray" look). Forcing it back on restores correct self-occlusion for the duration of the fade.
                var instanced = r.materials;
                for (int i = 0; i < instanced.Length; i++)
                    if (instanced[i] != null && instanced[i].HasProperty(ZWriteId))
                        instanced[i].SetInt(ZWriteId, 1);
            }

            if (disableShadowsOnFade)
                r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}