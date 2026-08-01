using UnityEngine;
using UnityEngine.UI;

public class InkXPUI : MonoBehaviour
{
    [Header("References")]
    public PlayerXP playerXP;

    public Image redFillBar;
    public Image redFillBottle;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float barPortion = 0.8f;

    public float fillSpeed = 4f;   // Higher = faster ink

    private float displayedProgress = 0f;

    void Update()
    {
        if (playerXP == null)
            return;

        float targetProgress = playerXP.xpLevel / playerXP.requiredXP;
        targetProgress = Mathf.Clamp01(targetProgress);

        // Smoothly move toward the target XP
        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            targetProgress,
            fillSpeed * Time.deltaTime);

        // Fill the bar first
        float barFill = Mathf.Clamp01(displayedProgress / barPortion);
        redFillBar.fillAmount = barFill;

        // Fill the bottle second
        float bottleFill = Mathf.Clamp01(
            (displayedProgress - barPortion) /
            (1f - barPortion));

        redFillBottle.fillAmount = bottleFill;
    }
}