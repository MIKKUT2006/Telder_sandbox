using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
//using System;
//using Assets;
//using Client = Supabase.Client;
//using System.Threading.Tasks;

public class ProceduralGeneration : MonoBehaviour
{
    // �����
    public static int X, Y;
    [System.Serializable]
    public class Column
    {
        public bool[] rows = new bool[Y];
    }

    public Column[] columns = new Column[X];
    // �����

    // ������ ��� �����
    [SerializeField] public GameObject chunkPrefab;
    [SerializeField] public GameObject lightchunkPrefab;
    [SerializeField] public GameObject bgchunkPrefab;

    // ������ ������ � ���������
    [Header("��������� ��������")]
    [SerializeField] public Tilemap testStructure;
    [SerializeField] public Tilemap mapleHouse;

    [Header("������� ����")]
    [SerializeField] public int height = 100;       // ������ (����)
    [SerializeField] public int width = 200;        // ������ (����)

    [Header("�������� �����, �����, �����")]
    [SerializeField] float smoothes;                // ��������
    [SerializeField] float cavessmothes;            // �������� �����
    [SerializeField] float stonesmothes;            // �������� �����
    [SerializeField] float biomeSmoothes;           // �������� ����� �������

    [Header ("�������� ��������� ���")]
    [SerializeField] float ironOre;                 // �������� �������� ����
    [SerializeField] float coalOre;                 // �������� �������� ����
    [SerializeField] float teleportiumOre;          // �������� ����� ������������

    [SerializeField] float seed;                    // ��� ����
    [SerializeField] List<TileBase> groundTile;     // ����
    [SerializeField] TileBase lightTile;            // ���� ���������
    [SerializeField] Tilemap tilemap;               // ����� ������
    [SerializeField] Tilemap bgTilemap;             // ����� ������ ������� ����
    [SerializeField] Tilemap lightTilemap;          // ����� ������ ��� ���������

    [SerializeField] Cell cell;

    [SerializeField] public static int[,] map;      // ��������� ������ �����
    [SerializeField] public static int[,] bgMap;    // ��������� ������ ����� ������� �����

    //private enum Biomes { Desert, Forest, Crystal, None }
    //private Biomes[] biomeMap;


    [SerializeField] GameObject mainTilemap;
    [SerializeField] Tilemap testhouse;

    [SerializeField] List<GameObject> Trees;

    // 0 = ��������� ����
    // 1 = �����
    // 2 = �����
    // 3 = ������
    // 4 = �������
    // 5 = ����� � ���������
    // 1 = �����
    // 1 = �����
    // 1 = �����

    // ��� �������� ������ ���� �������
    public int x = 0, y = 0;
    public int worldSeed;

    //public async Task<List<User>> GetUsers()
    //{
    //    var options = new Supabase.SupabaseOptions
    //    {
    //        AutoConnectRealtime = true
    //    };
    //    var supabase = new Supabase.Client(HelperClass.url, HelperClass.key, options);
    //    await supabase.InitializeAsync();

    //    //var result = await supabase.From<User>().
        
    //    //List<Assets.User> allUsers = result.Models;

    //    //return allUsers;
    //}

