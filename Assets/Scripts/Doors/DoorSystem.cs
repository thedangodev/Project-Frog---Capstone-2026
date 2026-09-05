using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

/// <summary>
/// DoorSystem
/// - Configure pairs of door GameObjects and trigger GameObjects
/// - Each pair has a minimum wave (and optional maximum wave) when it becomes active
/// - Doors open automatically when the configured wave is reached (via Animator or by disabling colliders)
/// - When the player passes through a child trigger (e.g. a 'Close' child), the door will be closed behind them
///   (colliders/renderers restored or Animator close trigger invoked).
///
/// Usage:
/// - Create DoorSystem on a manager GameObject and add entries in the inspector.
/// - Set the WaveRoundSystem reference (optional; will be found automatically if null).
/// - Triggers must have a Collider set to isTrigger. The script will attach a small relay
///   component to each trigger at runtime to listen for player entry (used to close doors after opening).
/// </summary>
public class DoorSystem : MonoBehaviour
{
    [Serializable]
    public class DoorLink
    {
        [Tooltip("The door GameObject to hide/disable or destroy when the wave opens it.")]
        public GameObject door;

        [Tooltip("A root trigger GameObject; child trigger colliders under this or under the door will be used to close the door.")]
        //public GameObject openTrigger;
        public GameObject closeTrigger;

        [Tooltip("Minimum wave number (1-based) when this door becomes active/opened.")]
        public int minWave = 1;

        [Tooltip("Optional maximum wave (1-based). Set to 0 for no maximum.")]
        public int maxWave = 0;

        //[Tooltip("If true the door GameObject will be destroyed when opened and cannot be restored.")]
        //public bool destroyDoor = false;

        [Tooltip("Optional Animator. If present and has a bool 'Open' parameter, it will be used to animate opening.")]
        public Animator doorAnimator;

        [Tooltip("How far this door moves when opened. Positive = down, Negative = up.")]
        public float lowerDistance = 5f;

        [NonSerialized]
        public Vector3 originalPosition;

        [NonSerialized]
        public bool opened = false;

        //// OLD - no longer used
        //[NonSerialized]
        //public List<Collider> disabledColliders = new List<Collider>();

        //// OLD - no longer used
        //[NonSerialized]
        //public List<Renderer> disabledRenderers = new List<Renderer>();

        //[NonSerialized]
        //public bool destroyed = false;

        // If true the player manually closed/restored the door; automatic reopening will be blocked until reset.
        [NonSerialized]
        public bool playerClosed = false;

        // How long (seconds) to wait after a player-initiated close before allowing automatic reopen
        public float reopenCooldown = 1.0f;
        [Tooltip("Door will not open until Ready is true.")]
        public bool ready = false;
        [NonSerialized]
        public float lastClosedTime = -Mathf.Infinity;

        [Tooltip("Speed the door lowers.")]
        public float lowerSpeed = 2f;

        [Tooltip("Speed the door rises back up.")]
        public float riseSpeed = 1f;

        [NonSerialized]
        public Coroutine moveRoutine;

        [Header("FMod Events")]
        [SerializeField] public EventReference doorOpenEvent;
        [SerializeField] public EventReference doorCloseEvent;
        [SerializeField] public EventReference narratorAreaTransitionEvent;

        [NonSerialized]
        public bool narratorPlayed = false;
    }
    public void SetDoorReady(int index)
    {
        if (index < 0 || index >= links.Length)
            return;

        links[index].ready = true;

        //Debug.Log("DoorSystem: Door index " + index + " is now ready.");
    }

    [Header("Door Links")]
    [SerializeField] private DoorLink[] links = Array.Empty<DoorLink>();

    [Header("References")]
    [SerializeField] private WaveRoundSystem waveRoundSystem;
    [SerializeField] private int currentWave;

