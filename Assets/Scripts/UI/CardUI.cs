using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundFrame;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button button;

    [Header("Frame Sprites")]
    [SerializeField] private Sprite fireCard;
    [SerializeField] private Sprite iceCard;
    [SerializeField] private Sprite windCard;

    [Header("Spawn Animation")]
    [SerializeField] private float spinDuration = 1.5f;

    private UpgradeDataSO upgradeData;
    private System.Action<UpgradeDataSO> onSelected;

    /// <summary>
    /// Fills the card with all its visual information and registers what happens when the player clicks it.
    /// </summary>
    public void Setup(UpgradeDataSO data, System.Action<UpgradeDataSO> callback)
    {
        upgradeData = data;
        onSelected = callback;

        // Set the card's icon and name
        icon.sprite = data.Icon;
        title.text = data.CardName;

        // Find out how many times the player has already picked this card
        int level = UpgradeManager.Instance.GetLevel(data);

        // The card will show what the NEXT pick would do, not the current state
        int nextLevel = Mathf.Clamp(level + 1, 0, upgradeData.MaxLevel - 1);

        // Show the player what changes if they pick this card
        description.text = upgradeData.GetTransitionDescription(nextLevel);

        // Show the level this card would reach, or MAX if it's already fully upgraded
        levelText.text = (nextLevel >= upgradeData.MaxLevel - 1) ? "MAX" : $"Lvl {nextLevel}";

        // Give the card a different frame depending on which element it belongs to
        switch (data.Element)
        {
            case AnchorElement.Fire: backgroundFrame.sprite = fireCard; break;
            case AnchorElement.Ice: backgroundFrame.sprite = iceCard; break;
            case AnchorElement.Wind: backgroundFrame.sprite = windCard; break;
        }

        // Clear any old click listeners before adding a fresh one to avoid triggering the callback multiple times
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(upgradeData));
    }

    /// <summary>
    /// Plays a flip-in animation where the card spins from edge-on to fully facing the player.
    /// Call this immediately after Setup when the card appears on screen.
    /// </summary>
    public void PlaySpawnAnimation()
    {
        StartCoroutine(SpinIn());
    }

    private IEnumerator SpinIn()
    {
        // The card starts rotated 90 degrees (invisible edge-on) and spins to face the player
        // unscaledDeltaTime is used here because the game is frozen during card selection — normal timers would never advance
        float elapsed = 0f;
        transform.rotation = Quaternion.Euler(0, 90f, 0);

        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / spinDuration;
            transform.rotation = Quaternion.Euler(0, Mathf.Lerp(90f, 0f, t), 0);
            yield return null;
        }

        // Snap to exactly flat to avoid any floating point drift at the end of the animation
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}