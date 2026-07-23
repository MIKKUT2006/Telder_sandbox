//using System.Collections;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//using UnityEngine.Tilemaps;
//using UnityEngine.WSA;
//using static UnityEditor.PlayerSettings;

//public class ChunkManager : MonoBehaviour
//{
//    public int chunkSize = 16;
//    public int chunkBuffer = 1;
//    public GameObject chunkPrefab;

//    private Dictionary<Vector2Int, Chunk> loadedChunks = new();
//    private HashSet<Vector2Int> generatingChunks = new();
//    private Queue<Chunk> chunkPool = new();
//    private ConcurrentQueue<ChunkData> readyChunks = new();

//    private Camera cam;
//    private float updateTimer = 0f;

//    void Start()
//    {
//        cam = Camera.main;
//        TileRegistry.Init();
//    }

//    void Update()
//    {
//        updateTimer += Time.deltaTime;
//        if (updateTimer > 0.2f)
//        {
//            updateTimer = 0f;
//            UpdateChunks();
//        }

//        // Отрисовать готовые чанки
//        while (readyChunks.TryDequeue(out var data))
//        {
//            DrawChunk(data);
//        }
//    }

//    void UpdateChunks()
//    {
//        var visibleChunks = GetVisibleChunks();

//        foreach (var coord in visibleChunks)
//        {
//            if (!loadedChunks.ContainsKey(coord) && !generatingChunks.Contains(coord))
//            {
//                generatingChunks.Add(coord);
//                ThreadPool.QueueUserWorkItem(_ => GenerateChunkThread(coord));
//            }
//        }

//        var toRemove = new List<Vector2Int>();
//        foreach (var c in loadedChunks.Keys)
//        {
//            if (!visibleChunks.Contains(c))
//                toRemove.Add(c);
//        }

//        foreach (var c in toRemove)
//            UnloadChunk(c);
//    }

//    HashSet<Vector2Int> GetVisibleChunks()
//    {
//        Bounds bounds = GetCameraBounds();
//        Vector2Int min = WorldToChunk(bounds.min);
//        Vector2Int max = WorldToChunk(bounds.max);

//        HashSet<Vector2Int> result = new();
//        for (int x = min.x - chunkBuffer; x <= max.x + chunkBuffer; x++)
//        {
//            for (int y = min.y - chunkBuffer; y <= max.y + chunkBuffer; y++)
//                result.Add(new Vector2Int(x, y));
//        }
//        return result;
//    }

//    Bounds GetCameraBounds()
//    {
//        float h = cam.orthographicSize * 2;
//        float w = h * cam.aspect;
//        return new Bounds(cam.transform.position, new Vector3(w, h, 0));
//    }

//    Vector2Int WorldToChunk(Vector3 pos)
//    {
//        int cx = Mathf.FloorToInt(pos.x / chunkSize);
//        int cy = Mathf.FloorToInt(pos.y / chunkSize);
//        return new Vector2Int(cx, cy);
//    }

//    void GenerateChunkThread(Vector2Int coord)
//    {
//        int[,] tiles = new int[chunkSize, chunkSize];
//        int[,] bgTiles = new int[chunkSize, chunkSize];

//        for (int lx = 0; lx < chunkSize; lx++)
//        {
//            for (int ly = 0; ly < chunkSize; ly++)
//            {
//                int gx = coord.x * chunkSize + lx;
//                int gy = coord.y * chunkSize + ly;
//                tiles[lx, ly] = ProceduralGeneration.GenerateTileAt(gx, gy, out bgTiles[lx, ly]);
//            }
//        }

//        readyChunks.Enqueue(new ChunkData
//        {
//            coord = coord,
//            tiles = tiles,
//            bgTiles = bgTiles
//        });
//    }

//    void DrawChunk(ChunkData data)
//    {
//        Chunk chunk;

//        if (chunkPool.Count > 0)
//        {
//            chunk = chunkPool.Dequeue();
//            chunk.go.SetActive(true);
//        }
//        else
//        {
//            GameObject go = Instantiate(chunkPrefab, transform);
//            chunk = new Chunk();
//            chunk.go = go;
//            chunk.ground = go.transform.Find("Ground").GetComponent<Tilemap>();
//            chunk.bg = go.transform.Find("BG").GetComponent<Tilemap>();
//            chunk.light = go.transform.Find("Light").GetComponent<Tilemap>();
//            chunk.grass = go.transform.Find("Grass").GetComponent<Tilemap>();
//        }

//        chunk.coord = data.coord;
//        loadedChunks[data.coord] = chunk;
//        generatingChunks.Remove(data.coord);

//        chunk.ground.ClearAllTiles();
//        chunk.bg.ClearAllTiles();

//        for (int lx = 0; lx < chunkSize; lx++)
//        {
//            for (int ly = 0; ly < chunkSize; ly++)
//            {
//                int gx = data.coord.x * chunkSize + lx;
//                int gy = data.coord.y * chunkSize + ly;
//                Vector3Int pos = new Vector3Int(gx, gy, 0);

//                int tileId = data.tiles[lx, ly];
//                int bgTileId = data.bgTiles[lx, ly];

//                chunk.ground.SetTile(pos, (tileId > 0 && tileId != 4) ? TileRegistry.GetTile(tileId) : null);
//                chunk.bg.SetTile(pos, bgTileId > 0 ? TileRegistry.GetTile(bgTileId) : null);
//            }
//        }
//    }

//    void UnloadChunk(Vector2Int coord)
//    {
//        if (loadedChunks.TryGetValue(coord, out var chunk))
//        {
//            chunk.go.SetActive(false);
//            chunkPool.Enqueue(chunk);
//            loadedChunks.Remove(coord);
//        }
//    }

//    class ChunkData
//    {
//        public Vector2Int coord;
//        public int[,] tiles;
//        public int[,] bgTiles;
//    }

//    class Chunk
//    {
//        public Vector2Int coord;
//        public GameObject go;
//        public Tilemap ground, bg, light, grass;
//    }


//}


//public static class TileRegistry
//{
//    private static Dictionary<int, TileBase> tiles = new();

//    public static void Init()
//    {
//        tiles[1] = Resources.Load<TileBase>("Tiles/Dirt");
//        tiles[2] = Resources.Load<TileBase>("Tiles/Grass");
//        tiles[3] = Resources.Load<TileBase>("Tiles/Stone");
//        tiles[4] = null; // очень важно!
//        tiles[6] = Resources.Load<TileBase>("Tiles/IronOre");
//        tiles[7] = Resources.Load<TileBase>("Tiles/Teleportium");
//        tiles[9] = Resources.Load<TileBase>("Tiles/Sand");
//        tiles[10] = Resources.Load<TileBase>("Tiles/Crystal");
//        tiles[11] = Resources.Load<TileBase>("Tiles/Snow");
//        tiles[12] = Resources.Load<TileBase>("Tiles/Moss");
//        tiles[17] = Resources.Load<TileBase>("Tiles/Coal");
//        // Добавь остальные по аналогии
//    }

//    public static TileBase GetTile(int id)
//    {

//        if (BlocksData.allBlocks[id].imagePath != null)
//        {
//            UnityEngine.Tilemaps.Tile tile = new UnityEngine.Tilemaps.Tile();
//            tile.sprite = (Sprite)Resources.Load(BlocksData.allBlocks[id].imagePath, typeof(Sprite));
//            return tile;
//        }
//        else
//        {
//            return null;
//        }
//    }
//}

