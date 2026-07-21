using UnityEngine;
using UnityEngine.UI;

public class AttackDuration : MonoBehaviour
{
    public PlayerStats playerStats;
    public GestureManager gestureManager;

    public Slider durationSlider;
    public GameObject sliderPanel;

    public float currentTime;
    float maxTime;

    void Update()
    {
        if (string.IsNullOrEmpty(gestureManager.currentAttack))
            return;

        currentTime -= Time.deltaTime;

        if (durationSlider != null)
            durationSlider.value = currentTime / maxTime;

        if (currentTime <= 0)
        {
            // Reset the slider but DON'T clear the attack
            if (durationSlider != null)
                durationSlider.value = 0;

            // DO NOT set gestureManager.currentAttack = AttackIds.None here!
            // The PlayerAttack script should handle that.
        }
    }

    public void StartAttackTimer(string attackId)
    {
        if (sliderPanel != null)
            sliderPanel.SetActive(true);

        Debug.Log("Starting timer for " + attackId);

        switch (attackId)
        {
            case AttackIds.Fireball:
                maxTime = playerStats.fireballDuration;
                break;

            case AttackIds.Wave:
                maxTime = playerStats.waveDuration;
                break;

            default:
                maxTime = playerStats.fireballDuration;
                break;
        }

        currentTime = maxTime;

        if (durationSlider != null)
        {
            durationSlider.maxValue = 1f;
            durationSlider.value = 1f;
        }
    }

    public void ResetDuration()
    {
        if (durationSlider != null)
            durationSlider.value = 0;
    }
}