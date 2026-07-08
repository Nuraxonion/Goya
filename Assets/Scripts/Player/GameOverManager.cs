using UnityEngine;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    // Shows coins earned this run + total banked coins.
    public TextMeshProUGUI coinsText;

    private bool isGameOver = false;

    public void ShowGameOver()
    {
        // Guard: the player's death check can fire every frame while an enemy
        // is still touching them, so only award coins / show the screen once.
        if (isGameOver) return;
        isGameOver = true;

        // Convert the run's XP into coins and bank them.
        int coinsEarned = 0;
        PlayerXP playerXP = FindObjectOfType<PlayerXP>();
        if (playerXP != null)
        {
            coinsEarned = playerXP.EndRunAndAddCoins();
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins earned: " + coinsEarned + "\nTotal coins: " + CoinBank.GetCoins();
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}