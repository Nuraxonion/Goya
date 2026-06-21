using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    public void ShowGameOver()
    {
        // Find and call the meta XP function
        PlayerXP playerXP = FindObjectOfType<PlayerXP>();
        if (playerXP != null)
        {
            playerXP.EndRunAndAddMetaXP();
        }

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
}