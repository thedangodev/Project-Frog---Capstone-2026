using UnityEngine;

public enum AnchorElement
{
    Fire,
    Ice,
    Wind
}

public enum CardRarity
{
    Common,
    Rare
}

[CreateAssetMenu(menuName = "ProjectFrog/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string cardName;
    [SerializeField,TextArea] private string description;

    [Header("Classification")]
    [SerializeField] private AnchorElement element;
    [SerializeField] private CardRarity rarity;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;

    [Header("Leveling")]
    // Example: [1,2,3] = 3 levels
    [SerializeField] private float[] levelValues;

    // Runtime tracking (not saved in asset)
    [HideInInspector][SerializeField] private int currentLevel = 0;

    public string CardName => cardName;
    public string Description => description;
    public AnchorElement Element => element;
    public CardRarity Rarity => rarity;
    public Sprite Icon => icon;

    public int CurrentLevel
        {
        get => currentLevel;
        set => currentLevel = Mathf.Clamp(value, 0, MaxLevel-1);
        }
    public int MaxLevel => levelValues != null ? levelValues.Length : 1;

    public bool IsMaxed => currentLevel >= MaxLevel-1;

    public float CurrentValue
    {
        get
        {
            int index = Mathf.Clamp(currentLevel, 0, MaxLevel - 1);
            return levelValues != null ? levelValues[index] : 0f;
        }
    }
}