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

// Every stat that a card can upgrade — None is used for cards that unlock an ability instead of boosting a number
public enum UpgradeStat
{
    None,
    FireDamage,
    ExplosionDamage,
    DartDamage,
    PostPierceDamage,
    DamageTakenIfSlowed,
    PrimarySpeed,
    PrimaryDamage,
    AttackSpeed,
    ExtraVolleyDarts,
    PointBlankDamage
}

// A card asset created in the Unity editor that holds all the static design data for one upgrade card
[CreateAssetMenu(menuName = "ProjectFrog/Card Data")]
public class UpgradeDataSO : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string cardName;
    [TextArea]
    // Optional — if left empty the card will generate a default description automatically
    [SerializeField] private string descriptionTemplate;

    [Header("Classification")]
    [SerializeField] private AnchorElement element;
    [SerializeField] private CardRarity rarity;

    [Header("Upgrade Target")]
    [SerializeField] private UpgradeStat stat;

    [Header("Upgrade Values")]
    // Each entry is how much this card adds at that pick — e.g. [10, 9, 8] means +10 on first pick, +19 total on second, +27 on third
    [SerializeField] private float[] levelValues;
    // The unit shown after numbers in the description — use % for damage bonuses, leave blank for whole number stats like dart count
    [SerializeField] private string unit = "%";

    [Header("Visual")]
    [SerializeField] private Sprite icon;

    public string CardName => cardName;
    public AnchorElement Element => element;
    public UpgradeStat Stat => stat;
    public Sprite Icon => icon;
    public CardRarity Rarity => rarity;

    // How many times this card can be picked before it is fully upgraded
    public int MaxLevel => levelValues != null ? levelValues.Length : 0;

    /// <summary>
    /// Returns the total accumulated bonus this card provides after being picked the given number of times.
    /// </summary>
    public float GetTotalValueUpToLevel(int level)
    {
        if (levelValues == null || levelValues.Length == 0)
            return 0f;
        level = Mathf.Clamp(level, 0, levelValues.Length - 1);
        float total = 0f;
        for (int i = 0; i <= level; i++)
            total += levelValues[i];
        return total;
    }

    /// <summary>
    /// Returns the bonus added by one specific pick, not the running total.
    /// </summary>
    public float GetIncrementAtLevel(int level)
    {
        if (levelValues == null || levelValues.Length == 0)
            return 0f;
        level = Mathf.Clamp(level, 0, levelValues.Length - 1);
        return levelValues[level];
    }

    /// <summary>
    /// Returns the total bonus before and after a specific pick — used to show the player what they are gaining.
    /// </summary>
    public (float oldValue, float newValue) GetLevelTransition(int nextLevel)
    {
        int oldLevel = Mathf.Clamp(nextLevel - 1, 0, MaxLevel - 1);
        int newLevel = Mathf.Clamp(nextLevel, 0, MaxLevel - 1);
        float oldTotal = GetTotalValueUpToLevel(oldLevel);
        float newTotal = GetTotalValueUpToLevel(newLevel);
        return (oldTotal, newTotal);
    }

    /// <summary>
    /// Builds the description string shown on the card in the selection UI.
    /// Uses the custom description template if one is set, otherwise generates a default "before → after" format.
    /// </summary>
    public string GetTransitionDescription(int nextLevel)
    {
        // If the card is already fully upgraded, just show the final value
        if (nextLevel >= MaxLevel)
        {
            float finalValue = GetTotalValueUpToLevel(MaxLevel - 1);
            return $"{cardName} {finalValue}{unit} (MAX)";
        }

        var (oldValue, newValue) = GetLevelTransition(nextLevel);

        // If a designer wrote a custom description for this card, fill in the before and after values
        if (!string.IsNullOrEmpty(descriptionTemplate))
        {
            return descriptionTemplate
                .Replace("{old}", oldValue.ToString())
                .Replace("{new}", newValue.ToString());
        }

        // Default format used when no custom description is provided
        return $"{cardName} {oldValue}{unit} → {newValue}{unit}";
    }

    /// <summary>
    /// Returns the bonus granted by the very first pick of this card — used to preview base strength before any levels are applied.
    /// </summary>
    public float GetBaseValue()
    {
        if (levelValues == null || levelValues.Length == 0)
            return 0f;
        return levelValues[0];
    }
}