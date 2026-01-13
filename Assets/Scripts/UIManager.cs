using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Sub-menus")]
    public GameObject settingsSubMenu;  // Contain Audio, Video, Language, Controls

    [Header("Primary Buttons")]
    public GameObject startButton;
    public GameObject optionButton;
    public GameObject exitButton;
    public GameObject creditsButton;

    private bool isStartExpanded = false;
    private bool isSettingsExpanded = false;

    void Start()
    {
        settingsSubMenu.SetActive(false);
    }

    // --- call primary fonction ---
    public void OnStartClicked()
    {
        SceneManager.LoadScene("gameScene");
    }

    public void OnOptionsClicked()
    {
       
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

    }

    // --- call sub menu ---

    public void OnAudioClicked()
    {
        Debug.Log("Menu Audio open");
        // show audio options
    }

    public void OnVideoClicked()
    {
        Debug.Log("Menu Video open");
        // show video options
    }

    public void OnLanguageClicked()
    {
        Debug.Log("Menu Language open");
        // show language options
    }

    public void OnControlsClicked()
    {
        Debug.Log("Menu Control open");
        // show controls options
    }
}