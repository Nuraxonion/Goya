using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;
    public Canvas canvas;

    public GameObject damagePrefab;

    void Awake()
    {
        Instance = this;
        canvas = FindFirstObjectByType<Canvas>();
    }

    public void ShowDamage(float damage, Vector3 worldPosition)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

        GameObject popup = Instantiate(damagePrefab);
        popup.transform.SetParent(canvas.transform, false);

        popup.transform.position = screenPos;

        popup.GetComponent<DamageText>().SetDamage(damage);
    }
}