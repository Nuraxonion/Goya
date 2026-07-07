using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UpgradeNotification : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI notificationText;
    public Image notificationIcon;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Animation Settings")]
    public float popInDuration = 0.3f;
    public float displayDuration = 2.0f;
    public float popOutDuration = 0.2f;

    [Header("Visual Settings")]
    public Color successColor = Color.green;
    public Color maxColor = Color.yellow;
    public Color errorColor = Color.red;

    [Header("Text Templates")]
    public string upgradeTextTemplate = " HEALTH UPGRADED! +{0} HP";
    public string maxTextTemplate = " MAX HEALTH REACHED!";
    public string insufficientFundsText = " NOT ENOUGH COINS!";

    [Header("Audio")]
    public AudioClip notificationSound;

    private AudioSource audioSource;
    private Coroutine currentCoroutine;

    void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (notificationText == null)
        {
            notificationText = GetComponentInChildren<TextMeshProUGUI>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && notificationSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        gameObject.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void ShowUpgradeNotification(float healthIncrease)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text = string.Format(upgradeTextTemplate, healthIncrease);
            notificationText.color = successColor;
        }

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(PlayNotification());
    }

    public void ShowMaxUpgradeNotification()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text = maxTextTemplate;
            notificationText.color = maxColor;
        }

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(PlayNotification());
    }

    public void ShowInsufficientFundsNotification()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text = insufficientFundsText;
            notificationText.color = errorColor;
        }

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(PlayNotification());
    }

    private IEnumerator PlayNotification()
    {
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound);
        }

        rectTransform.localScale = Vector3.zero;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        float elapsed = 0f;
        while (elapsed < popInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popInDuration;

            float scale = 1 + Mathf.Sin(t * Mathf.PI * 2) * 0.1f;
            rectTransform.localScale = Vector3.one * Mathf.Clamp01(scale);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            }

            yield return null;
        }

        rectTransform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(displayDuration);

        elapsed = 0f;
        while (elapsed < popOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popOutDuration;

            rectTransform.localScale = Vector3.one * (1 - t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1 - t;
            }

            yield return null;
        }

        gameObject.SetActive(false);
        rectTransform.localScale = Vector3.one;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        currentCoroutine = null;
    }
}