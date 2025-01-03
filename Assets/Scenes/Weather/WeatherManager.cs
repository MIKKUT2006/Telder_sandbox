using System.Collections;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public RainManager rainManager; // ������ �� ��� RainManager
    public float rainDuration = 5f; // ����������������� ����� � �������� (1 ������)
    public float checkInterval = 5f; // �������� �������� ������ ����� � �������� (1 ������)

    public bool isRaining = false;

    void Start()
    {
        StartCoroutine(WeatherCycle());
    }

    IEnumerator WeatherCycle()
    {
        while (true)
        {
            // �������� �� ������ ����� ������ ������
            if (isRaining == false && Random.value < 0.5f) // 50% ���� ������ �����
            {
                isRaining = true;
                StartCoroutine(Rain());
            }
            Debug.Log($"����� ����� ������� = {isRaining}");
            yield return new WaitForSeconds(checkInterval);
        }
    }

    IEnumerator Rain()
    {
        Debug.Log("������ �����");
        // ������ �����
        rainManager.StartRain(); // �������� ����� startRain() ������ RainManager.
        if (HelperClass.currentBiome != HelperClass.Biomes.Snow)
        {
            GetComponent<AudioSource>().Play();
        }
        // ����� ��� rainDuration ������
        yield return new WaitForSeconds(rainDuration);
        isRaining = false;
        rainManager.StopRain(); // �������� ����� stopRain() ������ RainManager.
        Debug.Log($"����� �������");
        if (HelperClass.currentBiome != HelperClass.Biomes.Snow)
        {
            GetComponent<AudioSource>().Stop();
        }
        // ��������� �����
    }
}
