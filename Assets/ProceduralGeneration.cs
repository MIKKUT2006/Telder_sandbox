using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;
using UnityEngine.Tilemaps;
//using System;
//using Assets;
//using Client = Supabase.Client;
//using System.Threading.Tasks;

//public class ProceduralGeneration : MonoBehaviour
//{
//    // ТЕСТЫ
//    public static int X, Y;
//    [System.Serializable]
//    public class Column
//    {
//        public bool[] rows = new bool[Y];
//    }

//    public Column[] columns = new Column[X];
//    // ТЕСТЫ

//    // Префаб для чанка
//    [SerializeField] public GameObject chunkPrefab;
//    [SerializeField] public GameObject lightchunkPrefab;
//    [SerializeField] public GameObject bgchunkPrefab;
//    [SerializeField] public GameObject grasschunkPrefab;

//    // Список блоков в структуре
//    [Header("генерация структур")]
//    [SerializeField] public Tilemap testStructure;
//    [SerializeField] public Tilemap mapleHouse;

//    [Header("Размеры мира")]
//    [SerializeField] public int height = 0;       // высота (мира)
//    [SerializeField] public int width = 0;        // Ширина (мира)

//    [Header("Мягкость земли, пещер, камня")]
//    [SerializeField] float smoothes;                // Мягкость
//    [SerializeField] float cavessmothes;            // Мягкость Пещер
//    [SerializeField] float stonesmothes;            // Мягкость Камня
//    [SerializeField] float biomeSmoothes;           // Мягкость Биома пустынм

//    [Header("Мягкость генерации руд")]
//    [SerializeField] float ironOre;                 // Мягкость железной руды
//    [SerializeField] float coalOre;                 // Мягкость угольной руды
//    [SerializeField] float teleportiumOre;          // Мягкость камня пространства

//    [SerializeField] float seed;                    // Сид мира
//    [SerializeField] List<TileBase> groundTile;     // Тайл
//    [SerializeField] public static List<TileBase> lightTiles;            // Тайл освещения
//    [SerializeField] List<TileBase> lightTilesInspector;            // Тайл освещения
//    [SerializeField] Tilemap tilemap;               // Карта тайлов
//    [SerializeField] Tilemap bgTilemap;             // Карта тайлов заднего фона
//    [SerializeField] Tilemap lightTilemap;          // Карта тайлов для освещения
//    [SerializeField] Tilemap grassTilemap;          // Карта тайлов растительности

//    [SerializeField] Cell cell;

//    [SerializeField] public static int[,] map;      // Двумерный массив карты
//    [SerializeField] public static float[,] lightMap;      // Двумерный массив карты
//    [SerializeField] public static float[,] sunlightMap;      // Двумерный массив карты
//    [SerializeField] public static int[,] bgMap;    // Двумерный массив карты заднего плана

//    //private enum Biomes { Desert, Forest, Crystal, None }
//    //private Biomes[] biomeMap;


//    [SerializeField] GameObject mainTilemap;
//    [SerializeField] Tilemap testhouse;

//    [SerializeField] List<GameObject> Trees;

//    // 0 = солнечный свет
//    // 1 = трава
//    // 2 = земля
//    // 3 = камень
//    // 4 = пустота
//    // 5 = трава с деревьями
//    // 1 = трава
//    // 1 = трава
//    // 1 = трава

//    // Для проверки работы шума перлина
//    public int x = 0, y = 0;
//    public int worldSeed;

//    //[Header("Освещение")]
//    //public static Texture2D worldTilesMap;
//    //public Texture2D worldTilesMapInspector;
//    //public Material lightShader;
//    //public float lightThreshold;
//    //public static float lightRadius = 7f;
//    static List<Vector2Int> unlitBlocks = new List<Vector2Int>();

//    [Header("Освещение")]
//    public Material lightShader;
//    public static float lightRadius = 7f;

//    // Используем двумерный массив для хранения данных освещения.
//    public static float[,] lightMapFloatArray;
//    public static int mapWidth, mapHeight;
//    public static Texture2D worldTilesMap;
//    public Texture2D worldTilesMapInspector;
//    //private static bool isUpdateLightMap = true;
//    public static ProceduralGeneration instance;

//    void Awake()
//    {
//        HelperClass.worldHeight = height;
//        HelperClass.worldWidth = width;

//        mapWidth = width;
//        mapHeight = height;
//        worldTilesMap = new Texture2D(mapWidth, mapWidth);
//        //worldTilesMap.filterMode = FilterMode.Point;
//        lightShader.SetTexture("_shadowTexture", worldTilesMap);
//        lightMapFloatArray = new float[mapWidth, mapHeight];

//        instance = this;

//        lightTiles = lightTilesInspector;

//        // Устанавливаем начальную темноту
//        for (int x = 0; x < mapWidth; x++)
//        {
//            for (int y = 0; y < mapHeight; y++)
//            {
//                lightMapFloatArray[x, y] = 0f;
//            }
//        }

//        HelperClass.biomeMap = new HelperClass.Biomes[width];

//        //cell = new Cell();

//        // Создаём чанки
//        //CreateChunks();
//        //// Проверяем на создание нового мира или загрузуку существующего
//        //if (HelperClass.isNewGame == true)
//        //{
//        //    Generation();

//        //    HelperClass.worldHeight = height;
//        //    HelperClass.worldWidth = width;
//        //}
//        //else
//        //{
//        //    map = HelperClass.map;
//        //    bgMap = HelperClass.bgMap;

//        //    height = HelperClass.worldHeight;
//        //    width = HelperClass.worldWidth;
//        //}

//        //RenderMap(map, tilemap, groundTile, bgMap);             // Показываем изменения

//        HelperClass.chunkPrefab = chunkPrefab;
//        HelperClass.lightchunkPrefab = lightchunkPrefab;
//        HelperClass.bgchunkPrefab = bgchunkPrefab;
//        HelperClass.grasschunkPrefab = grasschunkPrefab;

//        worldTilesMapInspector = worldTilesMap;

//        //UpdateTextureFromLightmap();
//        //ApplySunlight();

//        // Пример: добавим несколько источников света
//        //UpdateLightTexture();
//    }
//    void Generation()
//    {
//        bgMap = GenerateArray(width, height);       // Генерируем массив
//        lightMap = GenerateFloatArray(width, height);       // Генерируем массив
//        sunlightMap = GenerateFloatArray(width, height);       // Генерируем массив
//        map = GenerateArray(width, height);        // Генерируем массив

//        lightTilemap.ClearAllTiles();                           // Очищаем все тайлы перед генерацией

