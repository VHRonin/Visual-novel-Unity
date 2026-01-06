using UnityEngine;
using UnityEngine.EventSystems;

public class TapSpawner : MonoBehaviour
{
    [Header("Tap Effect Prefab")]
    public GameObject tapEffectPrefab;  // твой UI Image с TapEffect.cs
    public RectTransform canvasRect;    // Canvas, куда будем спавнить

    [Header("Options")]
    public Camera uiCamera;             // если Canvas Screen Space - Camera
    public bool ignoreIfPointerOverUI = true;  // не спавнить при тапе по кнопке

    void Update()
    {
        // Touch для мобильных
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                    TrySpawnAtScreenPos(touch.position);
            }
            return;
        }

        // Mouse для теста в редакторе
        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnAtScreenPos(Input.mousePosition);
        }
    }

    private void TrySpawnAtScreenPos(Vector2 screenPos)
    {
        if (ignoreIfPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 localPoint;
        bool converted;

        // if (canvasRect.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay)
        // {
        //     converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPoint);
        // }
        // else
        // {
        //     converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out localPoint);
        // }

        converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out localPoint);

        if (!converted) return;

        // спавним префаб
        GameObject go = Instantiate(tapEffectPrefab, canvasRect);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = localPoint;  // ставим по тапу
    }
}