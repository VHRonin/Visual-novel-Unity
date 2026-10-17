using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // новая система

public class TapSpawner : MonoBehaviour
{
    [Header("Tap Effect Prefab")]
    public GameObject tapEffectPrefab;
    public RectTransform canvasRect;

    [Header("Options")]
    public Camera uiCamera;  // для Screen Space - Camera
    public bool ignoreIfPointerOverUI = true;

    void Update()
    {
        // для сенсорного ввода
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                TrySpawnAtScreenPos(touch.position.ReadValue());
            }
        }

        // для мыши (редактор)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySpawnAtScreenPos(Mouse.current.position.ReadValue());
        }
    }

    private void TrySpawnAtScreenPos(Vector2 screenPos)
    {
        // if (ignoreIfPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //     return;

        Vector2 localPoint;
        bool converted;
        
        converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out localPoint);

        if (!converted) return;

        GameObject go = Instantiate(tapEffectPrefab, canvasRect);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = localPoint;
    }
}