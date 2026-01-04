using UnityEngine;

public class ContinueIcon : MonoBehaviour
{
    public float amplitude = 20f;   // в пикселях UI, а не 0.1
    public float speed = 2.0f;
    // public bool waitingForInput;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * speed) * amplitude;

        rectTransform.anchoredPosition = originalAnchoredPos + Vector2.up * yOffset;
    }

    public void ResetPosition()
    {
        rectTransform.anchoredPosition = originalAnchoredPos;
    }
}