//        // Основной план
//        tilemap.ClearAllTiles();                                // Очищаем все тайлы перед генерацией
//        BiomeGeneration();                                      // Генерируем биомы
//        map = TerrainGeneration(map);                           // Генерируем мир
//        map = StoneGeneration(map);                             // Генерируем камень
//        map = CavesGeneration(map, bgMap).Item1;                // Генерируем пещеры
//        DestroyStructures();
//        // Задниий план
//        bgTilemap.ClearAllTiles();                              // Очищаем все тайлы перед генерацией

//        bgMap = TerrainGeneration(bgMap);                       // Генерируем мир
//        bgMap = StoneGeneration(bgMap);                         // Генерируем мир
//        bgMap = GrassGeneration(bgMap);                         // Генерируем мир
//        bgMap = CavesGeneration(map, bgMap).Item2;              // Генерируем пещеры

//        map = TreesGeneration(map);

//        //LiquidSimulation.Initialize(width, height, HelperClass.playerGameObject.transform, GameObject.Find("Liquid").GetComponent<SpriteRenderer>());

//        //StructuresGeneration(testStructure);
//        //StructuresGeneration(testStructure, 12);
//        //StructuresGeneration(mapleHouse, 2);
//        //worldTilesMap.Apply();
//        HelperClass.isFullyGenerated = true;
//        Debug.Log("Всё готово");
//    }

//    public void CreateChunks()
//    {
//        int chunkSize = HelperClass.chunkSize;
//        int numChunksX = Mathf.CeilToInt((float)width / chunkSize); // Количество чанков по X
//        int numChunksY = Mathf.CeilToInt((float)height / chunkSize); // Количество чанков по Y

//        HelperClass.Chunks = new Tilemap[numChunksX, numChunksY];
//        HelperClass.ChunksGameobject = new GameObject[numChunksX, numChunksY];

//        HelperClass.lightChunks = new Tilemap[numChunksX, numChunksY];
//        HelperClass.lightChunksGameobject = new GameObject[numChunksX, numChunksY];

//        HelperClass.bgChunks = new Tilemap[numChunksX, numChunksY];
//        HelperClass.bgChunksGameobject = new GameObject[numChunksX, numChunksY];

//        HelperClass.grassChunks = new Tilemap[numChunksX, numChunksY];
//        HelperClass.grassChunksGameobject = new GameObject[numChunksX, numChunksY];

//        for (int x = 0; x < numChunksX; x++)
//        {
//            int indexX = x; // Индекс чанка x
//            for (int y = 0; y < numChunksY; y++)
//            {
//                int indexY = y; // Индекс чанка y
//                //------------------
//                GameObject Chunk = Instantiate(chunkPrefab);
//                Chunk.name = $"Chunk_{x}|{y}";
//                Chunk.transform.parent = transform;
//                HelperClass.ChunksGameobject[indexX, indexY] = Chunk;
//                HelperClass.Chunks[indexX, indexY] = Chunk.GetComponent<Tilemap>();

//                GameObject lightChunk = Instantiate(lightchunkPrefab);
//                lightChunk.name = $"LightChunk_{x}|{y}";
//                lightChunk.transform.parent = transform;
//                HelperClass.lightChunksGameobject[indexX, indexY] = lightChunk;
//                HelperClass.lightChunks[indexX, indexY] = lightChunk.GetComponent<Tilemap>();

//                GameObject bgChunk = Instantiate(bgchunkPrefab);
//                bgChunk.name = $"BgChunk_{x}|{y}";
//                bgChunk.transform.parent = transform;
//                HelperClass.bgChunksGameobject[indexX, indexY] = bgChunk;
//                HelperClass.bgChunks[indexX, indexY] = bgChunk.GetComponent<Tilemap>();

//                GameObject grassChunk = Instantiate(grasschunkPrefab);
//                grassChunk.name = $"BgChunk_{x}|{y}";
//                grassChunk.transform.parent = transform;
//                HelperClass.grassChunksGameobject[indexX, indexY] = grassChunk;
//                HelperClass.grassChunks[indexX, indexY] = grassChunk.GetComponent<Tilemap>();
//                //------------------
//            }
//        }
//    }

//    void DestroyStructures()
//    {
//        List<GameObject> trees = GameObject.FindGameObjectsWithTag("tree").ToList();

//        foreach (var item in trees)
//        {
//            Destroy(item);
//        }
//    }

//    public static int GenerateTileAt(int gx, int gy, out int bgTile)
//    {
//        bgTile = 0; // по умолчанию — пустой задний фон

//        // 1. Биом
//        HelperClass.Biomes biome = GetBiomeAt(gx);

//        // 2. Высота поверхности
//        int terrainHeight = Mathf.RoundToInt(Mathf.PerlinNoise(gx / 200f / 2f, HelperClass.worldSeed + 5000) * 100f);
//        terrainHeight += 50;

//        // 3. Камень
//        int stoneHeight = Mathf.RoundToInt(Mathf.PerlinNoise(gx / 2000 / 0.5f, HelperClass.worldSeed * 3) * 100f);
//        stoneHeight += 33;

//        // 4. Логика
//        if (gy < stoneHeight)
//        {
//            return GetStoneBlockByBiome(biome);
//        }

//        if (gy < terrainHeight - 1)
//        {
//            return GetDirtBlockByBiome(biome);
//        }

//        if (gy == terrainHeight - 1)
//        {
//            return GetSurfaceBlockByBiome(biome);
//        }

//        // 5. Пещеры
//        float caveNoise = Mathf.PerlinNoise((gx + HelperClass.worldSeed) / 10, (gy + HelperClass.worldSeed) / 10);
//        if (caveNoise < 0.4f)
//            return 4;

//        // 6. Замшелые пещеры
//        float caveMoss = Mathf.PerlinNoise((gx + 42) / 12f, (gy + 42) / 10f);
//        if (gy > stoneHeight && caveMoss > 0.15f && caveMoss < 0.2f)
//        {
//            bgTile = 12;
//            return 4;
//        }

//        // 7. Руды
//        if (gy < stoneHeight + 5)
//        {
//            float oreIron = Mathf.PerlinNoise((gx + HelperClass.worldSeed / 2f) / 10, (gy + HelperClass.worldSeed / 2f) / 10);
//            if (oreIron > 0.8f && biome == HelperClass.Biomes.Forest)
//                return 6;

//            float oreCoal = Mathf.PerlinNoise((gx + HelperClass.worldSeed / 4f) / 14, (gy + HelperClass.worldSeed / 4f) / 14);
//            if (oreCoal > 0.8f)
//                return 17;

//            float oreTeleportium = Mathf.PerlinNoise((gx + HelperClass.worldSeed) / 4 / 0.9f, (gy + HelperClass.worldSeed) / 4 / 0.9f);
//            if (oreTeleportium > 0.87f && biome == HelperClass.Biomes.Crystal)
//                return 7;
//        }

