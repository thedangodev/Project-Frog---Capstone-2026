using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [SerializeField] private Image foregroundImage;
    [SerializeField] private Transform uiContainer;
    [SerializeField] private Health health;


    // ============================================================
    // HEALTH
    // ============================================================

    [Header("Health")]

    [SerializeField]
    [Min(0f)]
    private float maxHealth = 100f;

    [SerializeField]
    [Min(0f)]
    private float currentHealth = 100f;

    [SerializeField]
    [Range(0f, 100f)]
    private float healthPercent = 100f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private Camera mainCamera;

    private RectTransform foregroundRect;

    private float originalWidth;

    private bool originalWidthCaptured;


    // ============================================================
    // UPDATE HEALTH BAR
    // ============================================================

    public void UpdateHealthBar(float newMaxHealth, float newCurrentHealth)
    {
        maxHealth = Mathf.Max(0f, newMaxHealth);

        currentHealth = Mathf.Clamp(
            newCurrentHealth,
            0f,
            maxHealth
        );

        CalculatePercent();

        UpdateVisual();
    }


    // ============================================================
    // CALCULATE PERCENT
    // ============================================================

    private void CalculatePercent()
    {
        if (maxHealth <= 0f)
        {
            healthPercent = 0f;
            return;
        }

        healthPercent = Mathf.Clamp(
            (currentHealth / maxHealth) * 100f,
            0f,
            100f
        );
    }


    // ============================================================
    // CURRENT FROM PERCENT
    // ============================================================

    private void CalculateCurrentFromPercent()
    {
        if (maxHealth <= 0f)
        {
            currentHealth = 0f;
            return;
        }

        currentHealth = Mathf.Clamp(
            maxHealth * (healthPercent / 100f),
            0f,
            maxHealth
        );
    }


    // ============================================================
    // UPDATE VISUAL
    // ============================================================

    private void UpdateVisual()
    {
        if (foregroundImage == null)
            return;

        if (foregroundRect == null)
            foregroundRect = foregroundImage.rectTransform;

        CaptureOriginalWidth();

        if (!originalWidthCaptured)
            return;

        float percent = Mathf.Clamp01(
            healthPercent / 100f
        );

        float newWidth = originalWidth * percent;

        foregroundRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            newWidth
        );
    }


    // ============================================================
    // ORIGINAL WIDTH
    // ============================================================

    private void CaptureOriginalWidth()
    {
        if (foregroundRect == null)
            return;

        if (originalWidthCaptured)
            return;

        originalWidth = foregroundRect.rect.width;

        if (originalWidth > 0f)
        {
            originalWidthCaptured = true;
        }
    }


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        if (health == null)
        {
            Debug.LogError($"Object {this} has no health component!");
            return;
        }

        if (foregroundImage == null)
        {
            Debug.LogError($"Object {this} has no foreground Image!");
            return;
        }

        mainCamera = Camera.main;

        foregroundRect = foregroundImage.rectTransform;

        CaptureOriginalWidth();

        // Get this unit's actual maximum health.
        maxHealth = health.maxHealth;

        // Listen for the actual health of this unit.
        health.OnHealthChanged += Health_OnHealthChanged;

        // Show the values supplied by the Health component
        // when it sends its health update.
    }


    // ============================================================
    // HEALTH COMPONENT CHANGED
    // ============================================================

    private void Health_OnHealthChanged(float newHealth)
    {
        if (health == null)
            return;

        maxHealth = health.maxHealth;

        currentHealth = Mathf.Clamp(
            newHealth,
            0f,
            maxHealth
        );

        CalculatePercent();

        UpdateVisual();
    }


    // ============================================================
    // CAMERA
    // ============================================================

    private void LateUpdate()
    {
        if (uiContainer != null && mainCamera != null)
        {
            uiContainer.forward = mainCamera.transform.forward;
        }
    }


    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= Health_OnHealthChanged;
        }
    }


    // ============================================================
    // EDITOR
    // ============================================================

#if UNITY_EDITOR

    private float editorLastMaxHealth;
    private float editorLastCurrentHealth;
    private float editorLastHealthPercent;
    private bool editorValuesInitialized;

    private void OnValidate()
    {
        if (maxHealth < 0f)
            maxHealth = 0f;

        if (!editorValuesInitialized)
        {
            currentHealth = Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

            CalculatePercent();

            editorLastMaxHealth = maxHealth;
            editorLastCurrentHealth = currentHealth;
            editorLastHealthPercent = healthPercent;

            editorValuesInitialized = true;

            UpdateVisual();

            return;
        }


        // ========================================================
        // MAX HEALTH SLIDER CHANGED
        // ========================================================

        if (!Mathf.Approximately(maxHealth, editorLastMaxHealth))
        {
            maxHealth = Mathf.Max(0f, maxHealth);

            currentHealth = Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

            CalculatePercent();
        }


        // ========================================================
        // CURRENT HEALTH SLIDER CHANGED
        // ========================================================

        else if (!Mathf.Approximately(
            currentHealth,
            editorLastCurrentHealth))
        {
            currentHealth = Mathf.Clamp(
                currentHealth,
                0f,
                maxHealth
            );

            CalculatePercent();
        }


        // ========================================================
        // HEALTH PERCENT SLIDER CHANGED
        // ========================================================

        else if (!Mathf.Approximately(
            healthPercent,
            editorLastHealthPercent))
        {
            healthPercent = Mathf.Clamp(
                healthPercent,
                0f,
                100f
            );

            CalculateCurrentFromPercent();
        }


        // ========================================================
        // SAVE LAST VALUES
        // ========================================================

        editorLastMaxHealth = maxHealth;
        editorLastCurrentHealth = currentHealth;
        editorLastHealthPercent = healthPercent;


        // ========================================================
        // UPDATE BAR
        // ========================================================

        if (foregroundImage != null)
        {
            if (foregroundRect == null)
                foregroundRect = foregroundImage.rectTransform;

            CaptureOriginalWidth();

            UpdateVisual();
        }
    }

#endif
}