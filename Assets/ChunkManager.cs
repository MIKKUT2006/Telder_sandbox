using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;
using static UnityEditor.PlayerSettings;

public class ChunkManager : MonoBehaviour
{
    public int chunkSize = 16;
    public int loadRadius = 2;
    public int chunkBuffer = 2;
    public GameObject chunkPrefab;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new();
    private Transform player;
    private Vector2Int prevPlayerChunk = new(int.MinValue, int.MinValue);

    void Start()
    {
        TileRegistry.Init();
        player = GameObject.FindWithTag("Player").transform;
        UpdateChunks();
    }

    float checkTimer = 0f;
    void FixedUpdate()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer > 0.3f) // обновлять раз в 0.3 секунды
        {
            checkTimer = 0f;
            UpdateChunks();
        }
    }
    Vector2Int WorldToChunk(Vector3 pos)
    {
        int cx = Mathf.FloorToInt(pos.x / chunkSize);
        int cy = Mathf.FloorToInt(pos.y / chunkSize);
        return new Vector2Int(cx, cy);
    }

    void UpdateChunks()
    {
        var neededChunks = GetVisibleChunks();

        // Загружаем видимые
        foreach (var coord in neededChunks)
            if (!loadedChunks.ContainsKey(coord))
                StartCoroutine(GenerateChunkAsync(coord)); // или обычная генерация

        // Удаляем невидимые
        var toRemove = new List<Vector2Int>();
        foreach (var coord in loadedChunks.Keys)
            if (!neededChunks.Contains(coord))
                toRemove.Add(coord);

        foreach (var coord in toRemove)
            UnloadChunk(coord);
    }
    Bounds GetCameraWorldBounds()
    {
        Camera cam = Camera.main;
        Vector3 camPos = cam.transform.position;

        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 center = cam.transform.position;
        Vector3 size = new Vector3(camWidth, camHeight, 1f);

        return new Bounds(center, size);
    }
    HashSet<Vector2Int> GetVisibleChunks()
    {
        Bounds camBounds = GetCameraWorldBounds();

        float minX = camBounds.min.x - chunkSize * chunkBuffer;
        float maxX = camBounds.max.x + chunkSize * chunkBuffer;
        float minY = camBounds.min.y - chunkSize * chunkBuffer;
        float maxY = camBounds.max.y + chunkSize * chunkBuffer;

        Vector2Int minChunk = WorldToChunk(new Vector3(minX, minY));
        Vector2Int maxChunk = WorldToChunk(new Vector3(maxX, maxY));

        HashSet<Vector2Int> result = new();
        for (int cx = minChunk.x; cx <= maxChunk.x; cx++)
            for (int cy = minChunk.y; cy <= maxChunk.y; cy++)
                result.Add(new Vector2Int(cx, cy));

        return result;
    }
    IEnumerator GenerateChunkAsync(Vector2Int coord)
    {
        var chunk = new Chunk();
        chunk.coord = coord;
        chunk.tiles = new int[chunkSize, chunkSize];
        chunk.bgTiles = new int[chunkSize, chunkSize];

        chunk.go = Instantiate(chunkPrefab, transform);
        chunk.go.name = $"Chunk_{coord.x}_{coord.y}";
        chunk.ground = chunk.go.transform.Find("Ground").GetComponent<Tilemap>();
        chunk.bg = chunk.go.transform.Find("BG").GetComponent<Tilemap>();
        chunk.light = chunk.go.transform.Find("Light").GetComponent<Tilemap>();
        chunk.grass = chunk.go.transform.Find("Grass").GetComponent<Tilemap>();

        for (int lx = 0; lx < chunkSize; lx++)
        {
            for (int ly = 0; ly < chunkSize; ly++)
            {
                int gx = coord.x * chunkSize + lx;
                int gy = coord.y * chunkSize + ly;

                int bg;
                int tile = ProceduralGeneration.GenerateTileAt(gx, gy, out bg);
                Debug.Log(bg);
                if (tile != 4)
                {
                    chunk.tiles[lx, ly] = tile;
                    chunk.bgTiles[lx, ly] = bg;

                    Vector3Int pos = new Vector3Int(gx, gy, 0);
                    SetTileInMaps(tile, bg, pos, chunk);
                }
                else
                {
                    Vector3Int pos = new Vector3Int(gx, gy, 0);
                    SetTileInMaps(tile, bg, pos, chunk);
                    Debug.DrawLine(new Vector3(gx, gy, 0), new Vector3(gx + 0.1f, gy + 0.1f, 0), Color.red, 5f);
                    Debug.Log("Пещера!");
                }
                
            }

            // разгрузка — подождать один кадр после каждой строки
            if (lx % 4 == 0) yield return null;
        }

        loadedChunks[coord] = chunk;
    }

    void UnloadChunk(Vector2Int coord)
    {
        if (loadedChunks.TryGetValue(coord, out var chunk))
        {
            Destroy(chunk.go);
            loadedChunks.Remove(coord);
        }
    }

    void SetTileInMaps(int tileId, int bgTileId, Vector3Int worldPos, Chunk chunk)
    {
        // Удалить всё по-любому
        chunk.ground.SetTile(worldPos, null);
        chunk.bg.SetTile(worldPos, null);
        chunk.light.SetTile(worldPos, null);
        chunk.grass.SetTile(worldPos, null);


        // Отрисовать основной блок, если он не воздух и не пещера
        if (tileId != 0 && tileId != 4)
        {
            var tile = TileRegistry.GetTile(tileId);
            if (tile != null)
                chunk.ground.SetTile(worldPos, tile);
        }

        // Отрисовать задний фон
        if (bgTileId != 0)
        {
            if (bgTileId == 3)
            {
                Debug.DrawLine(new Vector3(worldPos.x, worldPos.y, 0), new Vector3(worldPos.x + 0.2f, worldPos.y + 0.1f, 0), Color.blue, 5f);
            }
            var bg = TileRegistry.GetTile(bgTileId);
            if (bg == null)
                Debug.LogWarning($"bgTileId {bgTileId} has no TileBase!");
            else
                chunk.bg.SetTile(worldPos, bg);
        }
    }
}