//        return 0; // пустота
//    }
//    [System.Serializable]
//    public class BiomeRange
//    {
//        public string biomeName;
//        public float minThreshold;
//        public float maxThreshold;

//        public BiomeRange(string name, float min, float max)
//        {
//            biomeName = name;
//            minThreshold = min;
//            maxThreshold = max;
//        }
//    }

//    public static List<BiomeRange> biomeRanges = new List<BiomeRange>();
//    static HelperClass.Biomes GetBiomeAt(int gx)
//    {
//        float noise = Mathf.PerlinNoise(gx * 0.005f, 0);
//        foreach (var range in biomeRanges)
//        {
//            if (noise >= range.minThreshold && noise < range.maxThreshold)
//                return (HelperClass.Biomes)Enum.Parse(typeof(HelperClass.Biomes), range.biomeName);
//        }
//        return HelperClass.Biomes.Desert;
//    }

//    static int GetSurfaceBlockByBiome(HelperClass.Biomes biome)
//    {
//        return biome switch
//        {
//            HelperClass.Biomes.Forest => 2,
//            HelperClass.Biomes.Desert => 9,
//            HelperClass.Biomes.Crystal => 10,
//            HelperClass.Biomes.Snow => 11,
//            _ => 2,
//        };
//    }

//    static int GetDirtBlockByBiome(HelperClass.Biomes biome)
//    {
//        return biome switch
//        {
//            HelperClass.Biomes.Forest => 1,
//            HelperClass.Biomes.Desert => 9,
//            HelperClass.Biomes.Crystal => 10,
//            HelperClass.Biomes.Snow => 1,
//            _ => 1,
//        };
//    }

//    static int GetStoneBlockByBiome(HelperClass.Biomes biome)
//    {
//        return biome switch
//        {
//            HelperClass.Biomes.Forest => 3,
//            HelperClass.Biomes.Desert => 9,
//            HelperClass.Biomes.Crystal => 10,
//            HelperClass.Biomes.Snow => 3,
//            _ => 3,
//        };
//    }

//    // Создаём массив размерами мира, указывая, что у него есть тайлы (создано для разных размеров миров)
//    // Чтобы не создавать вручную весь массив
//    public int[,] GenerateArray(int width, int height)
//    {
//        int[,] array = new int[width, height];                // Устанавливаем размеры мира

//        // Генерируем мир по ширине
//        for (int i = 0; i < width; i++)
//        {
//            // Генерируем мир по высоте
//            for (int j = 0; j < height; j++)
//            {
//                array[i, j] = 0;
//            }
//        }
//        return array;
//    }
//    public float[,] GenerateFloatArray(int width, int height)
//    {
//        float[,] array = new float[width, height];                // Устанавливаем размеры мира

//        // Генерируем мир по ширине
//        for (int i = 0; i < width; i++)
//        {
//            // Генерируем мир по высоте
//            for (int j = 0; j < height; j++)
//            {
//                array[i, j] = 0;
//            }
//        }
//        return array;
//    }
//    //-----
//    // как рабоает генерация: мы получаем от формулы шума перлина высоту, до которой генерируется земля, всё, что выше = 0
//    // Как сгенерировали один столбик, переходит к следующему
//    //-----

//    // ГЕНЕРАЦИЯ БИОМОВ
//    private int biomeWidth = HelperClass.worldWidth; // Ширина карты биомов

//    //public HelperClass.Biomes[] test;

    

//    public void BiomeGeneration()
//    {
//        biomeWidth = width; // Ширина карты биомов
//        float biomeNoiseScale = 0.005f; // Масштаб шума для биомов

//        // Проверка на правильность настроек биомов
//        biomeRanges.Sort((a, b) => a.minThreshold.CompareTo(b.minThreshold)); // Сортируем по минимальному порогу
//        for (int i = 0; i < biomeRanges.Count - 1; i++)
//        {
//            if (biomeRanges[i].maxThreshold > biomeRanges[i + 1].minThreshold)
//            {
//                Debug.LogError("Ошибка в настройках биомов: Пересечение порогов!");
//                return;
//            }
//        }


//        HelperClass.biomeMap = new HelperClass.Biomes[biomeWidth]; // Инициализируем массив

//        // Генерирование биомов
//        for (int x = 0; x < biomeWidth; x++)
//        {
//            float biomeValue = Mathf.PerlinNoise(x * biomeNoiseScale, 0);

//            // Находим биом, соответствующий значению шума
//            HelperClass.Biomes biome = FindBiome(biomeValue);
//            if (biome == HelperClass.Biomes.Crystal)
//            {
//                Debug.Log("ДА");
//            }
//            HelperClass.biomeMap[x] = biome;
//        }

//        //test = HelperClass.biomeMap;
//    }

//    HelperClass.Biomes FindBiome(float value)
//    {
//        foreach (var range in biomeRanges)
//        {
//            if (value >= range.minThreshold && value < range.maxThreshold)
//            {
//                return (HelperClass.Biomes)System.Enum.Parse(typeof(HelperClass.Biomes), range.biomeName);
//            }
//        }
//        // Если значение не попадает ни в один диапазон, вернуть биом по умолчанию (или обработать ошибку)
//        Debug.LogError("Значение шума не соответствует ни одному биому: " + value);
//        return HelperClass.Biomes.Desert; // Биом по умолчанию
//    }
//    // ГЕНЕРАЦИЯ БИОМОВ


//    public int[,] TerrainGeneration(int[,] map)     // Генерация земли
//    {
//        int perlinHeight;           // Высота перлина
//        for (int i = 0; i < width; i++)
//        {
//            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed + height) * height / 2.5f);
//            perlinHeight += height / 2;
//            for (int j = 0; j <= perlinHeight + 1; j++)
//            {
//                if (j < perlinHeight)
//                {
//                    switch (HelperClass.biomeMap[i])
//                    {
//                        case HelperClass.Biomes.Forest:
//                            map[i, j] = 1;
//                            break;
//                        case HelperClass.Biomes.Desert:
//                            map[i, j] = 9;
//                            break;
//                        case HelperClass.Biomes.Crystal:
//                            map[i, j] = 10;
//                            break;
//                        case HelperClass.Biomes.Snow:
//                            map[i, j] = 1;
//                            break;
//                    }
//                }

//                if (j == perlinHeight)
//                {
//                    switch (HelperClass.biomeMap[i])
//                    {
//                        case HelperClass.Biomes.Forest:
//                            map[i, j] = 2;
//                            break;
//                        case HelperClass.Biomes.Desert:
//                            map[i, j] = 9;
//                            break;
//                        case HelperClass.Biomes.Crystal:
//                            map[i, j] = 10;
//                            break;
//                        case HelperClass.Biomes.Snow:
//                            map[i, j] = 11;
//                            break;
//                    }
//                }
//            }
//        }

