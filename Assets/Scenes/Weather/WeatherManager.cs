using System.Collections;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public RainManager rainManager; // Ссылка на ваш RainManager
    public float rainDuration = 5f; // Продолжительность дождя в секундах (1 минута)
    public float checkInterval = 5f; // Интервал проверки начала дождя в секундах (1 минута)

    public bool isRaining = false;

    void Start()
    {
        StartCoroutine(WeatherCycle());
    }

    IEnumerator WeatherCycle()
    {
        while (true)
        {
            // Проверка на начало дождя каждую минуту
            if (isRaining == false && Random.value < 0.5f) // 50% шанс начала дождя
            {
                isRaining = true;
                StartCoroutine(Rain());
            }
            Debug.Log($"Дождь после условия = {isRaining}");
            yield return new WaitForSeconds(checkInterval);
        }
    }

    IEnumerator Rain()
    {
        Debug.Log("НАчала цикла");
        // Запуск дождя
        rainManager.StartRain(); // Вызовите метод startRain() вашего RainManager.
        if (HelperClass.currentBiome != HelperClass.Biomes.Snow)
        {
            GetComponent<AudioSource>().Play();
        }
        // Дождь идёт rainDuration секунд
        yield return new WaitForSeconds(rainDuration);
        isRaining = false;
        rainManager.StopRain(); // Вызовите метод stopRain() вашего RainManager.
        Debug.Log($"Дождь окончен");
        if (HelperClass.currentBiome != HelperClass.Biomes.Snow)
        {
            GetComponent<AudioSource>().Stop();
        }
        // Остановка дождя
    }
}
