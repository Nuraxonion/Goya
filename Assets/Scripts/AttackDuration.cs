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
        if (string.IsNullOrEmpty(gestureManager.currentAttack))
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

    public void StartAttackTimer(string attackId)
    {
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

        durationSlider.maxValue = 1f;
        durationSlider.value = 1f;
    }

    void ResetAttack()
    {
        gestureManager.currentAttack = AttackIds.None;

        durationSlider.value = 0;
    }
}