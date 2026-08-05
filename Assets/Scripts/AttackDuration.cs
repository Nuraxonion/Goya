using UnityEngine;
using UnityEngine.UI;

public class AttackDuration : MonoBehaviour
{
    public PlayerStats playerStats;
    public GestureManager gestureManager;

    public Slider durationSlider;
    public GameObject sliderPanel;

    public float currentTime;
    private float maxTime;
    private bool isTimerRunning = false;

    void Start()
    {
        Debug.Log("✅ AttackDuration STARTED!");
    }

    void Update()
    {
        if (!isTimerRunning)
            return;

        if (string.IsNullOrEmpty(gestureManager.currentAttack))
        {
            ResetAttack();
            return;
        }

        currentTime -= Time.deltaTime;

        if (durationSlider != null && maxTime > 0f)
        {
            durationSlider.value = Mathf.Clamp01(currentTime / maxTime);
        }

        if (currentTime <= 0f)
        {
            Debug.Log("⏱️ Duration ended - attack cleared!");
            ResetAttack();
        }
    }

    public void StartAttackTimer(string attackId)
    {
        Debug.Log($"🎯 StartAttackTimer called for: {attackId}");

        if (sliderPanel != null)
        {
            sliderPanel.SetActive(true);
        }

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
        isTimerRunning = true;

        if (durationSlider != null)
        {
            durationSlider.maxValue = 1f;
            durationSlider.value = 1f;
        }
    }

    void ResetAttack()
    {
        isTimerRunning = false;
        currentTime = 0f;

        if (durationSlider != null)
        {
            durationSlider.value = 0f;
        }

        // Mirrors the SetActive(true) in StartAttackTimer. Without this the bar
        // stayed on screen permanently after the first cast.
        if (sliderPanel != null)
        {
            sliderPanel.SetActive(false);
        }

        // Clear the attack so it can be cast again
        if (gestureManager != null)
        {
            gestureManager.currentAttack = AttackIds.None;
        }
    }
}