using UnityEngine;

public class GestureMultiplierManager : MonoBehaviour
{
    [System.Serializable]
    public class AccuracyTier
    {
        [Header("Accuracy requirement")]
        [Range(0f, 1f)]
        public float minimumAccuracy;

        [Header("Damage")]
        [Min(1f)]
        public float damageMultiplier = 1f;

        [Header("Text")]
        public string displayText = "Normal";
    }

    public GestureRankCursorUI cursorUI;

    [Header("Accuracy tiers")]
    public AccuracyTier[] accuracyTiers;

    [Header("Current attack result")]
    public float currentDamageMultiplier = 1f;
    public string currentRank = "Normal";

    public void CalculateMultiplier(float accuracy)
    {
        // Default result if no tier matches.
        currentDamageMultiplier = 1f;
        currentRank = "Normal";

        // The highest matching tier wins.
        foreach (AccuracyTier tier in accuracyTiers)
        {
            if (accuracy >= tier.minimumAccuracy)
            {
                currentDamageMultiplier = tier.damageMultiplier;
                currentRank = tier.displayText;
            }
        }

        if (cursorUI != null)
        {
            cursorUI.ShowRank(currentRank);
        }

        Debug.Log(
            $"Accuracy: {accuracy:P0} | " +
            $"Rank: {currentRank} | " +
            $"Damage multiplier: x{currentDamageMultiplier}"
        );
    }

    public float GetDamageMultiplier()
    {
        return currentDamageMultiplier;
    }
}