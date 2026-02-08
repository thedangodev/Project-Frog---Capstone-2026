using UnityEngine;
using UnityEngine.UI;

public class Healthbar: MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
    [SerializeField] private Transform uiContainer; // Drag the UI child object here
    private Camera mainCamera;

    public void UpdateHealthBar(float maxHealth, float curHealth)
    {
        // Change the fill amount of the foreground to the percentage of health left
        foregroundImage.fillAmount = curHealth / maxHealth;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // Point the healthbar to the camera
        // Only rotate the UI container, not the main GameObject
        if (uiContainer != null && mainCamera != null)
        {
            uiContainer.forward = mainCamera.transform.forward;
        }
    }
}