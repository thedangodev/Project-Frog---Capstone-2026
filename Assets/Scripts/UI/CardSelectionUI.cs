using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveRoundSystem waveSpawner;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardUIPrefab;
    [SerializeField] private UpgradeManager upgradeManager;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // Grab the component that controls whether this UI is visible and clickable
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) { canvasGroup = gameObject.AddComponent<CanvasGroup>(); }
    }

    private void Start()
    {
        // Card selection is hidden when the game starts
        HideUI();
    }

    private void Update()
    {
        // TODO: remove before ship — pressing T is a shortcut to test card selection without playing through a wave
        if (Input.GetKeyDown(KeyCode.T)) { ShowCardSelection(); }
    }

    private void ShowUI()
    {
        // Make the card selection screen fully visible and responsive to clicks
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideUI()
    {
        // Make the card selection screen invisible and ignore all clicks
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowCardSelection()
    {
        // Freeze the game while the player is choosing a card
        Time.timeScale = 0f;

        // Show and unlock the mouse cursor so the player can click a card
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowUI();

        // Ask the upgrade manager to pick 3 random cards for the player to choose from
        List<UpgradeDataSO> selectedCards = upgradeManager.GetRandomCards(3);
        StartCoroutine(SpawnCardsSequentially(selectedCards));
    }

    private IEnumerator SpawnCardsSequentially(List<UpgradeDataSO> cards)
    {
        // Spawn each card one at a time with a short delay between them for a staggered animation effect
        // WaitForSecondsRealtime is used here because normal timers stop while the game is frozen
        foreach (UpgradeDataSO card in cards)
        {
            CardUI ui = Instantiate(cardUIPrefab, cardContainer);
            ui.Setup(card, OnCardChosen);
            ui.PlaySpawnAnimation();
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    private void OnCardChosen(UpgradeDataSO chosenCard)
    {
        // Tell the upgrade manager which card the player picked so it can apply the upgrade
        upgradeManager.OnCardChosen(chosenCard);

        // Remove all card UI objects from the screen
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // Hide the selection screen, unfreeze the game, and lock the cursor again
        HideUI();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}