using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveRoundSystem waveSpawner;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CardUI cardUIPrefab;
    [SerializeField] private CardPoolManager cardPool;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        HideUI();
        waveSpawner.onWaveCompleted += ShowCardSelection;
    }

    private void OnDestroy()
    {
        waveSpawner.onWaveCompleted -= ShowCardSelection;
    }

    private void Update()
    {
        // For testing: Press T to show card selection
        if (Input.GetKeyDown(KeyCode.T))
        ShowCardSelection();
    }

    private void ShowUI()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideUI()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowCardSelection()
    {
        // Pause game
        Time.timeScale = 0f;

        // enable cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Activate UI
        ShowUI();

        // Ask the pool for 3 valid cards
        List<CardData> selectedCards = cardPool.GetRandomCards(3);
        StartCoroutine(SpawnCardsSequentially(selectedCards));
    }

    private IEnumerator SpawnCardsSequentially(List<CardData> cards)
    {
        foreach (CardData card in cards)
        {
            CardUI ui = Instantiate(cardUIPrefab, cardContainer);
            ui.Setup(card, OnCardChosen);

            ui.PlaySpawnAnimation();

            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    private void OnCardChosen(CardData chosenCard)
    {
        // Tell the pool that this card was selected
        cardPool.OnCardChosen(chosenCard);

        // Destroy all card UI objects
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        // Hide UI
        HideUI();

        // Resume game
        Time.timeScale = 1f;

        //disable cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start next wave
        waveSpawner.StartNextWave();
    }
}