    [Header("Animation Settings")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string wiggleStateName = "DoorWiggle";
    [SerializeField] private string wiggleTriggerName = "Wiggle";

    private void Awake()
    {
        // Auto-assign Animator if not set in Inspector
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }

        if (waveRoundSystem == null)
            waveRoundSystem = FindAnyObjectByType<WaveRoundSystem>();

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            if (link == null) continue;

            //if (link.openTrigger != null)
                //AttachRelaysToTriggers(link.openTrigger, i);

            // ADD THIS
            if (link.closeTrigger != null)
                AttachRelaysToTriggers(link.closeTrigger, i);

            if (link.door != null)
            {
                AttachRelaysToTriggers(link.door, i);

                link.originalPosition = link.door.transform.position;
            }
        }
    }

    private void AttachRelaysToTriggers(GameObject root, int index)
    {
        var cols = root.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (!c.isTrigger) continue;

            var go = c.gameObject;
            var relay = go.GetComponent<DoorTriggerRelay>();
            if (relay == null) relay = go.AddComponent<DoorTriggerRelay>();
            relay.owner = this;
            relay.index = index;
        }
    }

    private void Update()
    {
        currentWave = waveRoundSystem.CurrentWave;
        //int currentWave = GetCurrentWaveSafe();
        //if (currentWave == 0) return;

        for (int i = 0; i < links.Length; i++)
        {
            var link = links[i];
            // Wait until this door is marked ready.

            if (link == null) continue;
            if (link.opened) continue;

            // if player manually closed the door, don't auto-open it
            if (link.playerClosed) continue;

            // honor recent player close cooldown to avoid immediate re-open
            if (Time.time - link.lastClosedTime < link.reopenCooldown) continue;

            if (currentWave < link.minWave) continue;
            if (link.maxWave > 0 && currentWave > link.maxWave) continue;

            // Wait until this door is marked ready.
            if (!link.ready)
                continue;

            OpenDoor(link);
        }
    }

    //private int GetCurrentWaveSafe()
    //{
    //    if (waveRoundSystem == null) return 0;
    //    try { return waveRoundSystem.CurrentWaveNumber; }
    //    catch { return 0; }
    //}
    private void MoveDoor(DoorLink link, Vector3 target, float speed, EventReference sound)
    {
        if (link.moveRoutine != null)
            StopCoroutine(link.moveRoutine);

        link.moveRoutine = StartCoroutine(MoveDoorRoutine(link, target, speed, sound));
    }


    private IEnumerator MoveDoorRoutine(DoorLink link, Vector3 target, float speed, EventReference sound)
    {
        if (link.door == null)
            yield break;

        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound, link.door.transform.position);

        while (Vector3.Distance(link.door.transform.position, target) > 0.01f)
        {
            link.door.transform.position = Vector3.MoveTowards(
                link.door.transform.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }

        link.door.transform.position = target;
    }

    private void OpenDoor(DoorLink link)
    {
        if (link.door == null)
        {
            link.opened = true;
            return;
        }

        StartCoroutine(OpenDoorRoutine(link));

        /*
        // Play DoorWiggle animation before lowering if an Animator is available
        Animator anim = link.door.GetComponent<Animator>();
        if (anim == null && doorAnimator != null) anim = doorAnimator;

        if (anim != null)
        {
            anim.Play(wiggleStateName, 0, 0f);
        }

        // Lower the door relative to original position
        Vector3 target = link.originalPosition + Vector3.down * link.lowerDistance;
        MoveDoor(link, target, link.lowerSpeed);
        link.opened = true;
        */

        /*
        if (link.doorAnimator != null)
        {
            bool hasBoolOpen = false;

            try
            {
                foreach (var p in link.doorAnimator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Bool && p.name == "Open")
                    {
                        hasBoolOpen = true;
                        break;
                    }
                }
            }
            catch { }

            if (hasBoolOpen)
            {
                link.doorAnimator.SetBool("Open", true);
                link.opened = true;
                //Debug.Log($"DoorSystem: Door '{link.door?.name}' animated open.");
                return;
            }
        }

        if (link.door == null)
        {
            link.opened = true;
            return;
        }*/

        //if (link.destroyDoor)
        //{
        //    Destroy(link.door);
        //    link.destroyed = true;
        //    link.opened = true;
        //    Debug.Log($"DoorSystem: Door '{link.door?.name}' destroyed for wave.");
        //    return;
        //}

        /*
        // ORIGINAL CODE (disabled components)

        link.disabledColliders.Clear();
        link.disabledRenderers.Clear();

        var cols = link.door.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c.isTrigger) continue;

            if (c.enabled)
            {
                c.enabled = false;
                link.disabledColliders.Add(c);
            }
        }

        var renders = link.door.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renders)
        {
            if (r == null) continue;

            if (r.enabled)
            {
                r.enabled = false;
                link.disabledRenderers.Add(r);
            }
        }
        */

        // NEW: Lower the door instead of disabling anything.
        //Vector3 target = link.originalPosition + Vector3.down * link.lowerDistance;

        //MoveDoor(link, target, link.lowerSpeed);
        //link.opened = true;

        //Debug.Log($"DoorSystem: Door '{link.door?.name}' lowered by {link.lowerDistance} units.");
    }

    private IEnumerator OpenDoorRoutine(DoorLink link)
    {
        // 1. Resolve Animator reference: local link animator, or global fallback
        Animator anim = link.door.GetComponent<Animator>();
        if (anim == null && doorAnimator != null)
        {
            anim = doorAnimator;
        }

        // 2. Trigger animation and wait for clip duration
        if (anim != null && !string.IsNullOrEmpty(wiggleTriggerName))
        {
            anim.SetTrigger(wiggleTriggerName);

            // Wait one frame so Animator transitions into the new state
            yield return null;

            // Get the current animation clip length and wait for it to finish
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length);
        }

        // 3. Lower door position after animation completes
        Vector3 target = link.originalPosition + Vector3.down * link.lowerDistance;
        MoveDoor(link, target, link.lowerSpeed, link.doorOpenEvent);
        link.opened = true;
    }

    private void CloseDoor(DoorLink link)
    {
        if (link == null) return;

        // Immediately stop opening and disable ready state
        link.ready = false;

        // Stop active movement routine if currently opening or moving
        if (link.moveRoutine != null)
        {
            StopCoroutine(link.moveRoutine);
            link.moveRoutine = null;
        }

        // Start closing sequence with animation wait
        StartCoroutine(CloseDoorRoutine(link));

        /*
        //if (link.destroyed)
        //{
        //Debug.LogWarning($"DoorSystem: Door '{link.door?.name}' was destroyed and cannot be restored.");
            //return;
        //}

        //if (link.destroyed)
        //    return;
        // Reactivate GameObject if necessary
        //if (!link.door.activeInHierarchy)
        //{
        //    link.door.SetActive(true);
        //}

        // Immediately stop opening and disable ready
        link.ready = false;

        // Stop current movement if it is opening
        if (link.moveRoutine != null)
        {
            //Debug.LogWarning($"DoorSystem: Door '{link.door?.name}'was lowering now stoped");

            StopCoroutine(link.moveRoutine);
            link.moveRoutine = null;
        }
        //// Restore recorded components
        //foreach (var c in link.disabledColliders)
        //{
        //    if (c == null) continue;
        //    c.enabled = true;
        //}

        //foreach (var r in link.disabledRenderers)
        //{
        //    if (r == null) continue;
        //    r.enabled = true;
        //}
        //try
        //{
        //    var mrs = link.door.GetComponentsInChildren<MeshRenderer>(true);
        //    foreach (var mr in mrs) if (mr != null) mr.enabled = true;

        //    var sk = link.door.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        //    foreach (var sr in sk) if (sr != null) sr.enabled = true;

        //    var bcs = link.door.GetComponentsInChildren<BoxCollider>(true);
        //    foreach (var bc in bcs) if (bc != null) bc.enabled = true;

        //    var col2 = link.door.GetComponentsInChildren<Collider2D>(true);
        //    foreach (var c2 in col2) if (c2 != null) c2.enabled = true;
        //}
                //catch { }

        //link.disabledColliders.Clear();
        //link.disabledRenderers.Clear();
        //link.opened = false;
        //link.lastClosedTime = Time.time;
        //link.playerClosed = true;
        //Debug.Log($"DoorSystem: Door '{link.door?.name}' restored (closed) after player passed.");
        //if (link.door == null)
        //    return;

        // Move back up immediately from current position
        MoveDoor(
            link,
            link.originalPosition,
            link.riseSpeed
        );

        link.opened = false;
        link.lastClosedTime = Time.time;
        link.playerClosed = true;
        //Debug.Log($"DoorSystem: Door '{link.door?.name}' restored (closed) after player passed.");

        if (waveRoundSystem != null)
            waveRoundSystem.OnPlayerReachedNextArea();
        */
    }

    private IEnumerator CloseDoorRoutine(DoorLink link)
    {
        if (link.door != null)
        {
            // 1. Resolve Animator reference (local link animator or global fallback)
            Animator anim = link.door.GetComponent<Animator>();
            if (anim == null && doorAnimator != null)
            {
                anim = doorAnimator;
            }

            // 2. Play wiggle animation and wait for completion
            if (anim != null && !string.IsNullOrEmpty(wiggleTriggerName))
            {
                anim.SetTrigger(wiggleTriggerName);

                // Wait one frame so Animator transitions into the wiggle state
                yield return null;

                // Wait for clip duration
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSeconds(stateInfo.length);
            }

            // 3. Move door back up from current position to original position
            MoveDoor(
                link,
                link.originalPosition,
                link.riseSpeed,
                link.doorCloseEvent
            );

            
        }

        // 4. Update tracking flags and state
        link.opened = false;
        link.lastClosedTime = Time.time;
        link.playerClosed = true;

        // 5. Notify round system
        if (waveRoundSystem != null)
        {
            waveRoundSystem.OnPlayerReachedNextArea();
        }
    }

    internal void OnTriggerActivated(int index)
    {
        if (index < 0 || index >= links.Length) return;
        var link = links[index];

        if (!link.narratorPlayed && !link.narratorAreaTransitionEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(link.narratorAreaTransitionEvent, link.door != null ? link.door.transform.position : transform.position);
            link.narratorPlayed = true;
        }

        //if (link == null)
        //{
        //    Debug.LogWarning($"DoorSystem: OnTriggerActivated received invalid link index {index}.");
        //    return;
        //}

        //Debug.Log($"DoorSystem: OnTriggerActivated index={index}, door='{link.door?.name}', opened={link.opened}, destroyed={link.destroyed}, disabledColliders={link.disabledColliders.Count}, disabledRenderers={link.disabledRenderers.Count}");

        //if (link.door == null)
        //{
        //    Debug.LogWarning($"DoorSystem: OnTriggerActivated link {index} has no door assigned.");
        //    return;
        //}

        CloseDoor(link);
    }
    // Optional API: allow external code to re-enable automatic opening for a link (e.g., after some time)
    public void ResetPlayerClosed(int linkIndex)
    {
        if (linkIndex < 0 || linkIndex >= links.Length) return;
        links[linkIndex].playerClosed = false;
    }

    private class DoorTriggerRelay : MonoBehaviour
    {
        public DoorSystem owner;
        public int index;
        private void OnTriggerEnter(Collider other)
        {
            if (owner == null) return;
            if (other.CompareTag("Player")) owner.OnTriggerActivated(index);
        }
    }
}

