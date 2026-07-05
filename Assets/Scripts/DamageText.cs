using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    private TextMeshProUGUI text;

    public float lifetime = 1f;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        Destroy(gameObject, lifetime);
    }

    public void SetDamage(float damage)
    {
        text.text = damage.ToString("0");
    }
}