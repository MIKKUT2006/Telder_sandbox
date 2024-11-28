using System.Collections.Generic;
using UnityEngine;

public class RainPool : MonoBehaviour
{
    public GameObject rainDropPrefab; // ������ �������� �����
    public int poolSize = 100;
    public List<GameObject> rainDropPool;

    private void Awake()
    {
        rainDropPool = new List<GameObject>();
        // ������������� ����
        for (int i = 0; i < poolSize; i++)
        {
            GameObject drop = Instantiate(rainDropPrefab);
            drop.SetActive(false);
            rainDropPool.Add(drop);
        }
    }

    public GameObject GetRainDrop()
    {
        foreach (GameObject drop in rainDropPool)
        {
            if (!drop.activeInHierarchy)
            {
                drop.SetActive(true);
                return drop;
            }
        }
        return null; // ���� ��� ��������� ������
    }

    public void ReturnRainDrop(GameObject drop)
    {
        drop.SetActive(false);
    }
}