///// <summary>
///// DoorSystem
///// - Configure pairs of door GameObjects and trigger GameObjects
///// - Each pair has a minimum wave (and optional maximum wave) when it becomes active
///// - Doors open automatically when the configured wave is reached (via Animator or by disabling colliders)
///// - When the player passes through a child trigger (e.g. a 'Close' child), the door will be closed behind them
/////   (colliders/renderers restored or Animator close trigger invoked).
/////
///// Usage:
///// - Create DoorSystem on a manager GameObject and add entries in the inspector.
///// - Set the WaveRoundSystem reference (optional; will be found automatically if null).
///// - Triggers must have a Collider set to isTrigger. The script will attach a small relay
/////   component to each trigger at runtime to listen for player entry (used to close doors after opening).
///// </summary>
//public class DoorSystem : MonoBehaviour
//{
//    [Serializable]
//    public class DoorLink
//    {
//        [Tooltip("The door GameObject to hide/disable or destroy when the wave opens it.")]
//        public GameObject door;

//        [Tooltip("A root trigger GameObject; child trigger colliders under this or under the door will be used to close the door.")]
//        public GameObject openTrigger;
//        public GameObject closeTrigger;

//        [Tooltip("Minimum wave number (1-based) when this door becomes active/opened.")]
//        public int minWave = 1;

