using UnityEngine;

public class WaveAnimation : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    public void Play()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        //Debug.Log("WaveAnimation: Play() called");
    }

    public void Stop()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}