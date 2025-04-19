using UnityEngine;

public class MenuBGScript : MonoBehaviour
{
    public float parallaxStrength = 0.02f;
    public float maxOffset = 30f; // Увеличил maxOffset для большего эффекта
    public float smoothTime = 0.3f; // Время сглаживания движения

    private Vector3 _initialPosition;
    private RectTransform _rectTransform;
    private Vector3 _currentVelocity; // Текущая скорость для SmoothDamp
    private Vector2 _backgroundSize;   // Размер фона

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("MenuBackgroundParallax скрипт должен быть на Canvas UI элементе!");
            enabled = false;
            return;
        }

        _initialPosition = transform.position;

        // Получаем размер RectTransform фона
        _backgroundSize = _rectTransform.sizeDelta;

        // Увеличиваем размер фона, чтобы он был больше экрана на maxOffset
        _rectTransform.sizeDelta = new Vector2(_backgroundSize.x + 2 * maxOffset, _backgroundSize.y + 2 * maxOffset);

        // Перемещаем фон в центр, чтобы компенсировать увеличение размера
        transform.position = _initialPosition; // + new Vector3(-maxOffset, -maxOffset, 0);

        _currentVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        // Получаем позицию мыши в мировых координатах
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Вычисляем целевое смещение на основе позиции мыши
        Vector3 targetOffset = (mousePosition - _initialPosition) * parallaxStrength;

        // Ограничиваем смещение максимальным значением
        targetOffset.x = Mathf.Clamp(targetOffset.x, -maxOffset, maxOffset);
        targetOffset.y = Mathf.Clamp(targetOffset.y, -maxOffset, maxOffset);

        // Используем SmoothDamp для плавного движения
        Vector3 newPosition = _initialPosition + targetOffset;
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref _currentVelocity, smoothTime);
    }

    void OnDisable()
    {
        if (_rectTransform != null)
            _rectTransform.sizeDelta = _backgroundSize; //Возвращаем исходный размер, если объект отключается
    }
}
