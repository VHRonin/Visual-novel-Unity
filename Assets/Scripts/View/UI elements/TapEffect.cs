using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TapEffect : MonoBehaviour
{
    public float duration = 0.35f;
    public float endRadius = 0.35f;    // финальный радиус в UV (0..0.5 - безопасно)
    public float startScale = 0.8f;
    public float endScale = 1.2f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

    private Material mat;
    private Image img;
    private float elapsed = 0f;

    void Awake()
    {
        img = GetComponent<Image>();
        // создаём инстанс материала чтобы не менять shared material
        mat = Instantiate(img.material);
        img.material = mat;

        // начальные значения (на всякий случай)
        mat.SetFloat("_Radius", 0f);
        mat.SetFloat("_Alpha", 1f);
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float e = ease.Evaluate(t);

        // Анимируем радиус в безопасном диапазоне (не до 1!)
        mat.SetFloat("_Radius", Mathf.Lerp(0f, endRadius, e));

        // Альфа: 1 -> 0
        mat.SetFloat("_Alpha", 1f - e);

        // Scale
        float scl = Mathf.Lerp(startScale, endScale, e);
        transform.localScale = Vector3.one * scl;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}