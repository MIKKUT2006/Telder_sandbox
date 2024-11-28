using UnityEngine;
using System.Collections;

public class RainManager : MonoBehaviour
{
    public RainPool rainPool; // Убедитесь, что это поле заполнено в инспекторе
    public float spawnInterval = 0.1f; // Интервал спавна капель
    public float spawnOffset = 1f; // Смещение для спавна капель сверху

    private Camera mainCamera;

    private void Start()
    {
        // Получаем ссылку на основную камеру
        mainCamera = Camera.main;
        if (rainPool == null)
        {
            Debug.LogError("RainPool is not assigned in the RainManager.");
            return;
        }

        StartCoroutine(SpawnRainDrops());
    }

    private System.Collections.IEnumerator SpawnRainDrops()
    {
        while (true)
        {
            // Определите границы камеры
            float leftBound = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
            float rightBound = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

            // Определяем высоту спавна
            float spawnHeight = mainCamera.transform.position.y + mainCamera.orthographicSize + spawnOffset;

            // Генерация случайной позиции спавна в пределах границ камеры
            Vector2 spawnPosition = new Vector2(
                Random.Range(leftBound, rightBound), // Случайная позиция по X в пределах камеры
                spawnHeight // Позиция по Y
            );

            GameObject drop = rainPool.GetRainDrop();
            if (drop != null)
            {
                drop.transform.position = spawnPosition;
                //drop.GetComponent<RainDrop>().ResetDrop();
            }
            else
            {
                Debug.LogWarning("No available rain drops in the pool.");
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
