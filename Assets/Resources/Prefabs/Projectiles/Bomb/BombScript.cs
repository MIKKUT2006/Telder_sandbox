using System.Collections;
using System.IO;
using System.Linq;
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
    private int chunkSize; // Добавили переменную для размера чанка
    AudioSource audioSource;
    GameObject BlockGameObject;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        timer = explosionDelay;
        chunkSize = HelperClass.chunkSize; // Инициализируем chunkSize здесь
        Initialize(ProceduralGeneration.map, HelperClass.Chunks);

        BlockGameObject = (GameObject)Resources.Load($"Public Elements/Prefabs/Inventory/Item");
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
        chunkSize = HelperClass.chunkSize; // Инициализируем chunkSize здесь

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
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        // 1. Создание частиц взрыва
        if (explosionParticlesPrefab != null)
        {
            Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
        }
        // 2. Разрушение тайлов и обновление массива
        ExplodeTilemap();

        StartCoroutine(SoundTimer());
    }

    private IEnumerator SoundTimer()
    {
        yield return new WaitForSeconds(0.3f);
        if (audioSource.isPlaying == true)
        {
            StartCoroutine(SoundTimer());
        }
        else
        {
            Destroy(gameObject);
        }
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
        // Получаем координаты чанка

        int chunkX = HelperClass.chunkSize;
        chunkX = Mathf.FloorToInt(worldPosition.x / (float)chunkSize);

        int chunkY = HelperClass.chunkSize;
        chunkY = Mathf.FloorToInt(worldPosition.y / (float)chunkSize);

        // Получаем локальную позицию тайла внутри чанка
        Vector3Int localTilePosition = HelperClass.Chunks[chunkX].WorldToCell(worldPosition);

        //Вычисляем глобальную позицию тайла на карте
        int globalX = chunkX * chunkSize + localTilePosition.x;
        int globalY = chunkY * chunkSize + localTilePosition.y;

        //Debug.Log(globalX);
        //Debug.Log(globalY);

        // Удаляем тайл
        //ProceduralGeneration.map[globalX, globalY] = 0;
        HelperClass.Chunks[chunkX].SetTile(localTilePosition, null);
        int x = (int)worldPosition.x;
        int y = (int)worldPosition.y;

        int blockId = BlocksData.allBlocks[ProceduralGeneration.map[x, y]].blockIndex;                 // Получаем айди блока
        
        // Проверка что это не блок света
        if (blockId != 4 && blockId != 0)
        {
            Debug.Log(blockId);
            // Цикл для создания всех выпадающих предметов с блока
            foreach (int drop in BlocksData.allBlocks.Where(x => x.blockIndex == blockId).FirstOrDefault().dropId)
            {
                Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
                AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
                Debug.Log(currentDrop.name);
                GameObject newBlock = Instantiate(BlockGameObject, newpos, Quaternion.identity);
                newBlock.name = currentDrop.blockIndex.ToString();
                Sprite sprite = null;
                // Получение его текстуры, если она есть

                //// Загружаем изображение из файла
                //float pixelsPerUnit = 16;

                //byte[] imageData = File.ReadAllBytes(currentDrop.imagePath);
                //Texture2D texture = new Texture2D(16, 16);
                //texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
                //texture.filterMode = FilterMode.Point;

                //// ������������ ������� ������� � ������ pixelsPerUnit
                //float width = texture.width / 16;
                //float height = texture.height / 16;

                //// �������� ������� �� ��������
                //Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                Sprite newSprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));

                newBlock.GetComponent<SpriteRenderer>().sprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));

                //ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
                //newParticles.gameObject.SetActive(true);
                //Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;
                //destroyMaterial.mainTexture = texture;
            }
            // Конец цикла
        }

    }
}
