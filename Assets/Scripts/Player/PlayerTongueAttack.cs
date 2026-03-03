using UnityEngine;

public class PlayerTongueAttack : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh;

    [Header("Tongue gameplay variables")]
    [SerializeField] private float extendSpeed = 10f;
    [SerializeField] private float maxExtendTime = 0.5f;
    [SerializeField] private float retractionTimeReductionFactor = 2f;

    [Header("Tongue Visual")]
    [SerializeField] private float tongueWidth = 0.3f;
    [SerializeField] private float tongueHeight = 1.4f;
     
    private float currentLength = 0f;
    private float extendTimer = 0f;
    private float maxTongueLength = 0f;
    private float retractDuration = 0f;

    private bool extending = false;
    private bool retracting = false;

    /// <summary>
    /// Returns true if tongue is currently extending or retracting
    /// </summary>
    public bool IsActive => extending || retracting;

    /// <summary>
    /// Event invoked when tongue finishes retracting
    /// </summary>
    public System.Action OnTongueFinished;

    /// <summary>
    /// Starts tongue extension
    /// </summary>
    public void StartExtend()
    {
        if (IsActive) return;

        extending = true;
        retracting = false;
        currentLength = 0f;
        extendTimer = 0f;

        // Maximum length of tongue determined by extendSpeed and maxExtendTime
        maxTongueLength = extendSpeed * maxExtendTime;
    }

    /// <summary>
    /// Starts tongue retraction
    /// </summary>
    public void StartRetract()
    {
        if (retracting) return;

        extending = false;
        retracting = true;

        retractDuration = extendTimer / retractionTimeReductionFactor;

        if (retractDuration <= 0f) retractDuration = 0.05f; // prevent division by 0 safety fallback
        extendTimer = 0f;
    }

    private void Update()
    {
        if (extending)
        {
            float delta = extendSpeed * Time.deltaTime;
            currentLength += delta;
            extendTimer += Time.deltaTime;

            if (currentLength >= maxTongueLength)
            {
                // If tongue at maxTongueLength, stop extending
                currentLength = maxTongueLength;
                extending = false;
            }
        } else if (retracting)
        {
            extendTimer += Time.deltaTime;
            currentLength = Mathf.Lerp(maxTongueLength, 0f, extendTimer / retractDuration);

            if (extendTimer >= retractDuration)
            {
                currentLength = 0f;
                retracting = false;
                
                OnTongueFinished?.Invoke(); // notify subscribers
            }
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, tongueWidth, currentLength);
        tongueMesh.localPosition = new Vector3(0f, tongueHeight, currentLength);
    }
}