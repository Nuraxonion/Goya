using UnityEngine;

public class MetaXPManager : MonoBehaviour
{
    public static MetaXPManager instance;

    public float metaXP = 0f;
    private const string META_XP_KEY = "MetaXP";

    void Awake()
    {
        // Singleton pattern - persist across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMetaXP();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Add meta XP (typically 1/10th of run XP)
    public void AddMetaXP(float amount)
    {
        metaXP += amount;
        SaveMetaXP();
        Debug.Log("Meta XP added: " + amount + ". Total Meta XP: " + metaXP);
    }

    // Get current meta XP
    public float GetMetaXP()
    {
        return metaXP;
    }

    // Save meta XP to player prefs
    private void SaveMetaXP()
    {
        PlayerPrefs.SetFloat(META_XP_KEY, metaXP);
        PlayerPrefs.Save();
    }

    // Load meta XP from player prefs
    private void LoadMetaXP()
    {
        if (PlayerPrefs.HasKey(META_XP_KEY))
        {
            metaXP = PlayerPrefs.GetFloat(META_XP_KEY);
            Debug.Log("Meta XP loaded: " + metaXP);
        }
        else
        {
            metaXP = 0f;
            Debug.Log("No existing Meta XP found. Starting at 0.");
        }
    }

    // Reset meta XP (for testing or new game+)
    public void ResetMetaXP()
    {
        metaXP = 0f;
        SaveMetaXP();
        Debug.Log("Meta XP reset to 0.");
    }
}