//        //// Дополнительные горы
//        for (int i = 0; i < width; i++)
//        {
//            // Получаем координату чанка
//            //int chunkCoord = i / HelperClass.chunkSize;   // Получаем координату чанка

//            //int ostatok = chunkCoord % 100;
//            //if (ostatok != 0)
//            //{
//            //    chunkCoord -= (chunkCoord - ostatok) + 1;
//            //}
//            //Tilemap lighttilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

//            //perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed / 2) * height / 2);
//            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 4, HelperClass.worldSeed + height) * height / 2f);
//            //Debug.Log(HelperClass.worldSeed);
//            perlinHeight += height / 2;
//            //perlinHeight = perlinHeight / 2;


//            for (int j = 0; j <= perlinHeight + 1; j++)
//            {
//                if (j < perlinHeight)
//                {
//                    switch (HelperClass.biomeMap[i])
//                    {
//                        case HelperClass.Biomes.Forest:
//                            map[i, j] = 1;
//                            break;
//                        case HelperClass.Biomes.Desert:
//                            map[i, j] = 9;
//                            break;
//                        case HelperClass.Biomes.Crystal:
//                            map[i, j] = 10;
//                            break;
//                        case HelperClass.Biomes.Snow:
//                            map[i, j] = 1;
//                            break;
//                    }
//                }

//                if (j == perlinHeight && map[i, j + 1] < 1)
//                {
//                    map[i, j] = 2;
//                }

//                if (j > perlinHeight && map[i, j + 1] < 1)
//                {
//                    map[i, j] = 0;
//                }
//            }
//        }
//        return map;
//    }

//    public int[,] StoneGeneration(int[,] map)     // Генерация каменного слоя
//    {
//        int perlinHeight;   // Высота перлина
//        for (int i = 0; i < width; i++)
//        {
//            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
//            perlinHeight += height / 3;

//            for (int j = 0; j <= perlinHeight; j++)
//            {

//                if (j < perlinHeight && (map[i, j] == 1 || map[i, j] != 2))
//                {
//                    switch (HelperClass.biomeMap[i])
//                    {
//                        case HelperClass.Biomes.Forest:
//                            map[i, j] = 3;
//                            break;
//                        case HelperClass.Biomes.Desert:
//                            map[i, j] = 9;
//                            break;
//                        case HelperClass.Biomes.Crystal:
//                            map[i, j] = 10;
//                            break;
//                        case HelperClass.Biomes.Snow:
//                            map[i, j] = 3;
//                            break;
//                    }
//                }
//            }
//        }
//        return map;
//    }

//    public (int[,], int[,]) CavesGeneration(int[,] map, int[,] bgMap)     // Генерация пещер
//    {
//        int perlinHeightStone;   // Высота перлина
//        float perlinHeightCaves;   // Высота перлина
//        float perlinHeightGround;   // Высота перлина поверхностных пещер
//        float perlinHeightOres;   // Высота перлина руд
//        float perlinHeightTeleportium;
//        float cavesSeed = UnityEngine.Random.Range(0, 100);

//        Debug.Log(bgMap);
//        for (int i = 0; i < width; i++)
//        {
//            perlinHeightStone = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
//            perlinHeightStone += height / 3;

//            for (int j = 0; j < height; j++)
//            {
//                // Пещеры
//                perlinHeightCaves = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
//                if (perlinHeightCaves < 0.4 && (map[i, j] == 3 || map[i, j] == 9 || map[i, j] == 10))
//                {
//                    map[i, j] = 4;
//                }

//                // Пещеры на поверхности
//                perlinHeightGround = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes / 2, (j + HelperClass.worldSeed) / cavessmothes / 2);
//                if (perlinHeightGround < 0.4 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
//                {
//                    map[i, j] = 4;
//                }

//                // Замшелые пещеры
//                perlinHeightGround = Mathf.PerlinNoise((i + cavesSeed) / 12, (j + cavesSeed) / 10);
//                if (perlinHeightGround > 0.15 && perlinHeightGround < 0.2 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
//                {
//                    map[i, j] = 12;
//                }
//                if (perlinHeightGround < 0.2 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
//                {
//                    map[i, j] = 4;
//                    bgMap[i, j] = 12;
//                }

//                // Генерация железной руды
//                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 2) / ironOre, (j + HelperClass.worldSeed / 2) / ironOre);
//                //Debug.Log(perlinHeightOres);
//                if (perlinHeightOres > 0.8 && map[i, j] == 3)
//                {
//                    map[i, j] = 6;
//                }

//                // Генерация угольной руды
//                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 4) / coalOre, (j + HelperClass.worldSeed / 4) / coalOre);
//                if (perlinHeightOres > 0.8 && map[i, j] == 3)
//                {
//                    map[i, j] = 17;
//                }

//                // Генерация руды телепортации
//                perlinHeightTeleportium = Mathf.PerlinNoise((i + HelperClass.worldSeed) / teleportiumOre / 0.9f, (j + HelperClass.worldSeed) / teleportiumOre / 0.9f);

//                //perlinHeightTeleportium = Mathf.RoundToInt(Mathf.PerlinNoise(j / stonesmothes, seed * 3) * height / 2.1f);
//                //Debug.Log(perlinHeightTeleportium);
//                if (perlinHeightTeleportium > 0.87 && HelperClass.biomeMap[i] == HelperClass.Biomes.Crystal && map[i, j] == 10)
//                {
//                    map[i, j] = 7;
//                }
//            }
//        }
//        return (map, bgMap);
//    }

//    public int[,] TreesGeneration(int[,] map)     // Генерация деревьев
//    {
//        float perlinHeight;   // Высота перлина

//        for (int i = 0; i < width; i++)
//        {
//            for (int j = 0; j < height; j++)
//            {
//                perlinHeight = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
//                //Debug.Log(perlinHeight);

//                if (perlinHeight < 0.4 && map[i, j] == 2)
//                {
//                    map[i, j] = 5;
//                }
//            }
//        }
//        return map;
//    }

//    public int[,] GrassGeneration(int[,] map)     // Генерация пушистой травы
//    {
//        float perlinHeight;   // Высота перлина

//        for (int i = 0; i < width; i++)
//        {
//            for (int j = 0; j < height; j++)
//            {
//                perlinHeight = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);

//                if (perlinHeight < 0.5 && map[i, j] == 2)
//                {
//                    bgMap[i, j + 1] = 8;
//                }
//            }
//        }
//        return map;
//    }

