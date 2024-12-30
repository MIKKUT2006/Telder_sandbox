using System.Collections;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombScript : MonoBehaviour
{
    public float explosionRadius = 3f;
    public float explosionDelay = 3f;
    public GameObject explosionParticlesPrefab;
    public int damage = 1;

    private Rigidbody2D rb;
    private float timer;
    private int[,] tileMapData;
    private Tilemap[] chunkList;
    private int chunkSize; // �������� ���������� ��� ������� �����


    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        timer = explosionDelay;
        chunkSize = HelperClass.chunkSize; // �������������� chunkSize �����
        Initialize(ProceduralGeneration.map, HelperClass.Chunks);
    }

    public void Initialize(int[,] mapData, Tilemap[] chunks)
    {
        if (mapData == null)
        {
            Debug.LogError("mapData is null!");
            return;
        }

        if (chunks == null)
        {
            Debug.LogError("chunks is null!");
            return;
        }
        tileMapData = mapData;
        //tileMap = map;
        chunkList = chunks;
        //Debug.Log("Initialize called: tileMapData=" + tileMapData);
        chunkSize = HelperClass.chunkSize; // �������������� chunkSize �����

        Debug.Log("Number of chunks: " + chunks.Length);
        for (int i = 0; i < chunks.Length; i++)
        {
            Debug.Log("Chunk " + i + ": position = " + chunks[i].transform.position + ", cellBounds.center = " + chunks[i].cellBounds.center);
        }

        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(explosionDelay);
        Explode();
    }

    private void Explode()
    {
        // 1. �������� ������ ������
        if (explosionParticlesPrefab != null)
        {
            Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
        }
        // 2. ���������� ������ � ���������� �������
        ExplodeTilemap();
        // 3. ����������� �����
        Destroy(gameObject);

    }


    private void ExplodeTilemap()
    {
        Vector3 explosionCenter = transform.position;
        for (int x = -Mathf.RoundToInt(explosionRadius); x <= Mathf.RoundToInt(explosionRadius); x++)
        {
            for (int y = -Mathf.RoundToInt(explosionRadius); y <= Mathf.RoundToInt(explosionRadius); y++)
            {
                Vector3 worldPosition = explosionCenter + new Vector3(x, y, 0);
                if (Vector2.Distance(explosionCenter, worldPosition) <= explosionRadius)
                {
                    DestroyTileAtWorldPosition(worldPosition);
                }
            }
        }
    }

    private void DestroyTileAtWorldPosition(Vector3 worldPosition)
    {
        // �������� ���������� �����

        int chunkX = HelperClass.chunkSize;
        chunkX = Mathf.FloorToInt(worldPosition.x / (float)chunkSize);

        int chunkY = HelperClass.chunkSize;
        chunkY = Mathf.FloorToInt(worldPosition.y / (float)chunkSize);

        // �������� ��������� ������� ����� ������ �����
        Vector3Int localTilePosition = HelperClass.Chunks[chunkX].WorldToCell(worldPosition);

        //��������� ���������� ������� ����� �� �����
        int globalX = chunkX * chunkSize + localTilePosition.x;
        int globalY = chunkY * chunkSize + localTilePosition.y;

        Debug.Log(globalX);
        Debug.Log(globalY);

        // ������� ����
        //ProceduralGeneration.map[globalX, globalY] = 0;
        HelperClass.Chunks[chunkX].SetTile(localTilePosition, null);
    }
}
