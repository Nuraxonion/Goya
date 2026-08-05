using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    public AudioClip music;

    void Start()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMusic(music);
    }
}