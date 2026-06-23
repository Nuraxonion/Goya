using UnityEngine;
using UnityEngine.UI;

public class AttackDuration : MonoBehaviour
{
    public PlayerStats playerStats;
    public GestureManager gestureManager;

    public Slider durationSlider;
    public GameObject sliderPanel;

    float currentTime;
    float maxTime;

    void Update()
    {
        if (gestureManager.currentAttack ==
            GestureManager.AttackType.NoAttack)
            //sliderPanel.SetActive(false);
            return;

        currentTime -= Time.deltaTime;

        durationSlider.value = currentTime / maxTime;

        if (currentTime <= 0)
        {
            ResetAttack();
            sliderPanel.SetActive(false);
        }
    }

    public void StartAttackTimer(
        GestureManager.AttackType attackType)
    {
        sliderPanel.SetActive(true);
        Debug.Log("Starting timer for " + attackType);
        switch (attackType)
        {
            case GestureManager.AttackType.Bracket:
                maxTime = playerStats.fireballDuration;
                break;

            case GestureManager.AttackType.Circle:
                maxTime = playerStats.waveDuration;
                break;
        }

        currentTime = maxTime;

        durationSlider.maxValue = 1f;
        durationSlider.value = 1f;
    }

    void ResetAttack()
    {
        gestureManager.currentAttack =
            GestureManager.AttackType.NoAttack;

        durationSlider.value = 0;
    }
}