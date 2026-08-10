using UnityEngine;

// Drives the level music off the player's health, in the same three bands the bat
// uses for its animation (see BatMoveScript.UpdateHealthAnimation).
public class HealthMusic : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Tooltip("Health above 2/3 of max.")]
    public AudioClip highHealthMusic;

    [Tooltip("Health between 1/3 and 2/3 of max.")]
    public AudioClip midHealthMusic;

    [Tooltip("Health at or below 1/3 of max.")]
    public AudioClip lowHealthMusic;

    private int currentBand = -1;   // 1, 2, or 3; -1 forces first update

    void Update()
    {
        if (playerHealth == null) return;
        if (playerHealth.maxHealth <= 0f) return;

        float ratio = playerHealth.currentHealth / playerHealth.maxHealth;

        // > 2/3        -> band 1 -> highHealthMusic
        // 1/3 .. 2/3   -> band 2 -> midHealthMusic
        // <= 1/3       -> band 3 -> lowHealthMusic
        int band;
        if (ratio > 2f / 3f)      band = 1;
        else if (ratio > 1f / 3f) band = 2;
        else                      band = 3;

        // Health only ever decreases, so the band only ever advances. This also keeps
        // health sitting on a threshold from flipping the music back and forth.
        if (band <= currentBand) return;

        currentBand = band;

        AudioClip clip = band == 1 ? highHealthMusic
                       : band == 2 ? midHealthMusic
                                   : lowHealthMusic;

        if (clip != null && MusicManager.Instance != null)
            MusicManager.Instance.PlayMusic(clip);
    }
}
