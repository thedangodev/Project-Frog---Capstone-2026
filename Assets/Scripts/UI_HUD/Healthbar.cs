using UnityEngine;
using UnityEngine.UI;

public class Healthbar: MonoBehaviour
{
    [SerializeField] private Image foregroundImage;
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
        transform.forward = mainCamera.transform.forward;
    }
}