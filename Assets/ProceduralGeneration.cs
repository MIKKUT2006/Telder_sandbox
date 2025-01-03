using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Unity.VisualScripting;
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
    [SerializeField] public GameObject grasschunkPrefab;

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

    [Header("�������� ��������� ���")]
    [SerializeField] float ironOre;                 // �������� �������� ����
    [SerializeField] float coalOre;                 // �������� �������� ����
    [SerializeField] float teleportiumOre;          // �������� ����� ������������

    [SerializeField] float seed;                    // ��� ����
    [SerializeField] List<TileBase> groundTile;     // ����
    [SerializeField] public static List<TileBase> lightTiles;            // ���� ���������
    [SerializeField] List<TileBase> lightTilesInspector;            // ���� ���������
    [SerializeField] Tilemap tilemap;               // ����� ������
    [SerializeField] Tilemap bgTilemap;             // ����� ������ ������� ����
    [SerializeField] Tilemap lightTilemap;          // ����� ������ ��� ���������
    [SerializeField] Tilemap grassTilemap;          // ����� ������ ��������������

    [SerializeField] Cell cell;

    [SerializeField] public static int[,] map;      // ��������� ������ �����
    [SerializeField] public static int[,] lightMap;      // ��������� ������ �����
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

    [Header("���������")]
    public static Texture2D worldTilesMap;
    public Texture2D worldTilesMapInspector;
    public Material lightShader;
    public float lightThreshold;
    public static float lightRadius = 7f;
    static List<Vector2Int> unlitBlocks = new List<Vector2Int>();
    void Awake()
    {
        HelperClass.worldWidth = width;
        HelperClass.worldHeight = height;

        
        lightTiles = lightTilesInspector;
        
        worldTilesMap = new Texture2D(width, width);// ������� �������� ���������
        //worldTilesMap.filterMode = FilterMode.Point;// ������������� ������ ���������
        lightShader.SetTexture("_shadowTexture", worldTilesMap);// ������������� �������� �� ������
        // ��������� �������� ������
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                worldTilesMap.SetPixel(x, y, Color.white);
            }
        }
        worldTilesMap.Apply();

        
        //HelperClass.Cells = new Cell[HelperClass.worldWidth, HelperClass.worldWidth];

        HelperClass.biomeMap = new HelperClass.Biomes[width];

        //cell = new Cell();

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
        GenerateLightMapTEST(map, width, height);

        //Grid.CreateGrid();

        

        HelperClass.chunkPrefab = chunkPrefab;
        HelperClass.lightchunkPrefab = lightchunkPrefab;
        HelperClass.bgchunkPrefab = bgchunkPrefab;
        HelperClass.grasschunkPrefab = grasschunkPrefab;

        worldTilesMapInspector = worldTilesMap;
    }
    void Generation()
    {
        // ���-�� �� ������
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (worldTilesMap.GetPixel(x, y) == Color.white)
                {
                    LightBlock(x, y, 1f, 0);
                }
            }
        }
        worldTilesMap.Apply();



        bgMap = GenerateArray(width, height);       // ���������� ������
        lightMap = GenerateArray(width, height);       // ���������� ������
        map = GenerateArray(width, height);        // ���������� ������

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

        worldTilesMap.Apply();

        Debug.Log("�� ������");
    }

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

        HelperClass.grassChunks = new Tilemap[numChunksX];
        HelperClass.grassChunksGameobject = new GameObject[numChunksX];

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

            GameObject grassChunk = Instantiate(grasschunkPrefab);
            grassChunk.name = $"BgChunk_{x}";
            grassChunk.transform.parent = transform;
            HelperClass.grassChunksGameobject[index] = grassChunk;
            HelperClass.grassChunks[index] = grassChunk.GetComponent<Tilemap>();
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
    public int[,] GenerateArray(int width, int height)
    {
        int[,] array = new int[width, height];                // ������������� ������� ����

        // ���������� ��� �� ������
        for (int i = 0; i < width; i++)
        {
            // ���������� ��� �� ������
            for (int j = 0; j < height; j++)
            {
                array[i, j] = 0;
            }
        }
        return array;
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

                if (j < perlinHeight && (map[i, j] == 1 || map[i, j] != 2))
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

            for (int j = height - 1; j > 0; j--)
            {
                //Debug.Log(i + ":" + j);
                if (map[i, j] != 0 && map[i, j] != 4)
                {
                    //Debug.Log(map[i, j]);
                    //map[i, j] = 0;
                    //lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);

                    //HelperClass.lightChunks[chunkCoord] = tilemap;
                    break;
                }

                if (map[i, j] == 4)
                {
                    //Debug.Log(map[i, j]);
                    //map[i, j] = 0;
                    //lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);

                    //HelperClass.lightChunks[chunkCoord] = tilemap;
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
            Tilemap grassTileMap = HelperClass.grassChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            for (int j = 0; j < height; j++)
            {
                //if (map[i, j] == 0)
                //{
                //    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
                //}

                //���������, �������� �� ������� ���� ���������� �����(int = 0)
                //if (map[i, j] == 0)
                //{
                //    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]); // ������ ������� ����
                //}

                //if (map[i, j] == 0 && map[i, j - 1] != 0)
                //{
                //    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[1]);
                //}
                //else
                //{
                //    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTile);
                //}

                switch (map[i, j])
                {
                    case 0:
                        //LightBlock(x, y, 1f, 0);
                        //worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
                        
                        break;
                    case 1:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
                        RemoveLightSource(x,y);
                        break;
                    case 2:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // ������������� ���� �����
                        RemoveLightSource(x, y);
                        break;
                    case 3:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                        RemoveLightSource(x, y);
                        break;
                    case 4:
                        //RemoveLightSource(x, y);
                        break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 5, 0);
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // ������������� �������
                        RemoveLightSource(x, y);
                        Instantiate(Trees[(int)UnityEngine.Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
                        break;
                    case 6:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // ������������� ���� �������� ����
                        RemoveLightSource(x, y);
                        break;
                    case 7:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[5]);       // ������������� ���� ���� ����� ������������
                        RemoveLightSource(x, y);
                        break;
                    case 8:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[7]);       // ������������� ���� �������
                        RemoveLightSource(x, y);
                        break;
                    case 9:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // ������������� ���� �����
                        RemoveLightSource(x, y);
                        break;
                    case 10:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // ������������� ���� �����
                        RemoveLightSource(x, y);
                        break;
                    case 11:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[10]);       // ������������� ���� �����
                        RemoveLightSource(x, y);
                        break;
                    case 12:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // ������������� ���� ���
                        RemoveLightSource(x, y);
                        break;
                    case 17:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[12]);       // ������������� ���� ����
                        RemoveLightSource(x, y);
                        break;
                }
                
                HelperClass.Chunks[chunkCoord] = tileMap;
                if (bgMap[i, j] == 0)
                {
                    worldTilesMap.SetPixel(i, j, Color.white);
                }
                if (bgMap[i, j] == 1)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // ������������� ���� �����
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
                if (bgMap[i, j] == 2)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // ������������� ���� �����
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
                if (bgMap[i, j] == 3)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
                if (bgMap[i, j] == 8)
                {
                    grassTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // ������������� ���� �������� �����
                }
                if (bgMap[i, j] == 9)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // ������������� ���� �����
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
                if (bgMap[i, j] == 10)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // ������������� ���� �����
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
                if (bgMap[i, j] == 12)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // ������������� ���� ���
                    worldTilesMap.SetPixel(i, j, Color.black);
                }
            }
        }

        // ��������� ���������
        for (int x = 0; x < width; x++) 
        {
            for (int y = 0; y < height; y++) 
            {
                if (map[x,y] == 0)
                {
                    LightBlock(x, y, 1f, 0);
                    worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
                }
            }
        }
        worldTilesMap.Apply();
    }
    //public void GenerateLightMap(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // ���������� ������� (������)
    //    {
    //        for (int i = 0; i < width; i++) // ���������� ������ (������)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            // ���� ���� - �������� �����, ������ ������� ���� � ��������� "�������"
    //            if (map[i, j] == 0)
    //            {
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //            }
    //            else
    //            {
    //                bool isShadow = FillLight(map, lightTileMap, i, j, width, height, 1, tileMap); // �������� � ������ ��������� 1
    //                if (isShadow)
    //                {
    //                    break;
    //                }
    //            }
    //        }
    //    }
    //}
    //private bool FillLight(int[,] map, Tilemap lightTileMap, int x, int y, int width, int height, int lightLevel, Tilemap tileMap)
    //{
    //    // �������� ������
    //    if (x < 0 || x >= width || y < 0 || y >= height) return false;

    //    // ���� ��� ���� ��� �������� �����, �� ���������������
    //    //if (map[x, y] == 0) return;
    //    if (tileMap.GetTile(new Vector3Int(x, y)) == null)
    //    {
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTiles[0]);
    //        return false;
    //    }
    //    // �������� ���� �� ������ �����
    //    TileBase lightTile = null;
    //    if (lightLevel == 1) lightTile = lightTiles[1];
    //    else if (lightLevel == 2)
    //    {
    //        lightTile = lightTiles[2];
    //    }
    //    else if (lightLevel > 2)
    //    {
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // ���� ���� ������ 3 ������, �� ������� ���
    //        return true;
    //    }

    //    // ���� ���� ������, ������ ���
    //    if (lightTile != null)
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTile);


    //    // ���������� �������������� ���� ���� (��� ������ ������, ������ ���� **������** ���� ���� ����� ������).
    //    FillLight(map, lightTileMap, x, y - 1, width, height, lightLevel + 1, tileMap);
    //    return false;
    //}

    //public void GenerateLightMap(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // ���������� ������� (������)
    //    {
    //        for (int i = 0; i < width; i++) // ���������� ������ (������)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

    //            // ���� ���� - �������� �����, ������ ������� ���� � ��������� "�������"
    //            if (map[i, j] == 0)
    //            {
    //                Debug.Log("GOIDA");
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //                FillLight(tileMap, lightTileMap, i, j, width, height, 1);
    //            }
    //        }
    //    }
    //}
    public void GenerateLightMap(int[,] map, int width, int height)
    {
        for (int j = 0; j < height; j++) // ���������� ������� (������)
        {
            for (int i = 0; i < width; i++) // ���������� ������ (������)
            {
                int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
                Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

                // ���� ���� - �������� �����, ������ ������� ���� � ��������� "�������"
                if (map[i, j] == 0)
                {
                    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
                    FillLight(tileMap, lightTileMap, i, j, width, height);
                }
            }
        }
    }

    //public void GenerateLightMapTEST(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // ���������� ������� (������)
    //    {
    //        for (int i = 0; i < width; i++) // ���������� ������ (������)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

    //            if (tileMap.GetTile(new Vector3Int(i, j, 0)) == null)
    //            {
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //            }
    //            else if (j + 1 < height)
    //            {
    //                if (tileMap.GetTile(new Vector3Int(i + 1, j , 0)) == null)
    //                {
    //                    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[1]);
                        
    //                }else
    //                if (tileMap.GetTile(new Vector3Int(i + 2, j, 0)) == null)
    //                {
    //                    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[2]);
    //                    break;
    //                }
    //            }
    //            else
    //            {
    //                Debug.Log("�������� ������ ������");
    //            }
                
    //        }
    //    }
    //}

    public void GenerateLightMapTEST(int[,] map, int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = height - 1; j > 0; j--)
            {
                int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
                Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
                // ������������� ���� �����
                Debug.Log($"{i}S{j}");
                lightMap[i, j] = 1;
                if (tileMap.GetTile(new Vector3Int(i, j, 0)) == null)
                {

                    lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
                    if (j + 1 < height)
                    {

                        if (tileMap.GetTile(new Vector3Int(i, j - 1, 0)) != null)
                        {
                            lightTileMap.SetTile(new Vector3Int(i, j - 1, 0), lightTiles[1]);
                            if (tileMap.GetTile(new Vector3Int(i, j - 2, 0)) != null)
                            {
                                lightTileMap.SetTile(new Vector3Int(i, j - 2, 0), lightTiles[2]);
                                if (tileMap.GetTile(new Vector3Int(i, j - 3, 0)) != null)
                                {
                                    lightTileMap.SetTile(new Vector3Int(i, j - 3, 0), lightTiles[3]);
                                    if (tileMap.GetTile(new Vector3Int(i, j - 4, 0)) != null)
                                    {
                                        lightTileMap.SetTile(new Vector3Int(i, j - 4, 0), lightTiles[4]);
                                        break;
                                    }
                                    break;
                                }
                                break;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    //private void FillLight(Tilemap tileMap, Tilemap lightTileMap, int x, int y, int width, int height, int lightLevel)
    //{
    //    // �������� ������
    //    if (x < 0 || x >= width || y < 0 || y >= height) return;


    //    // �������� ���� �� ������ �����
    //    TileBase lightTile = null;
    //    if (lightLevel == 1) lightTile = lightTiles[0];
    //    else if (lightLevel == 2 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[1];
    //    else if (lightLevel == 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[2];
    //    else if (lightLevel > 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null)
    //    {
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // ���� ���� ������ 3 ������, �� ������� ���
    //        return;
    //    }

    //    // �������� �� ������� ����� � TileBlocks
    //    if (tileMap.GetTile(new Vector3Int(x, y, 0)) == null)
    //    {
    //        if (lightTile != null)
    //            lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTile);

    //        // ���������� �������������� ���� �� ��� �������
    //        FillLight(tileMap, lightTileMap, x, y - 1, width, height, lightLevel + 1);
    //        FillLight(tileMap, lightTileMap, x, y + 1, width, height, lightLevel + 1);
    //        FillLight(tileMap, lightTileMap, x - 1, y, width, height, lightLevel + 1);
    //        FillLight(tileMap, lightTileMap, x + 1, y, width, height, lightLevel + 1);
    //    }
    //}

    public static void LightBlock(int x, int y, float intensity, int iteration)
    {
        if (iteration < lightRadius)
        {
            worldTilesMap.SetPixel(x, y, Color.white * intensity);

            for (int nx = x - 1; nx < x + 2; nx++) 
            {
                for (int ny = y-1; ny < y + 2; ny++) 
                {
                    if (nx != x || ny != y)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                        float targetIntensity = Mathf.Pow(0.7f, dist) * intensity;

                        if (worldTilesMap.GetPixel(nx,ny) != null)
                        {
                            if (worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                            {
                                LightBlock(nx, ny, targetIntensity, iteration + 1);   
                            }
                        }
                    }
                }
            }
            worldTilesMap.Apply();
        }
    }

    public static void RemoveLightSource(int x, int y)
    {
        unlitBlocks.Clear();
        UnLightBlock(x, y, x, y);

        List<Vector2Int> toRelight = new List<Vector2Int>();

        foreach (Vector2Int block in unlitBlocks)
        {
            for (int nx = block.x - 1; nx < block.x + 2; nx++)
            {
                for (int ny = block.y - 1; ny < block.y + 2; ny++)
                {
                    if (worldTilesMap.GetPixel(nx, ny) != null)
                    {
                        if (worldTilesMap.GetPixel(nx,ny).r > worldTilesMap.GetPixel(block.x, block.y).r)
                        {
                            if (!toRelight.Contains(new Vector2Int(nx, ny)))
                            {
                                toRelight.Add(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }
        }

        foreach (Vector2Int source in toRelight)
        {
            LightBlock(source.x, source.y, worldTilesMap.GetPixel(source.x, source.y).r, 0);
        }

        worldTilesMap.Apply();
    }

    public static void UnLightBlock(int x, int y, int ix, int iy)
    {
        if (Mathf.Abs(x - ix) >= lightRadius || Mathf.Abs(y - iy) >= lightRadius || unlitBlocks.Contains(new Vector2Int(x, y)))
        {
            return;
        }

        for (int nx = x-1; nx < x + 2; nx++)
        {
            for (int ny = y-1; ny < y + 2; ny++) 
            {
                if (nx != x || ny != y)
                {
                    if (worldTilesMap.GetPixel(nx, ny) != null)
                    {
                        if (worldTilesMap.GetPixel(nx,ny).r < worldTilesMap.GetPixel(x,y).r)
                        {
                            UnLightBlock(nx, ny, ix, iy);
                        }
                    }
                }
            }
        }
        worldTilesMap.SetPixel(x, y, Color.black);
        unlitBlocks.Add(new Vector2Int(x, y));
    }

    private void FillLight(Tilemap tileMap, Tilemap lightTileMap, int startX, int startY, int width, int height)
    {
        Queue<(int x, int y, int lightLevel)> queue = new Queue<(int x, int y, int lightLevel)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>(); // ���������� HashSet
        queue.Enqueue((startX, startY, 1)); // �������� � ������ ��������� 1
        visited.Add((startX, startY)); // �������� ��������� ���� ��� ����������

        while (queue.Count > 0)
        {
            (int x, int y, int lightLevel) = queue.Dequeue();

            // �������� ������
            if (x < 0 || x >= width || y < 0 || y >= height) continue;


            // �������� ���� �� ������ �����
            TileBase lightTile = null;
            //if (lightLevel == 1) lightTile = lightTiles[1];
            //else if (lightLevel == 2) lightTile = lightTiles[2];
            //else if (lightLevel > 2)
            //{
            //    lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // ���� ���� ������ 3 ������, �� ������� ���
            //    continue;
            //}

            if (lightLevel == 1) lightTile = lightTiles[0];
            else if (lightLevel == 2 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[1];
            else if (lightLevel == 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[2];
            else if (lightLevel > 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null)
            {
                lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // ���� ���� ������ 3 ������, �� ������� ���
                continue;
            }


            // �������� �� ������� ����� � TileBlocks
            if (tileMap.GetTile(new Vector3Int(x, y, 0)) == null)
            {
                if (lightTile != null)
                    lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTile);

                // ��������� �������� ����� � �������, ���� ��� �� ���� ��������
                if (!visited.Contains((x, y - 1)))
                {
                    queue.Enqueue((x, y - 1, lightLevel + 1));
                    visited.Add((x, y - 1));
                }
                if (!visited.Contains((x, y + 1)))
                {
                    queue.Enqueue((x, y + 1, lightLevel + 1));
                    visited.Add((x, y + 1));
                }
                if (!visited.Contains((x - 1, y)))
                {
                    queue.Enqueue((x - 1, y, lightLevel + 1));
                    visited.Add((x - 1, y));
                }
                if (!visited.Contains((x + 1, y)))
                {
                    queue.Enqueue((x + 1, y, lightLevel + 1));
                    visited.Add((x + 1, y));
                }
            }
        }
    }
}

