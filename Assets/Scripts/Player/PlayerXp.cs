using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    public float xpTotal = 0;

    public void AddXP(float amount)
    {
        xpTotal += amount;
        Debug.Log("Total XP: " + xpTotal);
    }
}