//    public int[,] LightGeneraion(int[,] map)
//    {
//        //int perlinHeight;   // Высота перлина
//        for (int i = 0; i < width; i++)
//        {
//            // Получаем координату чанка
//            //int chunkCoord = i / HelperClass.chunkSize;   // Получаем координату чанка
//            ////chunkCoord = chunkCoord * chunkSize;

//            //int ostatok = chunkCoord % 100;
//            //if (ostatok != 0)
//            //{
//            //    chunkCoord -= (chunkCoord - ostatok) + 1;
//            //}
//            //Tilemap lighttilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

//            //perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes, seed) * height / 2);
//            //perlinHeight += height / 2;

//            for (int j = height - 1; j > 0; j--)
//            {
//                //Debug.Log(i + ":" + j);
//                if (map[i, j] != 0 && map[i, j] != 4)
//                {
//                    //Debug.Log(map[i, j]);
//                    //map[i, j] = 0;
//                    //lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);

//                    //HelperClass.lightChunks[chunkCoord] = tilemap;
//                    break;
//                }

//                if (map[i, j] == 4)
//                {
//                    //Debug.Log(map[i, j]);
//                    //map[i, j] = 0;
//                    //lighttilemap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);

//                    //HelperClass.lightChunks[chunkCoord] = tilemap;
//                }
//            }
//            //for (int g = perlinHeight; g < height; g++)
//            //{
//            //    lighttilemap.SetTile(new Vector3Int(i, g, 0), lightTile);
//            //}
//            //lightChunks[chunkCoord] = tilemap;
//        }
//        return map;
//    }

//    public void StructuresGeneration(Tilemap structureTilemap, int structureCount)
//    {

//        for (int i = 0; i < structureCount; i++)
//        {
//            BoundsInt bounds = structureTilemap.cellBounds;
//            int structureCoordX = (int)UnityEngine.Random.RandomRange(0, width);
//            int structureCoordY = (int)UnityEngine.Random.RandomRange(0, 100);
//            if (map[structureCoordX, structureCoordY] != 0)
//            {
//                for (int x = bounds.xMin; x < bounds.xMax; x++)
//                {
//                    for (int y = bounds.yMin; y < bounds.yMax; y++)
//                    {
//                        Vector3Int tilePos = new Vector3Int(x, y, 0);
//                        TileBase tile = structureTilemap.GetTile(tilePos);

//                        if (tile != null)
//                        {
//                            //Debug.Log("Tile at position " + tilePos + " is " + tile.name);
//                            tilePos.x += structureCoordX;
//                            tilePos.y += structureCoordY;

//                            //AllItemsAndBlocks allItemsAndBlocks = BlocksData.allBlocks.Where(x => x.imagePath.Split('/')[x.imagePath.Split('/').Length-1] == tile.name).FirstOrDefault();
//                            //if (allItemsAndBlocks != null)
//                            //{
//                            //    map[x, y] = allItemsAndBlocks.blockIndex;
//                            //}

//                            int chunkCoord = tilePos.x / HelperClass.chunkSize;

//                            int ostatok = chunkCoord % 100;
//                            if (ostatok != 0)
//                            {
//                                chunkCoord -= (chunkCoord - ostatok) + 1;
//                            }
//                            int chunkCoordX = ChunkHelper.GetChunkXCoordinate(x);
//                            int chunkCoordY = ChunkHelper.GetChunkYCoordinate(y);
//                            Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
//                            //Debug.Log(HelperClass.Cells.GetLength(0));
//                            //HelperClass.Cells[x, y].SetType(CellType.Solid);
//                            tilemap.SetTile(tilePos, tile);
//                        }
//                    }
//                }
//            }

//        }
//    }

//    public static class ChunkHelper
//    {
//        public static int GetChunkXCoordinate(int x)
//        {
//            int chunkSize = HelperClass.chunkSize;
//            return Mathf.FloorToInt(x / (float)chunkSize);
//        }
//        public static int GetChunkYCoordinate(int y)
//        {
//            int chunkSize = HelperClass.chunkSize;
//            return Mathf.FloorToInt(y / (float)chunkSize);
//        }
//    }

//    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // Массив, тайлмап, тайлы блока (список блоков)
//    {
//        int width = map.GetLength(0);
//        int height = map.GetLength(1);
//        int chunkSize = HelperClass.chunkSize;

//        for (int i = 0; i < width; i++)
//        {
//            // Получаем координату чанка
//            int chunkCoordX = ChunkHelper.GetChunkXCoordinate(i);

//            for (int j = 0; j < height; j++)
//            {
//                int chunkCoordY = ChunkHelper.GetChunkYCoordinate(j);

//                Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
//                Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
//                Tilemap bgTileMap = HelperClass.bgChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
//                Tilemap grassTileMap = HelperClass.grassChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
//                switch (map[i, j])
//                {
//                    case 0:
//                        //LightBlock(x, y, 1f, 0);
//                        //worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
//                        lightMapFloatArray[i, j] = 1;
//                        //LightBlock(i, j, 1);
//                        break;
//                    case 1:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
//                        //LightBlock(i, j, 1);
//                        break;
//                    case 2:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // Устанавливаем тайл травы
//                        //LightBlock(i, j, 1);
//                        break;
//                    case 3:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
//                        //LightBlock(i, j, 1);
//                        break;
//                    case 4:
//                        break;
//                    case 5:
//                        Vector3 pos = new Vector3(i + 0.5f, j + 5, 0);
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // Устанавливаем деревья
//                        Instantiate(Trees[(int)UnityEngine.Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
//                        break;
//                    case 6:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // Устанавливаем тайл железной руды
//                        break;
//                    case 7:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[5]);       // Устанавливаем тайл руды камня пространства
//                        break;
//                    case 8:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[7]);       // Устанавливаем тайл барьера
//                        break;
//                    case 9:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // Устанавливаем тайл песка
//                        break;
//                    case 10:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // Устанавливаем тайл песка
//                        break;
//                    case 11:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[10]);       // Устанавливаем тайл снега
//                        break;
//                    case 12:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // Устанавливаем тайл мха
//                        break;
//                    case 17:
//                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[12]);       // Устанавливаем тайл угля
//                        break;
//                }

//                HelperClass.Chunks[chunkCoordX, chunkCoordY] = tileMap;
//                if (bgMap[i, j] == 0)
//                {
//                    worldTilesMap.SetPixel(i, j, Color.white);
//                }
//                if (bgMap[i, j] == 1)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // Устанавливаем тайл земли
//                    //RemoveLightSource(i, j);
//                }
//                if (bgMap[i, j] == 2)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
//                    //RemoveLightSource(i, j);
//                }
//                if (bgMap[i, j] == 3)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
//                    //RemoveLightSource(i, j);
//                }
//                if (bgMap[i, j] == 8)
//                {
//                    grassTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // Устанавливаем тайл пушистой травы
//                }
//                if (bgMap[i, j] == 9)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // Устанавливаем тайл камня
//                    //RemoveLightSource(i, j);
//                }
//                if (bgMap[i, j] == 10)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // Устанавливаем тайл камня
//                    //RemoveLightSource(i, j);
//                }
//                if (bgMap[i, j] == 12)
//                {
//                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // Устанавливаем тайл мха
//                    //RemoveLightSource(i, j);
//                }
//            }
//        }