    void Awake()
    {
        HelperClass.worldWidth = width;
        HelperClass.worldHeight = height;
        HelperClass.Cells = new Cell[HelperClass.worldWidth, HelperClass.worldWidth];

        HelperClass.biomeMap = new HelperClass.Biomes[width];

        cell = new Cell();

        // ������ �����
        CreateChunks();         
        // ��������� �� �������� ������ ���� ��� ��������� �������������
        if (HelperClass.isNewGame == true)
        {
            Generation();

            HelperClass.worldHeight = height;
            HelperClass.worldWidth = width;
        }
        else
        {
            map = HelperClass.map;
            bgMap = HelperClass.bgMap;

            height = HelperClass.worldHeight;
            width = HelperClass.worldWidth;
        }

        RenderMap(map, tilemap, groundTile, bgMap);             // ���������� ���������
        LightGeneraion(map);

        Grid.CreateGrid();

        HelperClass.chunkPrefab = chunkPrefab;
        HelperClass.lightchunkPrefab = lightchunkPrefab;
        HelperClass.bgchunkPrefab = bgchunkPrefab;

        Debug.Log(Mathf.PerlinNoise((x + worldSeed) / cavessmothes, (y + worldSeed) / cavessmothes));
    }
    void Generation()
    {
        bgMap = GenerateArray(width, height, true, true);       // ���������� ������
        map = GenerateArray(width, height, true, false);        // ���������� ������


        //lightMap = GenerateArray(width, height, true, true);    // ���������� ������
        lightTilemap.ClearAllTiles();                           // ������� ��� ����� ����� ����������

        // �������� ����
        tilemap.ClearAllTiles();                                // ������� ��� ����� ����� ����������
        BiomeGeneration();                                      // ���������� �����
        map = TerrainGeneration(map);                           // ���������� ���
        map = StoneGeneration(map);                             // ���������� ������
        map = CavesGeneration(map, bgMap).Item1;                // ���������� ������
        map = BarrierGeneration(map);
        DestroyStructures();
        // ������� ����
        bgTilemap.ClearAllTiles();                              // ������� ��� ����� ����� ����������
        
        bgMap = TerrainGeneration(bgMap);                       // ���������� ���
        bgMap = StoneGeneration(bgMap);                         // ���������� ���
        bgMap = GrassGeneration(bgMap);                         // ���������� ���
        bgMap = CavesGeneration(map, bgMap).Item2;              // ���������� ������

        map = TreesGeneration(map);
        //StructuresGeneration(testStructure);
        StructuresGeneration(testStructure, 12);
        StructuresGeneration(mapleHouse, 2);
        Debug.Log("�� ������");
    }

    //public void CreateChunks()                                  // �������� ������
    //{
    //    HelperClass.numChunks = width / HelperClass.chunkSize;  // ������������� ���������� ������

    //    HelperClass.Chunks = new Tilemap[HelperClass.numChunks];
    //    HelperClass.ChunksGameobject = new GameObject[HelperClass.numChunks];

    //    HelperClass.lightChunks = new Tilemap[HelperClass.numChunks];
    //    HelperClass.lightChunksGameobject = new GameObject[HelperClass.numChunks];

    //    HelperClass.bgChunks = new Tilemap[HelperClass.numChunks];
    //    HelperClass.bgChunksGameobject = new GameObject[HelperClass.numChunks];

    //    // ���� �� ���������� ������
    //    for (int i = 0; i < HelperClass.numChunks; i++)
    //    {
    //        // ��������� ������ � ������� ������� 1- � x+1 �� ����� �������

    //        //------------------
    //        Tilemap newChunk = new Tilemap();
    //        HelperClass.Chunks[i] = newChunk;
    //        GameObject Chunk = Instantiate(chunkPrefab);
    //        Chunk.name = i.ToString();
    //        Chunk.transform.parent = transform;
    //        HelperClass.ChunksGameobject[i] = Chunk;

    //        Tilemap newlightChunk = new Tilemap();
    //        HelperClass.lightChunks[i] = newlightChunk;
    //        GameObject lightChunk = Instantiate(lightchunkPrefab);
    //        lightChunk.name = i.ToString();
    //        lightChunk.transform.parent = transform;
    //        HelperClass.lightChunksGameobject[i] = lightChunk;

    //        GameObject bgChunk = Instantiate(bgchunkPrefab);
    //        HelperClass.bgChunks[i] = bgChunk.GetComponent<Tilemap>();
    //        bgChunk.name = i.ToString();
    //        bgChunk.transform.parent = transform;
    //        HelperClass.bgChunksGameobject[i] = bgChunk;
    //        //------------------
    //    }
    //}

    public void CreateChunks()
    {
        int chunkSize = HelperClass.chunkSize;
        int numChunksX = Mathf.CeilToInt((float)width / chunkSize); // ���������� ������ �� X

        HelperClass.Chunks = new Tilemap[numChunksX];
        HelperClass.ChunksGameobject = new GameObject[numChunksX];
        HelperClass.lightChunks = new Tilemap[numChunksX];
        HelperClass.lightChunksGameobject = new GameObject[numChunksX];
        HelperClass.bgChunks = new Tilemap[numChunksX];
        HelperClass.bgChunksGameobject = new GameObject[numChunksX];

        for (int x = 0; x < numChunksX; x++)
        {
            int index = x; // ������ �����

            //------------------
            GameObject Chunk = Instantiate(chunkPrefab);
            Chunk.name = $"Chunk_{x}";
            Chunk.transform.parent = transform;
            HelperClass.ChunksGameobject[index] = Chunk;
            HelperClass.Chunks[index] = Chunk.GetComponent<Tilemap>();

            GameObject lightChunk = Instantiate(lightchunkPrefab);
            lightChunk.name = $"LightChunk_{x}";
            lightChunk.transform.parent = transform;
            HelperClass.lightChunksGameobject[index] = lightChunk;
            HelperClass.lightChunks[index] = lightChunk.GetComponent<Tilemap>();

            GameObject bgChunk = Instantiate(bgchunkPrefab);
            bgChunk.name = $"BgChunk_{x}";
            bgChunk.transform.parent = transform;
            HelperClass.bgChunksGameobject[index] = bgChunk;
            HelperClass.bgChunks[index] = bgChunk.GetComponent<Tilemap>();
            //------------------
        }
    }

