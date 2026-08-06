using System.Collections;
using UnityEngine;

public class BrushStamp : MonoBehaviour
{
    private SpriteRenderer sr;

    public float fadeSpeed = 1.5f;


    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        sr.color = Color.red;
    }


    public void StartFade()
    {
        StartCoroutine(Burn());
    }


    IEnumerator Burn()
    {
        float t = 0;


        Color start = Color.red;
        Color end = Color.black;


        // Красный -> черный
        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;

            sr.color = Color.Lerp(
                start,
                end,
                t
            );

            yield return null;
        }


        // исчезновение
        Color c = sr.color;


        while (c.a > 0)
        {
            c.a -= Time.deltaTime * fadeSpeed;

            sr.color = c;

            yield return null;
        }


        Destroy(gameObject);
    }
}