//        //// заполняем освещение
//        //for (int x = 0; x < width; x++)
//        //{
//        //    float light = 1f;

//        //    for (int y = 0; y < height; y++)
//        //    {
//        //        //if (map[x, y] == 0)
//        //        //{
//        //        //    //LightBlock(x, y, 1f, 0);
//        //        //    //LightBlock(x, y, 1f);
//        //        //    LightFloodFill(x, y, 1f);
//        //        //    worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
//        //        //}
//        //        if (map[x, y] != 0)
//        //            break; // блок перекрывает солнечный свет

//        //        lightMap[x, y] = light;
//        //        light *= 0.95f; // постепенное затухание вниз
//        //    }

//        //    for (int y = height - 1; y >= 0; y--)
//        //    {
//        //        if (map[x, y] != 0)
//        //            break; // блок перекрывает солнечный свет

//        //        lightMap[x, y] = light;
//        //        light *= 0.95f; // постепенное затухание вниз
//        //    }
//        //}
//        //worldTilesMap.Apply();
//    }

//    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//    ///

//    void ApplySunlight()
//    {
//        for (int x = 0; x < width; x++)
//        {
//            bool blocked = false;
//            float light = 1f;

//            for (int y = height - 1; y >= 0; y--)
//            {
//                int tile = map[x, y];

//                if (tile == 0 || tile == 4)
//                {
//                    if (!blocked)
//                    {
//                        lightMap[x, y] = 1f; // воздух и ямы освещены полностью
//                    }
//                    else
//                    {
//                        lightMap[x, y] = 0f; // блоки в тени
//                    }
//                }
//                else
//                {
//                    if (!blocked)
//                    {
//                        // Свет проникает в твёрдые блоки
//                        light *= 0.85f;
//                        lightMap[x, y] = light;
//                        blocked = true;
//                    }
//                    else
//                    {
//                        light *= 0.6f; // продолжает затухать ниже
//                        lightMap[x, y] = light;
//                    }
//                }
//            }
//        }

//        // Боковое распространение света
//        PropagateSideLight();
//    }
//    public static void ApplySunlightColumn(int x)
//    {
//        if (x < 0 || x >= mapWidth) return;

//        bool blocked = false;
//        float light = 1f;

//        for (int y = mapHeight - 1; y >= 0; y--)
//        {
//            int tile = map[x, y];

//            if (tile == 0 || tile == 4)
//            {
//                if (!blocked)
//                {
//                    lightMap[x, y] = 1f;
//                }
//                else
//                {
//                    lightMap[x, y] = 0f;
//                }
//            }
//            else
//            {
//                if (!blocked)
//                {
//                    light *= 0.85f;
//                    lightMap[x, y] = light;
//                    blocked = true;
//                }
//                else
//                {
//                    light *= 0.6f;
//                    lightMap[x, y] = light;
//                }
//            }
//        }
//    }
//    void PropagateSideLight()
//    {
//        // Проходим по всем клеткам и распространяем свет по горизонтали
//        for (int y = 0; y < height; y++)
//        {
//            for (int x = 0; x < width; x++)
//            {
//                if (map[x, y] == 0 || map[x, y] == 4) // если это освещённые блоки (воздух или яма)
//                {
//                    // Пропагируем свет влево
//                    PropagateLightToSide(x, y, -1); // влево
//                                                    // Пропагируем свет вправо
//                    PropagateLightToSide(x, y, 1);  // вправо
//                }
//            }
//        }
//    }

//    void PropagateLightToSide(int startX, int startY, int direction)
//    {
//        int x = startX;
//        int y = startY;
//        float light = lightMap[startX, startY];

//        while (x >= 0 && x < width && y >= 0 && y < height)
//        {
//            // Пропагируем свет, проверяя, что индекс находится в пределах
//            if (x >= 0 && x < width && y >= 0 && y < height)
//            {
//                if (map[x, y] == 0 || map[x, y] == 4) // если это воздух или яма
//                {
//                    lightMap[x, y] = Mathf.Max(lightMap[x, y], light); // обновляем освещённость
//                }
//                else
//                {
//                    light *= 0.7f; // затухаем, если блок твёрдый
//                    if (light < 0.01f) break; // если свет слишком слабый, останавливаем
//                }
//            }

//            // Продолжаем двигаться в указанном направлении
//            x += direction;

//            // Если вышли за пределы, прекращаем распространение
//            if (x < 0 || x >= width || y < 0 || y >= height)
//                break;
//        }
//    }
//    void PropagateLight(int startX, int startY, float strength)
//    {
//        Queue<(int x, int y, float light)> queue = new();
//        queue.Enqueue((startX, startY, strength));

//        while (queue.Count > 0)
//        {
//            var (x, y, light) = queue.Dequeue();

//            if (x < 0 || y < 0 || x >= width || y >= height)
//                continue;

//            if (light <= 0.01f)
//                continue;

//            if (map[x, y] != 0)
//                light *= 0.5f; // свет частично проходит через блок

//            if (light <= lightMap[x, y])
//                continue;

//            lightMap[x, y] = light;

//            float newLight = light * 0.85f;

//            queue.Enqueue((x + 1, y, newLight));
//            queue.Enqueue((x - 1, y, newLight));
//            queue.Enqueue((x, y + 1, newLight));
//            queue.Enqueue((x, y - 1, newLight));

//            float diagLight = newLight * 0.8f;
//            queue.Enqueue((x + 1, y + 1, diagLight));
//            queue.Enqueue((x - 1, y + 1, diagLight));
//            queue.Enqueue((x + 1, y - 1, diagLight));
//            queue.Enqueue((x - 1, y - 1, diagLight));
//        }
//    }

//    public static void UpdateLightTexture()
//    {
//        if (worldTilesMap.width == 0 || worldTilesMap.height == 0)
//        {
//            Debug.LogError("Texture dimensions are invalid!");
//            return;
//        }

//        for (int x = 0; x < mapWidth; x++)
//        {
//            for (int y = 0; y < mapHeight; y++)
//            {
//                float light = Mathf.Clamp01(lightMap[x, y]);
//                Color lightColor = new Color(light, light, light);
//                worldTilesMap.SetPixel(x, y, lightColor);
//            }
//        }

//        // Применяем изменения
//        worldTilesMap.Apply();
//    }