    void DestroyStructures()
    {
        List<GameObject> trees = GameObject.FindGameObjectsWithTag("tree").ToList();

        foreach (var item in trees)
        {
            Destroy(item);
        }
    }

    // ������ ������ ��������� ����, ��������, ��� � ���� ���� ����� (������� ��� ������ �������� �����)
    // ����� �� ��������� ������� ���� ������
    public int[,] GenerateArray(int width, int height, bool useArray, bool bg)
    {
        int[,] map = new int[width, height];                // ������������� ������� ����
        int[,] bgMap = new int[width, height];              // ������������� ������� ����

        // ���������� ��� �� ������
        for (int i = 0; i < width; i++)
        {
            // ���������� ��� �� ������
            for (int j = 0; j < height; j++)
            {
                map[i, j] = (useArray) ? 0 : 1;             // ���� � ������� ����� false = 0, true = 1
                bgMap[i, j] = (useArray) ? 0 : 1;
            }
        }
        if (bg == false)
        {
            return map;
        }
        else
        {
            return bgMap;
        }
    }
    //-----
    // ��� ������� ���������: �� �������� �� ������� ���� ������� ������, �� ������� ������������ �����, ��, ��� ���� = 0
    // ��� ������������� ���� �������, ��������� � ����������
    //-----

    public int[,] BarrierGeneration(int[,] map)
    {
        //for (int x = 0; x < HelperClass.worldWidth; x++)
        //{
        //    for (int y = 0; y < HelperClass.worldHeight; y++)
        //    {
        //        // Add border
        //        if (x == 0 || y == 0 || x == HelperClass.worldWidth - 1 || y == HelperClass.worldHeight - 1)
        //        {
        //            map[x, y] = 8;
        //        }
        //    }
        //}
        return map;
    }

    //public void BiomeGeneration()
    //{
    //    float biomeNoiseScale = 0.005f; // ������� ���� ��� ������

    //    // ������������� ������
    //    for (int x = 0; x < width; x++)
    //    {
    //        // ��������� ����� ��� �������� �������
    //        float biomeValue = Mathf.PerlinNoise(x * biomeNoiseScale, 0);
    //        Debug.Log(biomeValue);
    //        if (biomeValue < 0.44f)
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Desert; // �������
    //        }
    //        else if (biomeValue < 0.66f)
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Forest; // ���
    //        }
    //        else
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Crystal; // ��������
    //        }
    //    }
    //}
    // ��������� ������
    private int biomeWidth = HelperClass.worldWidth; // ������ ����� ������
    
    //public HelperClass.Biomes[] test;

    [System.Serializable]
    public class BiomeRange
    {
        public string biomeName;
        public float minThreshold;
        public float maxThreshold;

        public BiomeRange(string name, float min, float max)
        {
            biomeName = name;
            minThreshold = min;
            maxThreshold = max;
        }
    }

    public List<BiomeRange> biomeRanges = new List<BiomeRange>();

    public void BiomeGeneration()
    {
        biomeWidth = width; // ������ ����� ������
        float biomeNoiseScale = 0.005f; // ������� ���� ��� ������

        // �������� �� ������������ �������� ������
        biomeRanges.Sort((a, b) => a.minThreshold.CompareTo(b.minThreshold)); // ��������� �� ������������ ������
        for (int i = 0; i < biomeRanges.Count - 1; i++)
        {
            if (biomeRanges[i].maxThreshold > biomeRanges[i + 1].minThreshold)
            {
                Debug.LogError("������ � ���������� ������: ����������� �������!");
                return;
            }
        }


        HelperClass.biomeMap = new HelperClass.Biomes[biomeWidth]; // �������������� ������

        // ������������� ������
        for (int x = 0; x < biomeWidth; x++)
        {
            float biomeValue = Mathf.PerlinNoise(x * biomeNoiseScale, 0);

            // ������� ����, ��������������� �������� ����
            HelperClass.Biomes biome = FindBiome(biomeValue);
            if (biome == HelperClass.Biomes.Crystal)
            {
                Debug.Log("��");
            }
            HelperClass.biomeMap[x] = biome;
        }

        //test = HelperClass.biomeMap;
    }

