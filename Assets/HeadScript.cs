using UnityEngine;

public class HeadScript : MonoBehaviour
{
    public Transform head;
    public Transform body;
    public float maxAngle = 45f;
    public float smoothSpeed = 5f;

    private float currentAngle;

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = 10; // Расстояние до плоскости курсора

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        // Получаем направление с учетом поворота тела:
        Vector3 direction = worldPosition - head.position;
        direction = body.InverseTransformDirection(direction); // Преобразуем в локальные координаты тела

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);

        currentAngle = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref currentAngle, smoothSpeed * Time.deltaTime);

        head.localRotation = Quaternion.Euler(0, 0, currentAngle); // Используем localRotation
    }
}
