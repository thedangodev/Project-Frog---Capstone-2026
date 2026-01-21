using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Sub-Menus Options")]
    [SerializeField] private Button audioButton;
    [SerializeField] private Button videoButton;
    [SerializeField] private Button controlsButton;

    [Header("Primary Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button creditsButton;

    [Header("Sub-Menus Audio")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicLabel;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterLabel;

    [Header("Sub-Menus Video")]


    [Header("Sub-Menus Controls")]
    [SerializeField] private GameObject keyboardImage;
    [SerializeField] private GameObject controllerImage;


    private bool isOptionsExpanded;
    private bool isAudioButtonOpen;
    private bool isVideoButtonOpen;
    private bool isControlsButtonOpen;

    private void Start()
    {
        audioButton.gameObject.SetActive(false);
        videoButton.gameObject.SetActive(false);
        controlsButton.gameObject.SetActive(false);

        sfxSlider.gameObject.SetActive(false);
        sfxLabel.gameObject.SetActive(false);

        musicSlider.gameObject.SetActive(false);
        musicLabel.gameObject.SetActive(false);

        masterSlider.gameObject.SetActive(false);
        masterLabel.gameObject.SetActive(false);

        keyboardImage.SetActive(false);
        controllerImage.SetActive(false);
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
        
        audioButton.gameObject.SetActive(isOptionsExpanded);
        videoButton.gameObject.SetActive(isOptionsExpanded);
        controlsButton.gameObject.SetActive(isOptionsExpanded);

        // Close audio sub-menu if options menu is closed
        if (!isOptionsExpanded)
        {
            isAudioButtonOpen = false;

            sfxSlider.gameObject.SetActive(false);
            sfxLabel.gameObject.SetActive(false);

            musicSlider.gameObject.SetActive(false);
            musicLabel.gameObject.SetActive(false);

            masterSlider.gameObject.SetActive(false);
            masterLabel.gameObject.SetActive(false);
        }

        // Close controls sub-menu if options menu is closed
        if (!isOptionsExpanded)
        {
            isControlsButtonOpen = false;
            keyboardImage.SetActive(false);
            controllerImage.SetActive(false);
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
        if (!isVideoButtonOpen)
            CloseAllSubMenus();

        isVideoButtonOpen = !isVideoButtonOpen;

        Debug.Log("Menu Video open");
        //Disable the Video button if audio menu is open
        videoButton.GetComponent<UnityEngine.UI.Button>().interactable = !isVideoButtonOpen;
    }

    public void OnAudioClicked()
    {
        if (!isAudioButtonOpen)
            CloseAllSubMenus();

        // open/close audio options
        isAudioButtonOpen = !isAudioButtonOpen;

        masterSlider.gameObject.SetActive(isAudioButtonOpen);
        masterLabel.gameObject.SetActive(isAudioButtonOpen);

        sfxSlider.gameObject.SetActive(isAudioButtonOpen);
        sfxLabel.gameObject.SetActive(isAudioButtonOpen);

        musicSlider.gameObject.SetActive(isAudioButtonOpen);
        musicLabel.gameObject.SetActive(isAudioButtonOpen);

        //Disable the Audio button if audio menu is open
        audioButton.GetComponent<UnityEngine.UI.Button>().interactable = !isAudioButtonOpen;
    }

    public void OnControlsClicked()
    {
        if (!isControlsButtonOpen)
            CloseAllSubMenus();

        isControlsButtonOpen = !isControlsButtonOpen;

        keyboardImage.SetActive(isControlsButtonOpen);
        controllerImage.SetActive(isControlsButtonOpen);

        //Disable the Controls button if audio menu is open
        controlsButton.GetComponent<UnityEngine.UI.Button>().interactable = !isControlsButtonOpen;
    }

    private void CloseAllSubMenus()
    {
        // Audio
        isAudioButtonOpen = false;
        masterSlider.gameObject.SetActive(false);
        masterLabel.gameObject.SetActive(false);
        sfxSlider.gameObject.SetActive(false);
        sfxLabel.gameObject.SetActive(false);
        musicSlider.gameObject.SetActive(false);
        musicLabel.gameObject.SetActive(false);

        audioButton.interactable = true;
        videoButton.interactable = true;
        controlsButton.interactable = true;

        // Controls
        isControlsButtonOpen = false;
        keyboardImage.SetActive(false);
        controllerImage.SetActive(false);

        // Video
        isVideoButtonOpen = false;
    }

}