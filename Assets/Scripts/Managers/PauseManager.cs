using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using FMODUnity;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("FMod Events")]
    [SerializeField] private EventReference openPauseEvent;
    [SerializeField] private EventReference closePauseEvent;
    //[SerializeField] private EventReference buttonClickEvent;
    //[SerializeField] private EventReference buttonHoverEvent;

    [Header("UI References")]
    [SerializeField] private GameObject pauseOverlayPanel;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject deathOverlay;
    [SerializeField] private CardSelectionUI cardSelectionUI;

    private PlayerInput playerInput;
    private InputAction pauseAction;

    private string currentActionMapName;
    private bool isPaused = false;
    public bool ispaused => isPaused;

    // --- Singleton / global state + event ---
    private static PauseManager s_instance;
    public static event Action<bool> OnPauseStateChanged;
    public static bool GlobalIsPaused => s_instance != null ? s_instance.ispaused : Mathf.Approximately(Time.timeScale, 0f);

    private void Awake()
    {
        // set singleton instance (last writer wins)
        s_instance = this;

        playerInput = FindAnyObjectByType<PlayerInput>();
        RebindActionsFromCurrentMap();

        if (pauseOverlayPanel != null)
            pauseOverlayPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= OnControlsChanged;
    }

    private void Update()
    {
        // Rebind if the action map has changed 
        if (playerInput != null &&
            playerInput.currentActionMap != null &&
            playerInput.currentActionMap.name != currentActionMapName)
        {
            RebindActionsFromCurrentMap();
        }

        if (pauseAction == null)
            return;

        // Look for the pause input
        if (pauseAction.WasPressedThisFrame())
        {
            TogglePause();
        }
    }

    private void OnControlsChanged(PlayerInput pi)
    {
        RebindActionsFromCurrentMap();
    }

    private void RebindActionsFromCurrentMap()
    {
        if (playerInput == null || playerInput.currentActionMap == null)
            return;

        currentActionMapName = playerInput.currentActionMap.name;
        pauseAction = playerInput.currentActionMap.FindAction("Pause");
    }

    private void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void ResumeGame()
    {
        Debug.Log("ResumeGame triggered");
        if (pauseOverlayPanel != null)
        {
            pauseOverlayPanel.SetActive(false);
            Debug.Log($"Panel active: {pauseOverlayPanel.activeSelf}");
        }

        RuntimeManager.PlayOneShot(closePauseEvent, transform.position);

        if (pauseOverlayPanel != null)
            pauseOverlayPanel.SetActive(false);

        if (playerHUD != null)
            playerHUD.SetActive(true);

        Time.timeScale = 1f;
        isPaused = false;

        // notify listeners
        OnPauseStateChanged?.Invoke(isPaused);

        StartCoroutine(ReenableGameplayInput());
    }

    private IEnumerator ReenableGameplayInput()
    {
        // Yeld for a frame to ensure the pause menu has fully deactivated and any input state has reset
        yield return null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PauseGame()
    {
        // Don't allow pausing if the death overlay is active
        if (deathOverlay != null && deathOverlay.activeInHierarchy)
        {
            return;
        }

        // Don't allow pausing if the card selection UI is active
        if (cardSelectionUI != null && cardSelectionUI.IsCardSelectionActive)
        {
            return;
        }

        RuntimeManager.PlayOneShot(openPauseEvent, transform.position);

        if (pauseOverlayPanel != null)
            pauseOverlayPanel.SetActive(true);

        if (playerHUD != null)
            playerHUD.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;

        // notify listeners
        OnPauseStateChanged?.Invoke(isPaused);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Set the first button in the pause menu as selected for controller navigation
        var firstButton = pauseOverlayPanel.GetComponentInChildren<Selectable>();
        if (firstButton != null)
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);

        // Refresh the card icons in the pause menu
        var iconManager = pauseOverlayPanel.GetComponentInChildren<CardIconManager>(true);
        if (iconManager != null)
            iconManager.RefreshIcons();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}