using TMPro;
using UnityEngine;

public class FloatingTextPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private float riseSpeed = 90f;
    [SerializeField] private float lifetime = 0.85f;
    [SerializeField] private float scalePunch = 0.18f;

    private Vector3 baseScale;
    private float age;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
    }

    public void Initialize(string message, Color color, Vector2 screenPosition)
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (label != null)
        {
            label.text = message;
            label.color = color;
        }

        if (rectTransform != null)
        {
            rectTransform.position = screenPosition;
            baseScale = rectTransform.localScale;
        }

        age = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;

        float normalizedAge = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);

        if (rectTransform != null)
        {
            rectTransform.position += Vector3.up * riseSpeed * Time.unscaledDeltaTime;
            float punch = 1f + Mathf.Sin(normalizedAge * Mathf.PI) * scalePunch;
            rectTransform.localScale = baseScale * punch;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f - normalizedAge;
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}