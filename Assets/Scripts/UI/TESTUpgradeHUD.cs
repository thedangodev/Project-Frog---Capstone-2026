using System.Collections.Generic;
using UnityEngine;

public class TESTUpgradeHUD : MonoBehaviour
{
    [Header("References")]
    private List<UpgradeDataSO> allCards;
    private GUIStyle style;

    private void Start()
    {
        allCards = UpgradeManager.Instance.GetAllCards();

        if (allCards == null || allCards.Count == 0)
            Debug.LogWarning("UpgradeDemoHUD has no UpgradeDataSO assigned!");

        style = new GUIStyle
        {
            fontSize = 18,
            normal = { textColor = Color.white }
        };
    }

    private void OnGUI()
    {
        var allCards = UpgradeManager.Instance.GetAllCards(); // fetch fresh every frame

        float y = 20;

        foreach (var card in allCards)
        {
            int level = UpgradeManager.Instance.GetLevel(card);
            float total = UpgradeCalculator.GetTotalValue(card);
            float multiplier = UpgradeCalculator.GetMultiplier(card);

            string line;

            if (card.CardName.ToLower().Contains("multishot"))
                line = $"{card.CardName} Lv {level} (+{total:0} darts)";
            else
                line = $"{card.CardName} Lv {level} (+{total:0.##}% | x{multiplier:0.###})";

            GUI.Label(new Rect(20, y, 900, 30), line, style);
            y += 22;
        }
    }
}