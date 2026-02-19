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
    [SerializeField] private int spinTurns = 2;

    private CardData cardData;
    private System.Action<CardData> onSelected;

    public void Setup(CardData data, System.Action<CardData> callback)
    {
        cardData = data;
        onSelected = callback;

        // Set visuals
        icon.sprite = data.Icon;
        title.text = data.CardName;

        int nextLevel = Mathf.Clamp(data.CurrentLevel + 1, 0, data.MaxLevel - 1);

        // Description with next level values
        description.text = data.GetDescriptionForLevel(nextLevel);

        // Level text
        if (nextLevel >= data.MaxLevel - 1)
            levelText.text = "MAX";
        else
            levelText.text = "Lvl " + (nextLevel);

        // Set frame based on element
        switch (data.Element)
        {
            case AnchorElement.Fire:
                backgroundFrame.sprite = fireCard;
                break;
            case AnchorElement.Ice:
                backgroundFrame.sprite = iceCard;
                break;
            case AnchorElement.Wind:
                backgroundFrame.sprite = windCard;
                break;
        }

        // Set button callback
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected(cardData));
    }

    public void PlaySpawnAnimation()
    {
        StartCoroutine(SpinIn());
    }

    private IEnumerator SpinIn()
    {
        float elapsed = 0f;

        // Start rotated sideways (invisible)
        Quaternion startRot = Quaternion.Euler(0, 90f, 0);
        Quaternion endRot = Quaternion.Euler(0, 0, 0);

        transform.rotation = startRot;

        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / spinDuration;

            float angle = Mathf.Lerp(90f, 0f, t);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            yield return null;
        }

        transform.rotation = endRot;
    }
}