//    public static void AddLightSource(int x, int y, float lightIntensity, int radius)
//    {
//        // Добавляем сам источник
//        lightMap[x, y] = Mathf.Max(lightMap[x, y], lightIntensity);

//        // Распространяем свет по радиусу
//        for (int dx = -radius; dx <= radius; dx++)
//        {
//            for (int dy = -radius; dy <= radius; dy++)
//            {
//                int newX = x + dx;
//                int newY = y + dy;

//                // Проверка на допустимость индексов
//                if (newX >= 0 && newX < mapWidth && newY >= 0 && newY < mapHeight)
//                {
//                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
//                    if (distance <= radius) // Если в пределах радиуса
//                    {
//                        float lightDecay = Mathf.Max(0f, lightIntensity - distance * 0.1f);
//                        lightMap[newX, newY] = Mathf.Max(lightMap[newX, newY], lightDecay);
//                    }
//                }
//            }
//        }
//    }

//    public static void RemoveLightSource(int x, int y, int radius)
//    {
//        if (!InBounds(x, y))
//            return;

//        // 1. Обнуляем свет в радиусе
//        for (int dx = -radius; dx <= radius; dx++)
//        {
//            for (int dy = -radius; dy <= radius; dy++)
//            {
//                int nx = x + dx;
//                int ny = y + dy;

//                if (InBounds(nx, ny))
//                {
//                    lightMap[nx, ny] = 0;
//                }
//            }
//        }

//        // 2. Проверяем, есть ли другие источники поблизости, и перезапускаем их свет
//        for (int dx = -radius; dx <= radius; dx++)
//        {
//            for (int dy = -radius; dy <= radius; dy++)
//            {
//                int nx = x + dx;
//                int ny = y + dy;

//                if (InBounds(nx, ny))
//                {
//                    float intensity = lightMap[nx, ny];

//                    // Если в этом месте остался свет — возможно, это другой источник
//                    if (intensity > 0.8f && (map[nx, ny] == 0 || map[nx, ny] == 4))
//                    {
//                        AddLightSource(nx, ny, intensity, radius); // Перезапуск света
//                    }
//                }
//            }
//        }
//    }
//    public static void RecalculateLightAround(int x, int y)
//    {
//        //int radius = 8; // тот же радиус, что у света

//        //// Обнуляем свет в радиусе
//        //for (int dx = -radius; dx <= radius; dx++)
//        //{
//        //    for (int dy = -radius; dy <= radius; dy++)
//        //    {
//        //        int nx = x + dx;
//        //        int ny = y + dy;

//        //        if (InBounds(nx, ny))
//        //        {
//        //            lightMap[nx, ny] = 0;
//        //        }
//        //    }
//        //}

//        //// Перезапускаем освещение от ближайших источников (ярких клеток воздуха)
//        //for (int dx = -radius; dx <= radius; dx++)
//        //{
//        //    for (int dy = -radius; dy <= radius; dy++)
//        //    {
//        //        int nx = x + dx;
//        //        int ny = y + dy;

//        //        if (InBounds(nx, ny))
//        //        {
//        //            // Считаем, что яркие клетки воздуха — это источник (например, солнце или факел)
//        //            if ((map[nx, ny] == 0 || map[nx, ny] == 4) && lightMap[nx, ny] >= 0.8f)
//        //            {
//        //                // Запускаем свет из этой клетки
//        //                AddLightSource(nx, ny, lightMap[nx, ny], radius);
//        //            }
//        //        }
//        //    }
//        //}

//        // Определяем радиус и начальную интенсивность света
//        int radius = 8;           // Радиус распространения света
//        float intensity = 1.0f;   // Начальная интенсивность света (можно настроить)

//        // Перебираем все соседние блоки вокруг разрушенного
//        for (int dx = -1; dx <= 1; dx++)
//        {
//            for (int dy = -1; dy <= 1; dy++)
//            {
//                int nx = x + dx;
//                int ny = y + dy;

//                // Проверяем, что индексы в пределах карты
//                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
//                {
//                    // Если блок не является источником света и это не пустое место (не воздух)
//                    if ((map[nx, ny] != 0 || map[nx, ny] != 4) && IsSolidBlock(map[nx, ny]))
//                    {
//                        // Перерасчитываем свет вокруг этого блока с указанным радиусом и интенсивностью
//                        PropagateLightFrom(nx, ny, intensity, radius);
//                    }
//                }
//            }
//        }
//    }
//    public static bool InBounds(int x, int y)
//    {
//        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
//    }

//    public static void PropagateLightFrom(int x, int y, float intensity, int radius)
//    {
//        if (intensity <= 0 || radius <= 0 || !InBounds(x, y))
//            return;

//        Queue<Vector2Int> queue = new Queue<Vector2Int>();
//        float[,] visited = new float[mapWidth, mapHeight];

//        queue.Enqueue(new Vector2Int(x, y));
//        visited[x, y] = intensity;
//        lightMap[x, y] = Mathf.Max(lightMap[x, y], intensity);

//        while (queue.Count > 0)
//        {
//            Vector2Int current = queue.Dequeue();
//            float currentLight = visited[current.x, current.y];

//            // распространяем по 4 сторонам
//            Vector2Int[] dirs = new Vector2Int[]
//            {
//            Vector2Int.up,
//            Vector2Int.down,
//            Vector2Int.left,
//            Vector2Int.right
//            };

//            foreach (var dir in dirs)
//            {
//                int nx = current.x + dir.x;
//                int ny = current.y + dir.y;

//                if (!InBounds(nx, ny))
//                    continue;

//                // Блок проходимый (воздух или яма)
//                if (map[nx, ny] == 0 || map[nx, ny] == 4)
//                {
//                    float nextLight = currentLight - 0.05f; // скорость затухания света

//                    if (nextLight > visited[nx, ny])
//                    {
//                        visited[nx, ny] = nextLight;
//                        lightMap[nx, ny] = Mathf.Max(lightMap[nx, ny], nextLight);

//                        if (nextLight > 0.01f && Vector2Int.Distance(new Vector2Int(x, y), new Vector2Int(nx, ny)) <= radius)
//                            queue.Enqueue(new Vector2Int(nx, ny));
//                    }
//                }
//            }
//        }
//    }
//    public static void UpdateLightingAfterBlockChange(int x, int y)
//    {
//        // 1. Если мы поставили твёрдый блок, то отключаем свет в этом месте
//        if (IsSolidBlock(map[x, y]))
//        {
//            // Удаляем свет в радиусе вокруг установленного блока
//            RemoveLightSource(x, y, 8);
//        }

//        // 2. Перерассчитываем свет вокруг разрушенного блока
//        if (map[x, y] == 0) // если блок разрушен и стал воздухом
//        {
//            Debug.Log("Пересчет освещения");
//            // Восстанавливаем солнечный свет сверху
//            ApplySunlightColumn(x);

