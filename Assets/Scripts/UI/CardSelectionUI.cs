using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using FMODUnity;

public class CardSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveRoundSystem waveSpawner;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardUIPrefab;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private UIPlayerHUD playerHUD;
    [SerializeField] private PlayerCrosshair playerCrosshair;
    [SerializeField] private Canvas crosshairCanvas;
    [SerializeField] private CanvasGroup cardContainerGroup;

    [Header("FMod Events")]
    [SerializeField] private EventReference cardSelectionEvent;

    private CanvasGroup canvasGroup;
    public bool IsCardSelectionActive { get; private set; }
    private bool cardAlreadyChosen = false;

    private List<CardUI> spawnedCards = new List<CardUI>();

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        upgradeManager = UpgradeManager.Instance;
        playerCrosshair = FindFirstObjectByType<PlayerCrosshair>();
        StartCoroutine(WaitForCrosshairCanvas());
        cardContainerGroup = cardContainer.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Card selection is hidden when the game starts
        HideUI();
    }

    public void ShowCardSelectionFromWave()
    {
        ShowCardSelection();
    }

    private void ShowUI()
    {
        // Make the card selection screen fully visible and responsive to clicks
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeCardContainer(1f, 0.35f));
    }

    private void HideUI()
    {
        // Make the card selection screen invisible and ignore all clicks
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeCardContainer(float target, float duration)
    {
        float start = cardContainerGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cardContainerGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        cardContainerGroup.alpha = target;
    }

    private void ShowCardSelection()
    {
        cardAlreadyChosen = false;
        IsCardSelectionActive = true;
        Time.timeScale = 0f;
        ShowUI();
        playerHUD.HideHUD();
        Cursor.visible = true;

        // Hide the player's crosshair while selecting a card
        if (playerCrosshair != null)
            playerCrosshair.gameObject.SetActive(false);
        if (crosshairCanvas != null)
            crosshairCanvas.gameObject.SetActive(false);

        var manager = UpgradeManager.Instance;
       
        // Ask the upgrade manager to pick 3 random cards for the player to choose from
        List<UpgradeDataSO> selectedCards = upgradeManager.GetRandomCards(3);
        StartCoroutine(SpawnCardsSequentially(selectedCards));

        RuntimeManager.PlayOneShot(cardSelectionEvent, transform.position);
    }

    private IEnumerator WaitForCrosshairCanvas()
    {
        while (crosshairCanvas == null)
        {
            crosshairCanvas = GameObject.Find("CrosshairCanvas")?.GetComponent<Canvas>();
            yield return null;
        }
    }

    private IEnumerator SpawnCardsSequentially(List<UpgradeDataSO> cards)
    {
        spawnedCards.Clear();

        // Spawn each card one at a time with a short delay between them for a staggered animation effect
        // WaitForSecondsRealtime is used here because normal timers stop while the game is frozen
        foreach (UpgradeDataSO card in cards)
        {
            CardUI ui = Instantiate(cardUIPrefab, cardContainer);
            ui.Setup(card, OnCardChosen);
            ui.PlaySpawnAnimation();
            spawnedCards.Add(ui);
            yield return new WaitForSecondsRealtime(0.35f);
        }

        // After all cards are spawned, set the first card as the selected button for controller/keyboard navigation
        yield return null; // wait one frame for the EventSystem to recognize the buttons
        var firstButton = cardContainer.GetComponentInChildren<UnityEngine.UI.Selectable>();
        if (firstButton != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    private void OnCardChosen(UpgradeDataSO chosenCard)
    {
        if (cardAlreadyChosen)
            return;

        cardAlreadyChosen = true;

        IsCardSelectionActive = false;

        upgradeManager.OnCardChosen(chosenCard);

        StartCoroutine(HandleCardDisappear(chosenCard));
    }

    private IEnumerator HandleCardDisappear(UpgradeDataSO chosenCard)
    {
        CardUI chosenUI = spawnedCards.Find(ui => ui.Data == chosenCard);

        foreach (var ui in spawnedCards)
        {
            if (ui != chosenUI)
                StartCoroutine(ui.PlaySlideDownAnimation());
        }

        yield return StartCoroutine(chosenUI.PlayDissolveAnimation());
        yield return StartCoroutine(FadeCardContainer(0f, 0.5f));

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        HideUI();
        Time.timeScale = 1f;

        playerHUD.ShowHUD();
        Cursor.visible = false;

        if (playerCrosshair != null)
            playerCrosshair.gameObject.SetActive(true);
        if (crosshairCanvas != null)
            crosshairCanvas.gameObject.SetActive(true);

        waveSpawner.StartNextWaveAfterCard();
    }
}