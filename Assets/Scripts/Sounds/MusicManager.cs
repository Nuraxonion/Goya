using UnityEngine;

public class MusicManager : MonoBehaviour
{
    void Awake()
    {
        // Deletes music object duplicate
        if (FindObjectsByType<MusicManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        // Do not destroy on new scene
        DontDestroyOnLoad(gameObject);
    }
}