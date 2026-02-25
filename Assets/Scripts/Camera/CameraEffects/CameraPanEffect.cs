using UnityEngine;

public class CameraPanEffect : CameraEffectBase
{
    [Header("Pan Settings")]
    [SerializeField] private float panTime = 1.0f;
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private bool usePlayerOffset = true;

    [Header("Player Control")]
    [SerializeField] private bool pausePlayerDuringPan = true;

    private enum State { Idle, PanningToPOI, Holding, Returning }
    private State state = State.Idle;

    private Transform pointOfInterest;
    private float timer;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 offsetFromPlayer;
    private Transform playerTransform;

    private CameraController controller;
    private PlayerMovement playerMovement;
    private bool playerPaused;

    private void Awake()
    {
        controller = GetComponentInParent<CameraController>();
        // cache player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerMovement = playerTransform != null ? playerTransform.GetComponent<PlayerMovement>() : null;
    }

    private void OnEnable()
    {
        if (controller == null)
            controller = GetComponentInParent<CameraController>();

        controller?.AddEffect(this);
    }

    private void OnDisable()
    {
        controller?.RemoveEffect(this);
        if (playerPaused)
            ResumePlayer();
    }

    public override Vector3 ApplyEffect(float deltaTime)
    {
        if (state == State.Idle || pointOfInterest == null)
            return Vector3.zero;

        timer += deltaTime;

        Vector3 desiredPosition = originalPosition;
        Quaternion desiredRotation = originalRotation;

        if (state == State.PanningToPOI)
        {
            float t = Mathf.Clamp01(timer / Mathf.Max(0.0001f, panTime));
            t = EaseInOutQuad(t);

            desiredPosition = Vector3.Lerp(originalPosition, targetPosition, t);
            desiredRotation = Quaternion.Slerp(originalRotation, targetRotation, t);

            if (t >= 1f)
            {
                state = holdTime > 0f ? State.Holding : State.Returning;
                timer = 0f;
            }
        }
        else if (state == State.Holding)
        {
            desiredPosition = targetPosition;
            desiredRotation = targetRotation;

            if (timer >= holdTime)
            {
                state = State.Returning;
                timer = 0f;
            }
        }
        else if (state == State.Returning)
        {
            float t = Mathf.Clamp01(timer / Mathf.Max(0.0001f, panTime));
            t = EaseInOutQuad(t);

            desiredPosition = Vector3.Lerp(targetPosition, originalPosition, t);
            desiredRotation = Quaternion.Slerp(targetRotation, originalRotation, t);

            if (t >= 1f)
            {
                state = State.Idle;
                timer = 0f;
                pointOfInterest = null;
            }
        }

        transform.rotation = desiredRotation;

        Vector3 effectOffset = (controller != null)
            ? desiredPosition - controller.GetBasePosition()
            : desiredPosition - transform.position;

        if (state == State.Idle && playerPaused)
            ResumePlayer();

        return effectOffset;
    }

    public void TriggerPan(Transform poi, float time)
    {
        if (poi == null || time <= 0f)
        {
            Debug.LogWarning("CameraPanEffect.TriggerPan: invalid parameters.");
            return;
        }

        pointOfInterest = poi;
        panTime = time;

        // ensure player references are available
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            playerMovement = playerTransform != null ? playerTransform.GetComponent<PlayerMovement>() : null;
        }

        if (controller == null)
            controller = GetComponentInParent<CameraController>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        if (usePlayerOffset && playerTransform != null)
        {
            offsetFromPlayer = originalPosition - playerTransform.position;
            targetPosition = pointOfInterest.position + offsetFromPlayer;
        }
        else
        {
            targetPosition = pointOfInterest.position + (originalPosition - (playerTransform != null ? playerTransform.position : Vector3.zero));
        }

        Vector3 lookDir = pointOfInterest.position - targetPosition;
        targetRotation = (lookDir.sqrMagnitude <= 0.0001f) ? originalRotation : Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        if (pausePlayerDuringPan && !playerPaused && playerMovement != null)
        {
            playerMovement.StopMovement();
            playerPaused = true;
        }

        timer = 0f;
        state = State.PanningToPOI;
    }

    private void ResumePlayer()
    {
        if (!playerPaused)
            return;

        if (playerMovement == null && playerTransform != null)
            playerMovement = playerTransform.GetComponent<PlayerMovement>();

        playerMovement?.ResumeMovement();
        playerPaused = false;
    }

    private static float EaseInOutQuad(float t)
    {
        if (t < 0.5f) return 2f * t * t;
        return -1f + (4f - 2f * t) * t;
    }

    public bool IsPanning => state != State.Idle;
}