//        [Tooltip("Optional maximum wave (1-based). Set to 0 for no maximum.")]
//        public int maxWave = 0;

//        [Tooltip("If true the door GameObject will be destroyed when opened and cannot be restored.")]
//        public bool destroyDoor = false;

//        [Tooltip("Optional Animator. If present and has a bool 'Open' parameter, it will be used to animate opening.")]
//        public Animator doorAnimator;

//        [NonSerialized]
//        public bool opened = false;

//        // components we disabled when opening so we can restore them later
//        [NonSerialized]
//        public List<Collider> disabledColliders = new List<Collider>();

//        [NonSerialized]
//        public List<Renderer> disabledRenderers = new List<Renderer>();

//        [NonSerialized]
//        public bool destroyed = false;

//        // If true the player manually closed/restored the door; automatic reopening will be blocked until reset.
//        [NonSerialized]
//        public bool playerClosed = false;

//        // How long (seconds) to wait after a player-initiated close before allowing automatic reopen
//        public float reopenCooldown = 1.0f;

//        [NonSerialized]
//        public float lastClosedTime = -Mathf.Infinity;
//    }

//    [Header("Door Links")]
//    [SerializeField] private DoorLink[] links = Array.Empty<DoorLink>();

//    [Header("References")]
//    [SerializeField] private WaveRoundSystem waveRoundSystem;

