using UnityEngine;

public class DayNightShadow : MonoBehaviour
{
    private void Start()
    {
        ScaleToScreen();
    }

    private void ScaleToScreen()
    {
        // �������� ������
        Camera camera = Camera.main;

        // ��������� ������ � ������ ������
        float screenWidth = camera.orthographicSize * camera.aspect * 2;
        float screenHeight = camera.orthographicSize * 2;

        // ������������� ������ ��������
        transform.localScale = new Vector3(screenWidth, screenHeight, 1);
    }
}
