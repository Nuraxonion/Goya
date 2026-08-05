using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthFill;
    public PlayerHealth playerHealth;

    void Update()
    {
        healthFill.fillAmount = playerHealth.currentHealth / playerHealth.maxHealth;
    }
}