public class Chunk
{
    public Vector2Int coord;
    public int[,] tiles;
    public int[,] bgTiles;
    public GameObject go;
    public Tilemap ground, bg, light, grass;
}

public static class TileRegistry
{
    private static Dictionary<int, TileBase> tiles = new();

    public static void Init()
    {
        tiles[1] = Resources.Load<TileBase>("Tiles/Dirt");
        tiles[2] = Resources.Load<TileBase>("Tiles/Grass");
        tiles[3] = Resources.Load<TileBase>("Tiles/Stone");
        tiles[4] = null; // очень важно!
        tiles[6] = Resources.Load<TileBase>("Tiles/IronOre");
        tiles[7] = Resources.Load<TileBase>("Tiles/Teleportium");
        tiles[9] = Resources.Load<TileBase>("Tiles/Sand");
        tiles[10] = Resources.Load<TileBase>("Tiles/Crystal");
        tiles[11] = Resources.Load<TileBase>("Tiles/Snow");
        tiles[12] = Resources.Load<TileBase>("Tiles/Moss");
        tiles[17] = Resources.Load<TileBase>("Tiles/Coal");
        // Добавь остальные по аналогии
    }

    public static TileBase GetTile(int id)
    {

        if (BlocksData.allBlocks[id].imagePath != null)
        {
            UnityEngine.Tilemaps.Tile tile = new UnityEngine.Tilemaps.Tile();
            tile.sprite = (Sprite)Resources.Load(BlocksData.allBlocks[id].imagePath, typeof(Sprite));
            return tile;
        }
        else
        {
            return null;
        }
    }
}
