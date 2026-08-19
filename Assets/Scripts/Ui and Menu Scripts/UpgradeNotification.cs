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
    // {0} is the upgrade name, {1} its effect line - so one template covers every
    // meta upgrade instead of needing a pair per upgrade.
    public string upgradeTextTemplate = " {0} UPGRADED! {1}";
    public string maxTextTemplate = " {0} FULLY UPGRADED!";
    public string insufficientFundsText = " NOT ENOUGH COINS!";
    public string refundTextTemplate = " REFUNDED {0} COINS";

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

    public void ShowUpgradeNotification(string upgradeName, string statsText)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text =
                string.Format(upgradeTextTemplate, upgradeName, statsText);
            notificationText.color = successColor;
        }

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(PlayNotification());
    }

    public void ShowMaxUpgradeNotification(string upgradeName)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text =
                string.Format(maxTextTemplate, upgradeName);
            notificationText.color = maxColor;
        }

        gameObject.SetActive(true);
        currentCoroutine = StartCoroutine(PlayNotification());
    }

    public void ShowRefundNotification(int coins)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        if (notificationText != null)
        {
            notificationText.text = string.Format(refundTextTemplate, coins);
            notificationText.color = successColor;
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