//            //Перезапускаем освещение из соседних источников
//            RecalculateLightAround(x, y);
//        }

//        // 3. Обновляем текстуру освещения
//        UpdateLightTexture();
//    }

//    private static bool IsSolidBlock(int blockId)
//    {
//        // Твёрдые блоки — это все, что не является воздухом (0) или ямой (4)
//        return blockId != 0 && blockId != 4;
//    }

//    //public static void ApplySunlightColumn(int x)
//    //{
//    //    if (x < 0 || x >= mapWidth) return;

//    //    float sunlightIntensity = 1.0f;

//    //    for (int y = mapHeight - 1; y >= 0; y--)
//    //    {
//    //        if (map[x, y] == 0 || map[x, y] == 4)
//    //        {
//    //            lightMap[x, y] = sunlightIntensity;
//    //        }
//    //        else
//    //        {
//    //            break; // твёрдый блок – свет больше не проходит
//    //        }
//    //    }
//    //}

//    public static void DebugPrintLightMap()
//    {
//        for (int y = 0; y < mapHeight; y++)
//        {
//            string line = "";
//            for (int x = 0; x < mapWidth; x++)
//            {
//                line += lightMap[x, y].ToString("F2") + " "; // Печать значений света в виде числа с 2 знаками после запятой
//            }
//            Debug.Log(line);
//        }
//    }
//    public static void RemoveLightAround(int x, int y)
//    {
//        int radius = 8;

//        for (int dx = -radius; dx <= radius; dx++)
//        {
//            for (int dy = -radius; dy <= radius; dy++)
//            {
//                int nx = x + dx;
//                int ny = y + dy;

//                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
//                {
//                    lightMap[nx, ny] = 0f;
//                }
//            }
//        }
//    }

//}




























































public class ProceduralGeneration : MonoBehaviour
{
    public static ProceduralGeneration instance;

    [Header("Noise settings")]
    public float smoothes = 10f;
    public float cavessmothes = 10f;
    public float stonesmothes = 10f;
    public float biomeSmoothes = 0.005f;

    [Header("Ore settings")]
    public float ironOre = 15f;
    public float coalOre = 20f;
    public float teleportiumOre = 25f;

    [Header("World Seed")]
    public int worldSeed = 1337;

    [Header("Biomes")]
    public List<BiomeRange> biomeRanges;

    [System.Serializable]
    public class BiomeRange
    {
        public string biomeName;
        public float minThreshold;
        public float maxThreshold;
    }

    void Awake()
    {
        instance = this;
        UnityEngine.Random.InitState(worldSeed);
    }

    /// <summary>
    /// Главная точечная функция генерации мира.
    /// </summary>
    public static int GenerateTileAt(int gx, int gy, out int bgTile)
    {
        bgTile = 0;

        var biome = GetBiomeAt(gx);

        int terrainHeight = Mathf.RoundToInt(Mathf.PerlinNoise(gx / instance.smoothes / 2f, instance.worldSeed + 5000) * 100f) + 50;
        int stoneHeight = Mathf.RoundToInt(Mathf.PerlinNoise(gx / instance.stonesmothes / 0.5f, instance.worldSeed * 3) * 100f) + 33;

        if (gy < stoneHeight + 5)
        {
            bgTile = 3;
            float oreIron = Mathf.PerlinNoise((gx + instance.worldSeed / 2f) / instance.ironOre, (gy + instance.worldSeed / 2f) / instance.ironOre);
            if (oreIron > 0.8f && biome == HelperClass.Biomes.Forest)
                return 6;

            float oreCoal = Mathf.PerlinNoise((gx + instance.worldSeed / 4f) / instance.coalOre, (gy + instance.worldSeed / 4f) / instance.coalOre);
            if (oreCoal > 0.8f)
                return 17;

            float oreTeleportium = Mathf.PerlinNoise((gx + instance.worldSeed) / instance.teleportiumOre / 0.9f, (gy + instance.worldSeed) / instance.teleportiumOre / 0.9f);
            if (oreTeleportium > 0.87f && biome == HelperClass.Biomes.Crystal)
                return 7;

            float moss = Mathf.PerlinNoise((gx + 42) / 12f, (gy + 42) / 10f);
            if (gy > stoneHeight && moss > 0.15f && moss < 0.2f)
            {
                bgTile = 12;
                return 4;
            }
        }

        float cave = Mathf.PerlinNoise((gx + instance.worldSeed) / instance.cavessmothes, (gy + instance.worldSeed) / instance.cavessmothes);
        if (gy < terrainHeight)
        {

            if (gy < stoneHeight)
                bgTile = 3;
            else bgTile = 1;

            if (cave < 0.4f)
            {
                return 4;
            }
            else
            {

                if (gy < stoneHeight)
                {
                    bgTile = 3;
                    return GetStoneBlock(biome);
                }

                if (gy < terrainHeight - 1)
                {
                    bgTile = 1;
                    return GetDirtBlock(biome);
                }

                if (gy == terrainHeight - 1)
                {
                    bgTile = 2;
                    return GetSurfaceBlock(biome);
                }
            }
        }
        

        

        

        return 0;
    }

    static HelperClass.Biomes GetBiomeAt(int gx)
    {
        float noise = Mathf.PerlinNoise(gx * instance.biomeSmoothes, 0);
        foreach (var range in instance.biomeRanges)
        {
            if (noise >= range.minThreshold && noise < range.maxThreshold)
                return (HelperClass.Biomes)Enum.Parse(typeof(HelperClass.Biomes), range.biomeName);
        }
        return HelperClass.Biomes.Forest; // fallback
    }

    static int GetSurfaceBlock(HelperClass.Biomes biome)
    {
        return biome switch
        {
            HelperClass.Biomes.Forest => 2,
            HelperClass.Biomes.Desert => 9,
            HelperClass.Biomes.Crystal => 10,
            HelperClass.Biomes.Snow => 11,
            _ => 2,
        };
    }

    static int GetDirtBlock(HelperClass.Biomes biome)
    {
        return biome switch
        {
            HelperClass.Biomes.Forest => 1,
            HelperClass.Biomes.Desert => 9,
            HelperClass.Biomes.Crystal => 10,
            HelperClass.Biomes.Snow => 1,
            _ => 1,
        };
    }

    static int GetStoneBlock(HelperClass.Biomes biome)
    {
        return biome switch
        {
            HelperClass.Biomes.Forest => 3,
            HelperClass.Biomes.Desert => 9,
            HelperClass.Biomes.Crystal => 10,
            HelperClass.Biomes.Snow => 3,
            _ => 3,
        };
    }
}


