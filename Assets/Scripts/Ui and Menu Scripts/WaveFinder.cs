using UnityEngine;

public class WaveFinder : MonoBehaviour
{
    void Start()
    {
        Debug.Log("===== FINDING WAVE =====");

        // Try to find the ability
        GameObject ability = GameObject.Find("WaveAttackAbility");
        Debug.Log($"WaveAttackAbility found: {ability != null}");
        if (ability != null)
        {
            Debug.Log($"WaveAttackAbility active: {ability.activeSelf}");
        }

        // Try to find the bubble
        GameObject bubble = GameObject.Find("WaveAttackBubble");
        Debug.Log($"WaveAttackBubble found: {bubble != null}");
        if (bubble != null)
        {
            Debug.Log($"WaveAttackBubble active: {bubble.activeSelf}");
        }

        // Search for anything with "Wave" in the name
        Debug.Log("===== ALL OBJECTS WITH 'Wave' =====");
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.name.Contains("Wave"))
            {
                Debug.Log($"Found: '{go.name}' - Active: {go.activeSelf} - Parent: {(go.transform.parent != null ? go.transform.parent.name : "None")}");
            }
        }
        Debug.Log("===== SEARCH COMPLETE =====");
    }
}