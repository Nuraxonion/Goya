using System.Collections;
using UnityEngine;

public class FadeStrokeController : MonoBehaviour
{
    public float lifetime = 2.5f;
    public float fadeDuration = 1f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        yield return new WaitForSeconds(lifetime);

        float t = 0;
        Color start = lr.startColor;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start.a, 0, t / fadeDuration);

            Color c = new Color(start.r, start.g, start.b, a);
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        Destroy(gameObject);
    }
}