//    private void Awake()
//    {
//        if (waveRoundSystem == null)
//            waveRoundSystem = FindAnyObjectByType<WaveRoundSystem>();

//        // attach relays to any trigger colliders under both the configured trigger root and the door itself
//        for (int i = 0; i < links.Length; i++)
//        {
//            var link = links[i];
//            if (link == null) continue;

//            if (link.openTrigger != null)
//                AttachRelaysToTriggers(link.openTrigger, i);

//            if (link.door != null)
//                AttachRelaysToTriggers(link.door, i);
//        }
//    }

//    private void AttachRelaysToTriggers(GameObject root, int index)
//    {
//        var cols = root.GetComponentsInChildren<Collider>(true);
//        foreach (var c in cols)
//        {
//            if (c == null) continue;
//            if (!c.isTrigger) continue;

//            var go = c.gameObject;
//            var relay = go.GetComponent<DoorTriggerRelay>();
//            if (relay == null) relay = go.AddComponent<DoorTriggerRelay>();
//            relay.owner = this;
//            relay.index = index;
//        }
//    }

//    private void Update()
//    {
//        int currentWave = GetCurrentWaveSafe();
//        if (currentWave == 0) return;

//        for (int i = 0; i < links.Length; i++)
//        {
//            var link = links[i];
//            if (link == null) continue;
//            if (link.opened || link.destroyed) continue;

//            // if player manually closed the door, don't auto-open it
//            if (link.playerClosed) continue;

//            // honor recent player close cooldown to avoid immediate re-open
//            if (Time.time - link.lastClosedTime < link.reopenCooldown) continue;

//            if (currentWave < link.minWave) continue;
//            if (link.maxWave > 0 && currentWave > link.maxWave) continue;

//            OpenDoor(link);
//        }
//    }

//    private int GetCurrentWaveSafe()
//    {
//        if (waveRoundSystem == null) return 0;
//        try { return waveRoundSystem.CurrentWaveNumber; }
//        catch { return 0; }
//    }

//    private void OpenDoor(DoorLink link)
//    {
//        if (link.doorAnimator != null)
//        {
//            bool hasBoolOpen = false;
//            try
//            {
//                foreach (var p in link.doorAnimator.parameters)
//                {
//                    if (p.type == AnimatorControllerParameterType.Bool && p.name == "Open") { hasBoolOpen = true; break; }
//                }
//            }
//            catch { }

//            if (hasBoolOpen)
//            {
//                link.doorAnimator.SetBool("Open", true);
//                link.opened = true;
//                Debug.Log($"DoorSystem: Door '{link.door?.name}' animated open.");
//                return;
//            }
//        }

//        if (link.door == null)
//        {
//            link.opened = true;
//            return;
//        }

