using UnityEngine;

public class DayNightShadow : MonoBehaviour
{
    private void Start()
    {
        ScaleToScreen();
    }

    private void ScaleToScreen()
    {
        // Получаем камеру
        Camera camera = Camera.main;

        // Вычисляем ширину и высоту экрана
        float screenWidth = camera.orthographicSize * camera.aspect * 2;
        float screenHeight = camera.orthographicSize * 2;

        // Устанавливаем размер квадрата
        transform.localScale = new Vector3(screenWidth, screenHeight, 1);
    }
}
