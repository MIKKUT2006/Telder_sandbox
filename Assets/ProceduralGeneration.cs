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
    // ТЕСТЫ
    public static int X, Y;
    [System.Serializable]
    public class Column
    {
        public bool[] rows = new bool[Y];
    }

    public Column[] columns = new Column[X];
    // ТЕСТЫ

    // Префаб для чанка
    [SerializeField] public GameObject chunkPrefab;
    [SerializeField] public GameObject lightchunkPrefab;
    [SerializeField] public GameObject bgchunkPrefab;
    [SerializeField] public GameObject grasschunkPrefab;

    // Список блоков в структуре
    [Header("генерация структур")]
    [SerializeField] public Tilemap testStructure;
    [SerializeField] public Tilemap mapleHouse;

    [Header("Размеры мира")]
    [SerializeField] public int height = 100;       // высота (мира)
    [SerializeField] public int width = 200;        // Ширина (мира)

    [Header("Мягкость земли, пещер, камня")]
    [SerializeField] float smoothes;                // Мягкость
    [SerializeField] float cavessmothes;            // Мягкость Пещер
    [SerializeField] float stonesmothes;            // Мягкость Камня
    [SerializeField] float biomeSmoothes;           // Мягкость Биома пустынм

    [Header("Мягкость генерации руд")]
    [SerializeField] float ironOre;                 // Мягкость железной руды
    [SerializeField] float coalOre;                 // Мягкость угольной руды
    [SerializeField] float teleportiumOre;          // Мягкость камня пространства

    [SerializeField] float seed;                    // Сид мира
    [SerializeField] List<TileBase> groundTile;     // Тайл
    [SerializeField] public static List<TileBase> lightTiles;            // Тайл освещения
    [SerializeField] List<TileBase> lightTilesInspector;            // Тайл освещения
    [SerializeField] Tilemap tilemap;               // Карта тайлов
    [SerializeField] Tilemap bgTilemap;             // Карта тайлов заднего фона
    [SerializeField] Tilemap lightTilemap;          // Карта тайлов для освещения
    [SerializeField] Tilemap grassTilemap;          // Карта тайлов растительности

    [SerializeField] Cell cell;

    [SerializeField] public static int[,] map;      // Двумерный массив карты
    [SerializeField] public static int[,] lightMap;      // Двумерный массив карты
    [SerializeField] public static int[,] bgMap;    // Двумерный массив карты заднего плана

    //private enum Biomes { Desert, Forest, Crystal, None }
    //private Biomes[] biomeMap;


    [SerializeField] GameObject mainTilemap;
    [SerializeField] Tilemap testhouse;

    [SerializeField] List<GameObject> Trees;

    // 0 = солнечный свет
    // 1 = трава
    // 2 = земля
    // 3 = камень
    // 4 = пустота
    // 5 = трава с деревьями
    // 1 = трава
    // 1 = трава
    // 1 = трава

    // Для проверки работы шума перлина
    public int x = 0, y = 0;
    public int worldSeed;

    //[Header("Освещение")]
    //public static Texture2D worldTilesMap;
    //public Texture2D worldTilesMapInspector;
    //public Material lightShader;
    //public float lightThreshold;
    //public static float lightRadius = 7f;
    static List<Vector2Int> unlitBlocks = new List<Vector2Int>();

    [Header("Освещение")]
    public Material lightShader;
    public static float lightRadius = 7f;

    // Используем двумерный массив для хранения данных освещения.
    public static float[,] lightMapFloatArray;
    public static int mapWidth, mapHeight;
    public static Texture2D worldTilesMap;
    public Texture2D worldTilesMapInspector;
    //private static bool isUpdateLightMap = true;
    public static ProceduralGeneration instance;

    void Awake()
    {
        instance = this;
        //worldTilesMap = worldTilesMapInspector;
        HelperClass.worldWidth = width;
        HelperClass.worldHeight = height;

        

        lightTiles = lightTilesInspector;

        //worldTilesMap = new Texture2D(width, width);// Создаем текстуру освещения
        //worldTilesMap.filterMode = FilterMode.Point;// Устанавливаем фильтр освещения
        //lightShader.SetTexture("_shadowTexture", worldTilesMap);// Устанавливаем текстуру на шейдер
        //// Заполняем текстуру светом
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        worldTilesMap.SetPixel(x, y, Color.white);
        //    }
        //}

        //worldTilesMap.Apply();

        mapWidth = width;
        mapHeight = height;
        worldTilesMap = new Texture2D(mapWidth, mapWidth);
        //worldTilesMap.filterMode = FilterMode.Point;
        lightShader.SetTexture("_shadowTexture", worldTilesMap);
        lightMapFloatArray = new float[mapWidth, mapHeight];

        // Устанавливаем начальную темноту
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                lightMapFloatArray[x, y] = 0f;
            }
        }

        //UpdateTextureFromLightmap(); // Обновляем текстуру после установки темноты




        //HelperClass.Cells = new Cell[HelperClass.worldWidth, HelperClass.worldWidth];

        HelperClass.biomeMap = new HelperClass.Biomes[width];

        //cell = new Cell();

        // Создаём чанки
        CreateChunks();
        // Проверяем на создание нового мира или загрузуку существующего
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

        RenderMap(map, tilemap, groundTile, bgMap);             // Показываем изменения
        //GenerateLightMapTEST(map, width, height);

        //Grid.CreateGrid();

        

        HelperClass.chunkPrefab = chunkPrefab;
        HelperClass.lightchunkPrefab = lightchunkPrefab;
        HelperClass.bgchunkPrefab = bgchunkPrefab;
        HelperClass.grasschunkPrefab = grasschunkPrefab;

        worldTilesMapInspector = worldTilesMap;

        // Initialize lightMap when the world is generated
        //mapWidth = worldTilesMap.width;
        //mapHeight = worldTilesMap.height;
        //lightMapFloatArray = new float[mapWidth, mapHeight];
        ////Initial lightmap fill - optional, could also be done on demand
        //UpdateLightmapFromTexture();
        UpdateTextureFromLightmap();
    }
    void Generation()
    {
        // Что-то со светом
        //for (int x = 0; x < width; x++)
        //{
        //    for (int y = 0; y < height; y++)
        //    {
        //        if (worldTilesMap.GetPixel(x, y) == Color.white)
        //        {
        //            //LightBlock(x, y, 1f, 0);
        //            LightBlock(x, y, 1f);
        //        }
        //    }
        //}
        //worldTilesMap.Apply();



        bgMap = GenerateArray(width, height);       // Генерируем массив
        lightMap = GenerateArray(width, height);       // Генерируем массив
        map = GenerateArray(width, height);        // Генерируем массив

        lightTilemap.ClearAllTiles();                           // Очищаем все тайлы перед генерацией

        // Основной план
        tilemap.ClearAllTiles();                                // Очищаем все тайлы перед генерацией
        BiomeGeneration();                                      // Генерируем биомы
        map = TerrainGeneration(map);                           // Генерируем мир
        map = StoneGeneration(map);                             // Генерируем камень
        map = CavesGeneration(map, bgMap).Item1;                // Генерируем пещеры
        map = BarrierGeneration(map);
        DestroyStructures();
        // Задниий план
        bgTilemap.ClearAllTiles();                              // Очищаем все тайлы перед генерацией

        bgMap = TerrainGeneration(bgMap);                       // Генерируем мир
        bgMap = StoneGeneration(bgMap);                         // Генерируем мир
        bgMap = GrassGeneration(bgMap);                         // Генерируем мир
        bgMap = CavesGeneration(map, bgMap).Item2;              // Генерируем пещеры

        map = TreesGeneration(map);
        //StructuresGeneration(testStructure);
        StructuresGeneration(testStructure, 12);
        StructuresGeneration(mapleHouse, 2);

        worldTilesMap.Apply();

        Debug.Log("Всё готово");
    }

    public void CreateChunks()
    {
        int chunkSize = HelperClass.chunkSize;
        int numChunksX = Mathf.CeilToInt((float)width / chunkSize); // Количество чанков по X

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
            int index = x; // Индекс чанка

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

    // Создаём массив размерами мира, указывая, что у него есть тайлы (создано для разных размеров миров)
    // Чтобы не создавать вручную весь массив
    public int[,] GenerateArray(int width, int height)
    {
        int[,] array = new int[width, height];                // Устанавливаем размеры мира

        // Генерируем мир по ширине
        for (int i = 0; i < width; i++)
        {
            // Генерируем мир по высоте
            for (int j = 0; j < height; j++)
            {
                array[i, j] = 0;
            }
        }
        return array;
    }
    //-----
    // как рабоает генерация: мы получаем от формулы шума перлина высоту, до которой генерируется земля, всё, что выше = 0
    // Как сгенерировали один столбик, переходит к следующему
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
    //    float biomeNoiseScale = 0.005f; // Масштаб шума для биомов

    //    // Генерирование биомов
    //    for (int x = 0; x < width; x++)
    //    {
    //        // Генерация биома для текущего столбца
    //        float biomeValue = Mathf.PerlinNoise(x * biomeNoiseScale, 0);
    //        Debug.Log(biomeValue);
    //        if (biomeValue < 0.44f)
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Desert; // Пустыня
    //        }
    //        else if (biomeValue < 0.66f)
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Forest; // Лес
    //        }
    //        else
    //        {
    //            HelperClass.biomeMap[x] = HelperClass.Biomes.Crystal; // Кристалл
    //        }
    //    }
    //}
    // ГЕНЕРАЦИЯ БИОМОВ
    private int biomeWidth = HelperClass.worldWidth; // Ширина карты биомов

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
        biomeWidth = width; // Ширина карты биомов
        float biomeNoiseScale = 0.005f; // Масштаб шума для биомов

        // Проверка на правильность настроек биомов
        biomeRanges.Sort((a, b) => a.minThreshold.CompareTo(b.minThreshold)); // Сортируем по минимальному порогу
        for (int i = 0; i < biomeRanges.Count - 1; i++)
        {
            if (biomeRanges[i].maxThreshold > biomeRanges[i + 1].minThreshold)
            {
                Debug.LogError("Ошибка в настройках биомов: Пересечение порогов!");
                return;
            }
        }


        HelperClass.biomeMap = new HelperClass.Biomes[biomeWidth]; // Инициализируем массив

        // Генерирование биомов
        for (int x = 0; x < biomeWidth; x++)
        {
            float biomeValue = Mathf.PerlinNoise(x * biomeNoiseScale, 0);

            // Находим биом, соответствующий значению шума
            HelperClass.Biomes biome = FindBiome(biomeValue);
            if (biome == HelperClass.Biomes.Crystal)
            {
                Debug.Log("ДА");
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
        // Если значение не попадает ни в один диапазон, вернуть биом по умолчанию (или обработать ошибку)
        Debug.LogError("Значение шума не соответствует ни одному биому: " + value);
        return HelperClass.Biomes.Desert; // Биом по умолчанию
    }
    // ГЕНЕРАЦИЯ БИОМОВ


    public int[,] TerrainGeneration(int[,] map)     // Генерация земли
    {
        int perlinHeight;           // Высота перлина
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

        //// Дополнительные горы
        for (int i = 0; i < width; i++)
        {
            // Получаем координату чанка
            int chunkCoord = i / HelperClass.chunkSize;   // Получаем координату чанка

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

    public int[,] StoneGeneration(int[,] map)     // Генерация каменного слоя
    {
        int perlinHeight;   // Высота перлина
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

    public (int[,], int[,]) CavesGeneration(int[,] map, int[,] bgMap)     // Генерация пещер
    {
        int perlinHeightStone;   // Высота перлина
        float perlinHeightCaves;   // Высота перлина
        float perlinHeightGround;   // Высота перлина поверхностных пещер
        float perlinHeightOres;   // Высота перлина руд
        float perlinHeightTeleportium;
        float cavesSeed = Random.Range(0, 100);

        Debug.Log(bgMap);
        for (int i = 0; i < width; i++)
        {
            perlinHeightStone = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
            perlinHeightStone += height / 3;

            for (int j = 0; j < height; j++)
            {
                // Пещеры
                perlinHeightCaves = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
                if (perlinHeightCaves < 0.4 && (map[i, j] == 3 || map[i, j] == 9 || map[i, j] == 10))
                {
                    map[i, j] = 4;
                }

                // Пещеры на поверхности
                perlinHeightGround = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes / 2, (j + HelperClass.worldSeed) / cavessmothes / 2);
                if (perlinHeightGround < 0.4 && (map[i, j] == 1 || map[i, j] == 2 || map[i, j] == 9 || map[i, j] == 10 || map[i, j] == 11) && j > perlinHeightStone - 1)
                {
                    map[i, j] = 4;
                }

                // Замшелые пещеры
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

                // Генерация железной руды
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 2) / ironOre, (j + HelperClass.worldSeed / 2) / ironOre);
                //Debug.Log(perlinHeightOres);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 6;
                }

                // Генерация угольной руды
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 4) / coalOre, (j + HelperClass.worldSeed / 4) / coalOre);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 17;
                }

                // Генерация руды телепортации
                perlinHeightTeleportium = Mathf.PerlinNoise((i + HelperClass.worldSeed) / teleportiumOre / 0.9f, (j + HelperClass.worldSeed) / teleportiumOre / 0.9f);

                //perlinHeightTeleportium = Mathf.RoundToInt(Mathf.PerlinNoise(j / stonesmothes, seed * 3) * height / 2.1f);
                //Debug.Log(perlinHeightTeleportium);
                if (perlinHeightTeleportium > 0.87 && HelperClass.biomeMap[i] == HelperClass.Biomes.Crystal && map[i, j] == 10)
                {
                    map[i, j] = 7;
                }
            }
        }
        UpdateTextureFromLightmap();
        return (map, bgMap);
    }

    public int[,] TreesGeneration(int[,] map)     // Генерация деревьев
    {
        float perlinHeight;   // Высота перлина

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

    public int[,] GrassGeneration(int[,] map)     // Генерация пушистой травы
    {
        float perlinHeight;   // Высота перлина

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
        //int perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            // Получаем координату чанка
            int chunkCoord = i / HelperClass.chunkSize;   // Получаем координату чанка
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

                            //AllItemsAndBlocks allItemsAndBlocks = BlocksData.allBlocks.Where(x => x.imagePath.Split('/')[x.imagePath.Split('/').Length-1] == tile.name).FirstOrDefault();
                            //if (allItemsAndBlocks != null)
                            //{
                            //    map[x, y] = allItemsAndBlocks.blockIndex;
                            //}

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
    }

    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // Массив, тайлмап, тайлы блока (список блоков)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int chunkSize = HelperClass.chunkSize;

        for (int i = 0; i < width; i++)
        {
            // Получаем координату чанка
            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);

            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap bgTileMap = HelperClass.bgChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap grassTileMap = HelperClass.grassChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            for (int j = 0; j < height; j++)
            {

                switch (map[i, j])
                {
                    case 0:
                        //LightBlock(x, y, 1f, 0);
                        //worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
                        LightBlock(i, j, 1);
                        break;
                    case 1:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
                        //LightBlock(i, j, 1);
                        break;
                    case 2:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // Устанавливаем тайл травы
                        //LightBlock(i, j, 1);
                        break;
                    case 3:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                        //LightBlock(i, j, 1);
                        break;
                    case 4:
                        RemoveLightSource(i, j);
                        break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 5, 0);
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // Устанавливаем деревья
                        RemoveLightSource(i, j);
                        Instantiate(Trees[(int)UnityEngine.Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
                        break;
                    case 6:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // Устанавливаем тайл железной руды
                        RemoveLightSource(i, j);
                        break;
                    case 7:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[5]);       // Устанавливаем тайл руды камня пространства
                        RemoveLightSource(i, j);
                        break;
                    case 8:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[7]);       // Устанавливаем тайл барьера
                        RemoveLightSource(i, j);
                        break;
                    case 9:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // Устанавливаем тайл песка
                        RemoveLightSource(i, j);
                        break;
                    case 10:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // Устанавливаем тайл песка
                        RemoveLightSource(i, j);
                        break;
                    case 11:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[10]);       // Устанавливаем тайл снега
                        RemoveLightSource(i, j);
                        break;
                    case 12:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // Устанавливаем тайл мха
                        RemoveLightSource(i, j);
                        break;
                    case 17:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[12]);       // Устанавливаем тайл угля
                        RemoveLightSource(i, j);
                        break;
                }
                
                HelperClass.Chunks[chunkCoord] = tileMap;
                if (bgMap[i, j] == 0)
                {
                    worldTilesMap.SetPixel(i, j, Color.white);
                }
                if (bgMap[i, j] == 1)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // Устанавливаем тайл земли
                    RemoveLightSource(i, j);
                }
                if (bgMap[i, j] == 2)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
                    RemoveLightSource(i, j);
                }
                if (bgMap[i, j] == 3)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                    RemoveLightSource(i, j);
                }
                if (bgMap[i, j] == 8)
                {
                    grassTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // Устанавливаем тайл пушистой травы
                }
                if (bgMap[i, j] == 9)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[8]);       // Устанавливаем тайл камня
                    RemoveLightSource(i, j);
                }
                if (bgMap[i, j] == 10)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[9]);       // Устанавливаем тайл камня
                    RemoveLightSource(i, j);
                }
                if (bgMap[i, j] == 12)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[3]);       // Устанавливаем тайл мха
                    RemoveLightSource(i, j);
                }
            }
        }

        // заполняем освещение
        for (int x = 0; x < width; x++) 
        {
            for (int y = 0; y < height; y++) 
            {
                if (map[x,y] == 0)
                {
                    //LightBlock(x, y, 1f, 0);
                    LightBlock(x, y, 1f);
                    worldTilesMap.SetPixel(x, y, UnityEngine.Color.white);
                }
            }
        }
        worldTilesMap.Apply();
    }
    //public void GenerateLightMap(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // Перебираем столбцы (высоту)
    //    {
    //        for (int i = 0; i < width; i++) // Перебираем строки (ширину)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            // Если блок - источник света, ставим обычный свет и запускаем "заливку"
    //            if (map[i, j] == 0)
    //            {
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //            }
    //            else
    //            {
    //                bool isShadow = FillLight(map, lightTileMap, i, j, width, height, 1, tileMap); // Начинаем с уровня тусклости 1
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
    //    // Проверка границ
    //    if (x < 0 || x >= width || y < 0 || y >= height) return false;

    //    // Если это блок или источник света, то останавливаемся
    //    //if (map[x, y] == 0) return;
    //    if (tileMap.GetTile(new Vector3Int(x, y)) == null)
    //    {
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTiles[0]);
    //        return false;
    //    }
    //    // Выбираем тайл по уровню света
    //    TileBase lightTile = null;
    //    if (lightLevel == 1) lightTile = lightTiles[1];
    //    else if (lightLevel == 2)
    //    {
    //        lightTile = lightTiles[2];
    //    }
    //    else if (lightLevel > 2)
    //    {
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // Если свет дальше 3 блоков, то убираем его
    //        return true;
    //    }

    //    // Если тайл выбран, ставим его
    //    if (lightTile != null)
    //        lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTile);


    //    // Рекурсивно распространяем свет вниз (тут важный момент, ставим свет **только** если блок снизу пустой).
    //    FillLight(map, lightTileMap, x, y - 1, width, height, lightLevel + 1, tileMap);
    //    return false;
    //}

    //public void GenerateLightMap(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // Перебираем столбцы (высоту)
    //    {
    //        for (int i = 0; i < width; i++) // Перебираем строки (ширину)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

    //            // Если блок - источник света, ставим обычный свет и запускаем "заливку"
    //            if (map[i, j] == 0)
    //            {
    //                Debug.Log("GOIDA");
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //                FillLight(tileMap, lightTileMap, i, j, width, height, 1);
    //            }
    //        }
    //    }
    //}
    //public void GenerateLightMap(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // Перебираем столбцы (высоту)
    //    {
    //        for (int i = 0; i < width; i++) // Перебираем строки (ширину)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

    //            // Если блок - источник света, ставим обычный свет и запускаем "заливку"
    //            if (map[i, j] == 0)
    //            {
    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //                FillLight(tileMap, lightTileMap, i, j, width, height);
    //            }
    //        }
    //    }
    //}

    //public void GenerateLightMapTEST(int[,] map, int width, int height)
    //{
    //    for (int j = 0; j < height; j++) // Перебираем столбцы (высоту)
    //    {
    //        for (int i = 0; i < width; i++) // Перебираем строки (ширину)
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
    //                Debug.Log("Значение больше высоты");
    //            }
                
    //        }
    //    }
    //}

    //public void GenerateLightMapTEST(int[,] map, int width, int height)
    //{
    //    for (int i = 0; i < width; i++)
    //    {
    //        for (int j = height - 1; j > 0; j--)
    //        {
    //            int chunkCoord = ChunkHelper.GetChunkXCoordinate(i);
    //            Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();
    //            Tilemap tileMap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

    //            lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //            // Устанавливаем тайл света
    //            Debug.Log($"{i}S{j}");
    //            lightMap[i, j] = 1;
    //            if (tileMap.GetTile(new Vector3Int(i, j, 0)) == null)
    //            {

    //                lightTileMap.SetTile(new Vector3Int(i, j, 0), lightTiles[0]);
    //                if (j + 1 < height)
    //                {

    //                    if (tileMap.GetTile(new Vector3Int(i, j - 1, 0)) != null)
    //                    {
    //                        lightTileMap.SetTile(new Vector3Int(i, j - 1, 0), lightTiles[1]);
    //                        if (tileMap.GetTile(new Vector3Int(i, j - 2, 0)) != null)
    //                        {
    //                            lightTileMap.SetTile(new Vector3Int(i, j - 2, 0), lightTiles[2]);
    //                            if (tileMap.GetTile(new Vector3Int(i, j - 3, 0)) != null)
    //                            {
    //                                lightTileMap.SetTile(new Vector3Int(i, j - 3, 0), lightTiles[3]);
    //                                if (tileMap.GetTile(new Vector3Int(i, j - 4, 0)) != null)
    //                                {
    //                                    lightTileMap.SetTile(new Vector3Int(i, j - 4, 0), lightTiles[4]);
    //                                    break;
    //                                }
    //                                break;
    //                            }
    //                            break;
    //                        }
    //                        break;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    //public static void LightBlock(int x, int y, float intensity, int iteration)
    //{
    //    if (iteration < lightRadius)
    //    {
    //        worldTilesMap.SetPixel(x, y, Color.white * intensity);

    //        for (int nx = x - 1; nx < x + 2; nx++) 
    //        {
    //            for (int ny = y-1; ny < y + 2; ny++) 
    //            {
    //                if (nx != x || ny != y)
    //                {
    //                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
    //                    float targetIntensity = Mathf.Pow(0.7f, dist) * intensity;

    //                    if (worldTilesMap.GetPixel(nx,ny) != null)
    //                    {
    //                        if (worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
    //                        {
    //                            LightBlock(nx, ny, targetIntensity, iteration + 1);   
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        worldTilesMap.Apply();
    //    }
    //}


    // Итеративный алгоритм распространения света
    public static void LightBlock(int x, int y, float intensity)
    {
        //Check bounds
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return;
        //Use a queue for non-recursive light propagation
        Queue<(int x, int y, float intensity)> queue = new Queue<(int, int, float)>();
        queue.Enqueue((x, y, intensity));

        while (queue.Count > 0)
        {
            (int curX, int curY, float curIntensity) = queue.Dequeue();

            //Check bounds again
            if (curX < 0 || curX >= mapWidth || curY < 0 || curY >= mapHeight) continue;

            if (lightMapFloatArray[curX, curY] < curIntensity)
            {
                lightMapFloatArray[curX, curY] = curIntensity;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip self

                        int nx = curX + dx;
                        int ny = curY + dy;

                        //Check bounds
                        if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                        {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy); //Simplified distance calculation
                            float targetIntensity = Mathf.Pow(0.7f, dist) * curIntensity;
                            queue.Enqueue((nx, ny, targetIntensity));
                        }
                    }
                }
            }
        }

    }

    // Обновляем текстуру света из массива
    public static void UpdateTextureFromLightmap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                worldTilesMap.SetPixel(x, y, new Color(lightMapFloatArray[x, y], lightMapFloatArray[x, y], lightMapFloatArray[x, y]));
            }
        }
        worldTilesMap.Apply();
    }

    public static void RemoveLightSource(int x, int y)
    {
        LightBlock(x, y, 0f);
    }

    public void UpdateLightOnBlockChange(int x, int y)
    {
        int range = Mathf.CeilToInt(lightRadius);
        Debug.Log(range);
        for (int i = x - range; i <= x + range; i++)
        {
            for (int j = y - range; j <= y + range; j++)
            {
                if (i >= 0 && i < mapWidth && j >= 0 && j < mapHeight)
                {
                    LightBlock(i, j, 0);
                }
            }
        }
        UpdateTextureFromLightmap();
    }

    //public void PlaceBlock(int x, int y, int tileType) //tileType - id блока
    //{
    //    // Обновляем массив карты
    //    if (x < 0 || x >= width || y < 0 || y >= height) return;
    //    map[x, y] = tileType;
    //    Debug.Log($"Block placed at ({x}, {y}) with type: {tileType}.");

    //    // Вызываем пересчет освещения
    //    UpdateLightOnBlockChange(x, y);
    //}

    //public static void RemoveLightSource(int x, int y)
    //{
    //    unlitBlocks.Clear();
    //    UnLightBlock(x, y, x, y);

    //    List<Vector2Int> toRelight = new List<Vector2Int>();

    //    foreach (Vector2Int block in unlitBlocks)
    //    {
    //        for (int nx = block.x - 1; nx < block.x + 2; nx++)
    //        {
    //            for (int ny = block.y - 1; ny < block.y + 2; ny++)
    //            {
    //                if (worldTilesMap.GetPixel(nx, ny) != null)
    //                {
    //                    if (worldTilesMap.GetPixel(nx,ny).r > worldTilesMap.GetPixel(block.x, block.y).r)
    //                    {
    //                        if (!toRelight.Contains(new Vector2Int(nx, ny)))
    //                        {
    //                            toRelight.Add(new Vector2Int(nx, ny));
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    foreach (Vector2Int source in toRelight)
    //    {
    //        LightBlock(source.x, source.y, worldTilesMap.GetPixel(source.x, source.y).r, 0);
    //    }

    //    worldTilesMap.Apply();
    //}

    //public static void RemoveLightSource(int x, int y)
    //{
    //    // Simple approach: relight the whole area.
    //    // For better optimization, implement a more sophisticated change propagation algorithm.
    //    LightBlock(x, y, 0f); //Set to 0
    //}

    //public static void UnLightBlock(int x, int y, int ix, int iy)
    //{
    //    if (Mathf.Abs(x - ix) >= lightRadius || Mathf.Abs(y - iy) >= lightRadius || unlitBlocks.Contains(new Vector2Int(x, y)))
    //    {
    //        return;
    //    }

    //    for (int nx = x-1; nx < x + 2; nx++)
    //    {
    //        for (int ny = y-1; ny < y + 2; ny++) 
    //        {
    //            if (nx != x || ny != y)
    //            {
    //                if (worldTilesMap.GetPixel(nx, ny) != null)
    //                {
    //                    if (worldTilesMap.GetPixel(nx,ny).r < worldTilesMap.GetPixel(x,y).r)
    //                    {
    //                        UnLightBlock(nx, ny, ix, iy);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    worldTilesMap.SetPixel(x, y, Color.black);
    //    unlitBlocks.Add(new Vector2Int(x, y));
    //}

    private void FillLight(Tilemap tileMap, Tilemap lightTileMap, int startX, int startY, int width, int height)
    {
        Queue<(int x, int y, int lightLevel)> queue = new Queue<(int x, int y, int lightLevel)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>(); // Используем HashSet
        queue.Enqueue((startX, startY, 1)); // Начинаем с уровня тусклости 1
        visited.Add((startX, startY)); // Помечаем стартовый блок как посещенный

        while (queue.Count > 0)
        {
            (int x, int y, int lightLevel) = queue.Dequeue();

            // Проверка границ
            if (x < 0 || x >= width || y < 0 || y >= height) continue;


            // Выбираем тайл по уровню света
            TileBase lightTile = null;
            //if (lightLevel == 1) lightTile = lightTiles[1];
            //else if (lightLevel == 2) lightTile = lightTiles[2];
            //else if (lightLevel > 2)
            //{
            //    lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // Если свет дальше 3 блоков, то убираем его
            //    continue;
            //}

            if (lightLevel == 1) lightTile = lightTiles[0];
            else if (lightLevel == 2 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[1];
            else if (lightLevel == 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null) lightTile = lightTiles[2];
            else if (lightLevel > 3 && tileMap.GetTile(new Vector3Int(x, y, 0)) != null)
            {
                lightTileMap.SetTile(new Vector3Int(x, y, 0), null); // Если свет дальше 3 блоков, то убираем его
                continue;
            }


            // Проверка на наличие блока в TileBlocks
            if (tileMap.GetTile(new Vector3Int(x, y, 0)) == null)
            {
                if (lightTile != null)
                    lightTileMap.SetTile(new Vector3Int(x, y, 0), lightTile);

                // Добавляем соседние блоки в очередь, если они не были посещены
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

