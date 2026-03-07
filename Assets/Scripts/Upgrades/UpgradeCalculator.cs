/// <summary>
/// Stateless utility for computing card stat values based on runtime levels.
/// </summary>
public static class UpgradeCalculator
{
    /// <summary>
    /// Returns the total accumulated value of a card at its current level.
    /// </summary>
    public static float GetTotalValue(UpgradeDataSO card)
    {
        int level = UpgradeManager.Instance.GetLevel(card);
        return card.GetTotalValueUpToLevel(level);
    }

    /// <summary>
    /// Returns a multiplier in the form 1 + (totalValue / 100).
    /// Use for stats that scale proportionally, e.g. damage bonuses.
    /// </summary>
    public static float GetMultiplier(UpgradeDataSO card)
    {
        return 1f + GetTotalValue(card) / 100f;
    }

    /// <summary>
    /// Returns true if the card has been picked at least once.
    /// Use for one-time unlocks or boolean ability checks.
    /// </summary>
    public static bool IsActive(UpgradeDataSO card)
    {
        return UpgradeManager.Instance.GetLevel(card) > 0;
    }
}