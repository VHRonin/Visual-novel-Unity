using UnityEngine;

public class ContinueIcon : MonoBehaviour
{
    public float amplitude = 0.1f;
    public float speed = 1.0f;
    public bool waitingForInput;

    private Vector3 originalPosition;
    
    void Start()
    {
        originalPosition = transform.position;
    }

    void Update()
    {
        // Вычисляем новую позицию с колебаниями вверх-вниз
        float yOffset = Mathf.Sin(Time.time * speed) * amplitude;
        
        // Меняем положение объекта
        transform.position = new Vector3(originalPosition.x, originalPosition.y + yOffset, originalPosition.z);
    }
}
