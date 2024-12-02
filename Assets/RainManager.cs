using UnityEngine;
using System.Collections;

public class RainManager : MonoBehaviour
{
    public RainPool rainPool; // ���������, ��� ��� ���� ��������� � ����������
    public float spawnInterval = 0.1f; // �������� ������ ������
    public float spawnOffset = 1f; // �������� ��� ������ ������ ������

    private Camera mainCamera;

    private void Awake()
    {
        // �������� ������ �� �������� ������
        mainCamera = Camera.main;
        if (rainPool == null)
        {
            Debug.LogError("RainPool is not assigned in the RainManager.");
            return;
        }

        //StartCoroutine(SpawnRainDrops());
    }

    private System.Collections.IEnumerator SpawnRainDrops()
    {
        while (true)
        {
            // ���������� ������� ������
            float leftBound = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
            float rightBound = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

            // ���������� ������ ������
            float spawnHeight = mainCamera.transform.position.y + mainCamera.orthographicSize + spawnOffset;

            // ��������� ��������� ������� ������ � �������� ������ ������
            Vector2 spawnPosition = new Vector2(
                Random.Range(leftBound, rightBound), // ��������� ������� �� X � �������� ������
                spawnHeight // ������� �� Y
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

    public void StartRain()
    {
        // ��������� ����� ������
        StartCoroutine(SpawnRainDrops());
        // ����� ����� �������� ��������� ������ �����, ���� ��� � ��� ����
    }

    public void StopRain()
    {
        // ���������� ����� ������
        Debug.Log("�������� �����������");
        StopAllCoroutines(); // ������������� �������� ������
        // ������� ��� ������������ �����.
        //foreach (Transform child in transform)
        //{
        //    Destroy(child.gameObject);
        //}
        // ����� ����� �������� ����������� ������ �����, ���� ��� � ��� ����.
    }
}