    HelperClass.Biomes FindBiome(float value)
    {
        foreach (var range in biomeRanges)
        {
            if (value >= range.minThreshold && value < range.maxThreshold)
            {
                return (HelperClass.Biomes)System.Enum.Parse(typeof(HelperClass.Biomes), range.biomeName);
            }
        }
        // ���� �������� �� �������� �� � ���� ��������, ������� ���� �� ��������� (��� ���������� ������)
        Debug.LogError("�������� ���� �� ������������� �� ������ �����: " + value);
        return HelperClass.Biomes.Desert; // ���� �� ���������
    }
    // ��������� ������


    public int[,] TerrainGeneration(int[,] map)     // ��������� �����
    {
        int perlinHeight;           // ������ �������
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed + height) * height / 2.5f);
            perlinHeight += height / 2;
            for (int j = 0; j <= perlinHeight + 1; j++)
            {
                if (j < perlinHeight)
                {
                    switch (HelperClass.biomeMap[i])
                    {
                        case HelperClass.Biomes.Forest:
                            map[i, j] = 1;
                            break;
                        case HelperClass.Biomes.Desert:
                            map[i, j] = 9;
                            break;
                        case HelperClass.Biomes.Crystal:
                            map[i, j] = 10;
                            break;
                        case HelperClass.Biomes.Snow:
                            map[i, j] = 1;
                            break;
                    }
                }

                if (j == perlinHeight)
                {
                    switch (HelperClass.biomeMap[i])
                    {
                        case HelperClass.Biomes.Forest:
                            map[i, j] = 2;
                            break;
                        case HelperClass.Biomes.Desert:
                            map[i, j] = 9;
                            break;
                        case HelperClass.Biomes.Crystal:
                            map[i, j] = 10;
                            break;
                        case HelperClass.Biomes.Snow:
                            map[i, j] = 11;
                            break;
                    }
                }
            }
        }

        //// �������������� ����
        for (int i = 0; i < width; i++)
        {
            // �������� ���������� �����
            int chunkCoord = i / HelperClass.chunkSize;   // �������� ���������� �����

            int ostatok = chunkCoord % 100;
            if (ostatok != 0)
            {
                chunkCoord -= (chunkCoord - ostatok) + 1;
            }
            Tilemap lighttilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            //perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed / 2) * height / 2);
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 4, HelperClass.worldSeed + height) * height / 2f);
            //Debug.Log(HelperClass.worldSeed);
            perlinHeight += height / 2;
            //perlinHeight = perlinHeight / 2;


            for (int j = 0; j <= perlinHeight + 1; j++)
            {
                if (j < perlinHeight)
                {
                    switch (HelperClass.biomeMap[i])
                    {
                        case HelperClass.Biomes.Forest:
                            map[i, j] = 1;
                            break;
                        case HelperClass.Biomes.Desert:
                            map[i, j] = 9;
                            break;
                        case HelperClass.Biomes.Crystal:
                            map[i, j] = 10;
                            break;
                        case HelperClass.Biomes.Snow:
                            map[i, j] = 1;
                            break;
                    }
                }

                if (j == perlinHeight && map[i, j + 1] < 1)
                {
                    map[i, j] = 2;
                }

                if (j > perlinHeight && map[i, j + 1] < 1)
                {
                    map[i, j] = 0;
                }
            }
        }
        return map;
    }

    public int[,] StoneGeneration(int[,] map)     // ��������� ��������� ����
    {
        int perlinHeight;   // ������ �������
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
            perlinHeight += height / 3;

            for (int j = 0; j <= perlinHeight; j++)
            {

                if (j < perlinHeight && (map[i,j] == 1 || map[i, j] != 2))
                {
                    switch (HelperClass.biomeMap[i])
                    {
                        case HelperClass.Biomes.Forest:
                            map[i, j] = 3;
                            break;
                        case HelperClass.Biomes.Desert:
                            map[i, j] = 9;
                            break;
                        case HelperClass.Biomes.Crystal:
                            map[i, j] = 10;
                            break;
                        case HelperClass.Biomes.Snow:
                            map[i, j] = 3;
                            break;
                    }
                }
            }
        }
        return map;
    }

    public (int[,], int[,]) CavesGeneration(int[,] map, int[,] bgMap)     // ��������� �����
    {
        int perlinHeightStone;   // ������ �������
        float perlinHeightCaves;   // ������ �������
        float perlinHeightGround;   // ������ ������� ������������� �����
        float perlinHeightOres;   // ������ ������� ���
        float perlinHeightTeleportium;
        float cavesSeed = Random.Range(0, 100);

        Debug.Log(bgMap);
        for (int i = 0; i < width; i++)
        {
            perlinHeightStone = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
            perlinHeightStone += height / 3;

            for (int j = 0; j < height; j++)
            {
                // ������
                perlinHeightCaves = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
                if (perlinHeightCaves < 0.4 && (map[i, j] == 3 || map[i, j] == 9 || map[i, j] == 10))
                {
                    map[i, j] = 4;
                }

                // ������ �� �����������
                perlinHeightGround = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes / 2, (j + HelperClass.worldSeed) / cavessmothes / 2);
                if (perlinHeightGround < 0.4 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
                {
                    map[i, j] = 4;
                }

                // �������� ������
                perlinHeightGround = Mathf.PerlinNoise((i + cavesSeed) / 12, (j + cavesSeed) / 10);
                if (perlinHeightGround > 0.15 && perlinHeightGround < 0.2 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
                {
                    map[i, j] = 12;
                }
                if (perlinHeightGround < 0.2 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
                {
                    map[i, j] = 4;
                    bgMap[i, j] = 12;
                }

                // ��������� �������� ����
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 2) / ironOre, (j + HelperClass.worldSeed / 2) / ironOre);
                //Debug.Log(perlinHeightOres);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 6;
                }

                // ��������� �������� ����
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 4) / coalOre, (j + HelperClass.worldSeed / 4) / coalOre);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 17;
                }

                // ��������� ���� ������������
                perlinHeightTeleportium = Mathf.PerlinNoise((i + HelperClass.worldSeed) / teleportiumOre / 0.9f, (j + HelperClass.worldSeed) / teleportiumOre / 0.9f);

                //perlinHeightTeleportium = Mathf.RoundToInt(Mathf.PerlinNoise(j / stonesmothes, seed * 3) * height / 2.1f);
                //Debug.Log(perlinHeightTeleportium);
                if (perlinHeightTeleportium > 0.87 && HelperClass.biomeMap[i] == HelperClass.Biomes.Crystal && map[i, j] == 10)
                {
                    map[i, j] = 7;
                }
            }
        }
        return (map, bgMap);
    }

    public int[,] TreesGeneration(int[,] map)     // ��������� ��������
    {
        float perlinHeight;   // ������ �������

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
                //Debug.Log(perlinHeight);

                if (perlinHeight < 0.4 && map[i, j] == 2)
                {
                    map[i, j] = 5;
                }
            }
        }
        return map;
    }

    public int[,] GrassGeneration(int[,] map)     // ��������� �������� �����
    {
        float perlinHeight;   // ������ �������

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);

                if (perlinHeight < 0.5 && map[i, j] == 2)
                {
                    bgMap[i, j + 1] = 8;
                }
            }
        }
        return map;
    }

    public int[,] LightGeneraion(int[,] map)
    {
        //int perlinHeight;   // ������ �������
        for (int i = 0; i < width; i++)
        {
            // �������� ���������� �����
            int chunkCoord = i / HelperClass.chunkSize;   // �������� ���������� �����
            //chunkCoord = chunkCoord * chunkSize;

            int ostatok = chunkCoord % 100;
            if (ostatok != 0)
            {
                chunkCoord -= (chunkCoord - ostatok) + 1;
            }
            Tilemap lighttilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            //perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes, seed) * height / 2);
            //perlinHeight += height / 2;

            for (int j = height-1; j > 0; j--)
            {
                //Debug.Log(i + ":" + j);
                if (map[i, j] != 0 && map[i, j] != 4)
                {
                    //Debug.Log(map[i, j]);
                    //map[i, j] = 0;
                    lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTile);

                    HelperClass.lightChunks[chunkCoord] = tilemap;
                    break;
                }

                if (map[i, j] == 4)
                {
                    //Debug.Log(map[i, j]);
                    //map[i, j] = 0;
                    lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTile);

                    HelperClass.lightChunks[chunkCoord] = tilemap;
                }
            }
            //for (int g = perlinHeight; g < height; g++)
            //{
            //    lighttilemap.SetTile(new Vector3Int(i, g, 0), lightTile);
            //}
            //lightChunks[chunkCoord] = tilemap;
        }
        return map;
    }

    public void StructuresGeneration(Tilemap structureTilemap, int structureCount)
    {
        
        for (int i = 0; i < structureCount; i++)
        {
            BoundsInt bounds = structureTilemap.cellBounds;
            int structureCoordX = (int)UnityEngine.Random.RandomRange(0, width);
            int structureCoordY = (int)UnityEngine.Random.RandomRange(0, 100);
            if (map[structureCoordX, structureCoordY] != 0)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        Vector3Int tilePos = new Vector3Int(x, y, 0);
                        TileBase tile = structureTilemap.GetTile(tilePos);

                        if (tile != null)
                        {
                            //Debug.Log("Tile at position " + tilePos + " is " + tile.name);
                            tilePos.x += structureCoordX;
                            tilePos.y += structureCoordY;
                            //map[x, y] = 3;
                            int chunkCoord = tilePos.x / HelperClass.chunkSize;

                            int ostatok = chunkCoord % 100;
                            if (ostatok != 0)
                            {
                                chunkCoord -= (chunkCoord - ostatok) + 1;
                            }

                            Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                            //Debug.Log(HelperClass.Cells.GetLength(0));
                            //HelperClass.Cells[x, y].SetType(CellType.Solid);
                            tilemap.SetTile(tilePos, tile);
                        }
                    }
                }
            }
            
        }
    }

    public static class ChunkHelper
    {
        public static int GetChunkXCoordinate(int x)
        {
            int chunkSize = HelperClass.chunkSize;
            return Mathf.FloorToInt(x / (float)chunkSize);
        }
        public static int GetChunkYCoordinate(int y)
        {
            int chunkSize = HelperClass.chunkSize;
            return Mathf.FloorToInt(y / (float)chunkSize);
        }

        //public static int GetChunkCountX()
        //{
        //    //��� ����� ��������� ���������� ������ �� X, ������ �� ������� ���� � ������� �����.
        //}

        //public static int GetChunkCountY()
        //{
        //    //��� ����� ��������� ���������� ������ �� Y, ������ �� ������� ���� � ������� �����.
        //}
    }

    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // ������, �������, ����� ����� (������ ������)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int chunkSize = HelperClass.chunkSize;

        for (int i = 0; i < width; i++)
        {
            // �������� ���������� �����
            //int chunkCoord = i / HelperClass.chunkSize;   // �������� ���������� �����
            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);

            //int ostatok = chunkCoord % 100;
            //if (ostatok != 0)
            //{
            //    chunkCoord -= (chunkCoord - ostatok) + 1;
            //}

            //int chunkCoord = ChunkHelper.GetChunkXCoordinate(x);

            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap bgTileMap = HelperClass.bgChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            for (int j = 0; j < height; j++)
            {
                if (map[i, j] == 0)
                {
                    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTile);
                }

                switch (map[i, j])
                {
                    //case 0:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // ������������� ���� �����
                    //    break;
                    case 1:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
                        break;
                    case 2:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // ������������� ���� �����

                        break;
                    case 3:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                        break;
                    //case 4:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // ������������� ���� �����
                    //    break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 5, 0);
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // ������������� �������

                        Instantiate(Trees[(int)UnityEngine.Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
                        break;
                    case 6:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // ������������� ���� �������� ����
                        break;
                    case 7:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[5]);       // ������������� ���� ���� ����� ������������
                        break;
                    case 8:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[7]);       // ������������� ���� �������
                        break;
                    case 9:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // ������������� ���� �����
                        break;
                    case 10:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // ������������� ���� �����
                        break;
                    case 11:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[10]);       // ������������� ���� �����
                        break;
                    case 12:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // ������������� ���� ���
                        break;
                    case 17:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[12]);       // ������������� ���� ����
                        break;
                }

                HelperClass.Chunks[chunkCoord] = tileMap;

                if (bgMap[i, j] == 1)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 2)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 3)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 8)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // ������������� ���� �������� �����
                }
                if (bgMap[i, j] == 9)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 10)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 12)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // ������������� ���� ���
                }


                //if (bgMap[i, j] == 8)
                //{
                //    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // ������������� ���� �����
                //}
            }
        }
    }
}
