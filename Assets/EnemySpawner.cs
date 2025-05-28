using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    [Header("Ссылки")]
    public GameObject[] enemyPrefab;
    public Transform player;

    [Header("Мир и блоки")]
    public float blockSize = 1f; // размер одного блока

    [Header("Спавн")]
    public float spawnInterval = 20f;
    public int spawnPerWave = 5;
    public int maxEnemies = 30;
    public float minSpawnDistance = 20f;
    public float maxSpawnDistance = 40f;

    private float timer = 0f;
    private List<GameObject> enemies = new List<GameObject>();

    void Update()
    {
        timer += Time.deltaTime;
        enemies.RemoveAll(e => e == null);

        if (timer >= spawnInterval && enemies.Count < maxEnemies)
        {
            SpawnWave();
            timer = 0f;
        }
    }

    void SpawnWave()
    {
        int spawned = 0;
        int attempts = 100;

        while (spawned < spawnPerWave && attempts-- > 0)
        {
            if (HelperClass.isFullyGenerated)
            {
                Vector3? spawnPos = FindSurfacePositionOutsideCamera();
                if (spawnPos.HasValue)
                {
                    GameObject enemy = Instantiate(enemyPrefab[Random.Range(0, enemyPrefab.Count())], spawnPos.Value, Quaternion.identity);
                    enemies.Add(enemy);
                    spawned++;
                }
            }
        }
    }

    Vector3? FindSurfacePositionOutsideCamera()
    {
        //HelperClass.map.GetLength(0);
        //HelperClass.map.GetLength(1);

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 checkPos = player.position + (Vector3)offset;

        int x = Mathf.RoundToInt(checkPos.x / blockSize);
        int y = Mathf.RoundToInt(checkPos.y / blockSize);

        //if (x < 0 || x >= ProceduralGeneration.map.GetLength(0) || y < 1 || y >= ProceduralGeneration.map.GetLength(1) - 1)
            return null;
        // Условие: тайл под ногами есть, сверху пусто
        //if (ProceduralGeneration.map[x, y - 1] != 0 && ProceduralGeneration.map[x, y] == 0)
        //{
        //    Vector3 spawnPos = new Vector3(x * blockSize + blockSize / 2f, y * blockSize + blockSize / 2f, 0f);
        //    return spawnPos;
        //}

        return null;
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
    }
}