//        if (link.destroyDoor)
//        {
//            Destroy(link.door);
//            link.destroyed = true;
//            link.opened = true;
//            Debug.Log($"DoorSystem: Door '{link.door?.name}' destroyed for wave.");
//            return;
//        }

//        // disable non-trigger colliders and renderers, record them for restore
//        link.disabledColliders.Clear();
//        link.disabledRenderers.Clear();

//        var cols = link.door.GetComponentsInChildren<Collider>(true);
//        foreach (var c in cols)
//        {
//            if (c == null) continue;
//            if (c.isTrigger) continue;
//            if (c.enabled)
//            {
//                c.enabled = false;
//                link.disabledColliders.Add(c);
//            }
//        }

//        var renders = link.door.GetComponentsInChildren<Renderer>(true);
//        foreach (var r in renders)
//        {
//            if (r == null) continue;
//            if (r.enabled)
//            {
//                r.enabled = false;
//                link.disabledRenderers.Add(r);
//            }
//        }

//        link.opened = true;
//        Debug.Log($"DoorSystem: Door '{link.door?.name}' opened (components disabled) for wave.");
//    }

//    internal void OnTriggerActivated(int index)
//    {
//        if (index < 0 || index >= links.Length) return;
//        var link = links[index];
//        if (link == null)
//        {
//            Debug.LogWarning($"DoorSystem: OnTriggerActivated received invalid link index {index}.");
//            return;
//        }

//        Debug.Log($"DoorSystem: OnTriggerActivated index={index}, door='{link.door?.name}', opened={link.opened}, destroyed={link.destroyed}, disabledColliders={link.disabledColliders.Count}, disabledRenderers={link.disabledRenderers.Count}");

//        if (link.door == null)
//        {
//            Debug.LogWarning($"DoorSystem: OnTriggerActivated link {index} has no door assigned.");
//            return;
//        }

//        CloseDoor(link);
//    }

//    private void CloseDoor(DoorLink link)
//    {
//        if (link.destroyed)
//        {
//            Debug.LogWarning($"DoorSystem: Door '{link.door?.name}' was destroyed and cannot be restored.");
//            return;
//        }

//        // Reactivate GameObject if necessary
//        if (!link.door.activeInHierarchy)
//        {
//            link.door.SetActive(true);
//        }

//        // Restore recorded components
//        foreach (var c in link.disabledColliders)
//        {
//            if (c == null) continue;
//            c.enabled = true;
//        }

//        foreach (var r in link.disabledRenderers)
//        {
//            if (r == null) continue;
//            r.enabled = true;
//        }

//        // Also explicitly try common types in case they weren't recorded
//        try
//        {
//            var mrs = link.door.GetComponentsInChildren<MeshRenderer>(true);
//            foreach (var mr in mrs) if (mr != null) mr.enabled = true;

//            var sk = link.door.GetComponentsInChildren<SkinnedMeshRenderer>(true);
//            foreach (var sr in sk) if (sr != null) sr.enabled = true;

//            var bcs = link.door.GetComponentsInChildren<BoxCollider>(true);
//            foreach (var bc in bcs) if (bc != null) bc.enabled = true;

//            var col2 = link.door.GetComponentsInChildren<Collider2D>(true);
//            foreach (var c2 in col2) if (c2 != null) c2.enabled = true;
//        }
//        catch { }

//        link.disabledColliders.Clear();
//        link.disabledRenderers.Clear();
//        link.opened = false;
//        link.lastClosedTime = Time.time;
//        link.playerClosed = true;
//        Debug.Log($"DoorSystem: Door '{link.door?.name}' restored (closed) after player passed.");
//    }

//    // Optional API: allow external code to re-enable automatic opening for a link (e.g., after some time)
//    public void ResetPlayerClosed(int linkIndex)
//    {
//        if (linkIndex < 0 || linkIndex >= links.Length) return;
//        links[linkIndex].playerClosed = false;
//    }

//    private class DoorTriggerRelay : MonoBehaviour
//    {
//        public DoorSystem owner;
//        public int index;
//        private void OnTriggerEnter(Collider other)
//        {
//            if (owner == null) return;
//            if (other.CompareTag("Player")) owner.OnTriggerActivated(index);
//        }
//    }
//}
