using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Sub-Menus")]
    [SerializeField] private GameObject audioMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject controlsMenu;

    [Header("Primary Buttons")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject optionButton;
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject creditsButton;

    [Header("Audio")]
    [SerializeField] private GameObject sfxSlider;
    [SerializeField] private GameObject musicSlider;
    [SerializeField] private GameObject sfxLabel;
    [SerializeField] private GameObject musicLabel;

    private bool isOptionsExpanded;
    private bool isAudioMenuOpen;

    private void Start()
    {
        audioMenu.SetActive(false);
        videoMenu.SetActive(false);
        controlsMenu.SetActive(false);
        sfxSlider.SetActive(false);
        sfxLabel.SetActive(false);
        musicSlider.SetActive(false);
        musicLabel.SetActive(false);
    }

    // --- call primary fonction ---
    public void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnOptionsClicked()
    {
        // open/close settings menu
        isOptionsExpanded = !isOptionsExpanded;
        
        audioMenu.SetActive(isOptionsExpanded);
        videoMenu.SetActive(isOptionsExpanded);
        controlsMenu.SetActive(isOptionsExpanded);

        // Close audio sub-menu if options menu is closed
        if (!isOptionsExpanded)
        {
            isAudioMenuOpen = false;

            sfxSlider.SetActive(false);
            sfxLabel.SetActive(false);
            musicSlider.SetActive(false);
            musicLabel.SetActive(false);
        }
    }

    public void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the editor
#endif
    }

    public void OnCreditsClicked()
    {
        SceneManager.LoadScene("Credits");
    }

    //Sub-menu functions

    public void OnVideoClicked()
    {
        Debug.Log("Menu Video open");
        // show video options
    }

    public void OnAudioClicked()
    {
        // open/close audio options
        isAudioMenuOpen = !isAudioMenuOpen;

        sfxSlider.SetActive(isAudioMenuOpen);
        sfxLabel.SetActive(isAudioMenuOpen);
        musicSlider.SetActive(isAudioMenuOpen);
        musicLabel.SetActive(isAudioMenuOpen);
    }

    public void OnControlsClicked()
    {
        Debug.Log("Menu Control open");
        // show controls options
    }
}