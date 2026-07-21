using UnityEngine;
using UnityEngine.UI;

public class PipTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("===== PIP TEST STARTED =====");

        // Find the Fireball bubble
        GameObject bubble = GameObject.Find("FireballBubble");

        if (bubble == null)
        {
            Debug.LogError("❌ FireballBubble NOT FOUND!");
            return;
        }

        Debug.Log($"✅ FireballBubble FOUND!");

        // Force all pips to red
        for (int i = 1; i <= 8; i++)
        {
            Transform pip = bubble.transform.Find("Pip" + i);

            if (pip == null)
            {
                Debug.LogWarning($"⚠️ Pip{i} not found!");
                continue;
            }

            Image pipImage = pip.GetComponent<Image>();

            if (pipImage == null)
            {
                Debug.LogWarning($"⚠️ Pip{i} has no Image component!");
                continue;
            }

            pipImage.color = Color.red;
            pip.gameObject.SetActive(true);
            Debug.Log($"✅ Pip{i} set to RED!");
        }

        Debug.Log("===== PIP TEST COMPLETE =====");
    }
}