using System.Collections.Generic;
using UnityEngine;

public class RainPool : MonoBehaviour
{
    public GameObject rainDropPrefab; // Префаб дождевой капли
    public GameObject snowDropPrefab; // Префаб снежинки
    public int poolSize = 100;
    public List<GameObject> rainDropPool;

    private void Awake()
    {
        GeneratePool(false);
    }

    private void GeneratePool(bool isSnow)
    {
        if (!isSnow)
        {
            HelperClass.weatherFallSpeed = 7f;
            rainDropPool = new List<GameObject>();
            // Инициализация пула
            for (int i = 0; i < poolSize; i++)
            {
                GameObject drop = Instantiate(rainDropPrefab);
                drop.SetActive(false);
                rainDropPool.Add(drop);
            }

        }
        else
        {
            HelperClass.weatherFallSpeed = 2f;
            rainDropPool = new List<GameObject>();
            // Инициализация пула
            for (int i = 0; i < poolSize; i++)
            {
                GameObject drop = Instantiate(snowDropPrefab);
                drop.SetActive(false);
                rainDropPool.Add(drop);
            }
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
        return null; // Если нет доступных капель
    }

    public void ReturnRainDrop(GameObject drop)
    {
        drop.SetActive(false);
    }
}