using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;
using UnityEngine.WSA;
using static UnityEngine.Rendering.VolumeComponent;
using System.Drawing;

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

    [Header ("Мягкость генерации руд")]
    [SerializeField] float ironOre;                 // Мягкость железной руды
    [SerializeField] float teleportiumOre;          // Мягкость камня пространства

    [SerializeField] float seed;                    // Сид мира
    [SerializeField] List<TileBase> groundTile;     // Тайл
    [SerializeField] TileBase lightTile;            // Тайл освещения
    [SerializeField] Tilemap tilemap;               // Карта тайлов
    [SerializeField] Tilemap bgTilemap;             // Карта тайлов заднего фона
    [SerializeField] Tilemap lightTilemap;          // Карта тайлов для освещения

    [SerializeField] Cell cell;

    [SerializeField] public static int[,] map;      // Двумерный массив карты
    [SerializeField] public static int[,] bgMap;    // Двумерный массив карты заднего плана
    //[SerializeField] int[,] lightMap;             // Двумерный массив карты заднего плана

    

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

    // Для проверки работы шума перлина
    public int x = 0, y = 0;
    public int worldSeed;

    void Awake()
    {
        HelperClass.Cells = new Cell[HelperClass.worldWidth, HelperClass.worldWidth];

        cell = new Cell();

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

            Debug.Log(bgMap.GetLength(0));
        }

        RenderMap(map, tilemap, groundTile, bgMap);             // Показываем изменения
        LightGeneraion(map);

        Grid.CreateGrid();

        HelperClass.chunkPrefab = chunkPrefab;
        HelperClass.lightchunkPrefab = lightchunkPrefab;
        HelperClass.bgchunkPrefab = bgchunkPrefab;

        Debug.Log(Mathf.PerlinNoise((x + worldSeed) / cavessmothes, (y + worldSeed) / cavessmothes));
    }
    void Generation()
    {

        
        //lightMap = GenerateArray(width, height, true, true);    // Генерируем массив
        lightTilemap.ClearAllTiles();                           // Очищаем все тайлы перед генерацией

        // Основной план
        tilemap.ClearAllTiles();                                // Очищаем все тайлы перед генерацией
        map = GenerateArray(width, height, true, false);        // Генерируем массив
        map = TerrainGeneration(map);                           // Генерируем мир
        map = StoneGeneration(map);                             // Генерируем камень
        map = CavesGeneration(map);                             // Генерируем пещеры
        map = OresGeneration(map);                              // Генерируем руды
        map = BarrierGeneration(map);
        DestroyStructures();
        // Задниий план
        bgTilemap.ClearAllTiles();                              // Очищаем все тайлы перед генерацией
        bgMap = GenerateArray(width, height, true, true);       // Генерируем массив
        bgMap = TerrainGeneration(bgMap);                       // Генерируем мир
        bgMap = StoneGeneration(bgMap);                         // Генерируем мир
        bgMap = GrassGeneration(bgMap);                         // Генерируем мир

        map = TreesGeneration(map);
        //StructuresGeneration(testStructure);
        StructuresGeneration(testStructure, 12);
        StructuresGeneration(mapleHouse, 2);
        Debug.Log("Всё готово");
    }

    public void CreateChunks()                                  // Создание чанков
    {
        HelperClass.numChunks = width / HelperClass.chunkSize;  // Устанавливаем количество чанков

        HelperClass.Chunks = new Tilemap[HelperClass.numChunks];
        HelperClass.ChunksGameobject = new GameObject[HelperClass.numChunks];

        HelperClass.lightChunks = new Tilemap[HelperClass.numChunks];
        HelperClass.lightChunksGameobject = new GameObject[HelperClass.numChunks];

        HelperClass.bgChunks = new Tilemap[HelperClass.numChunks];
        HelperClass.bgChunksGameobject = new GameObject[HelperClass.numChunks];

        // Цикл на количество чанков
        for (int i = 0; i < HelperClass.numChunks; i++)
        {
            // Генерация чанков с пустыми чанками 1- и x+1 по краям локации

            //------------------
            Tilemap newChunk = new Tilemap();
            HelperClass.Chunks[i] = newChunk;
            GameObject Chunk = Instantiate(chunkPrefab);
            Chunk.name = i.ToString();
            Chunk.transform.parent = transform;
            HelperClass.ChunksGameobject[i] = Chunk;

            Tilemap newlightChunk = new Tilemap();
            HelperClass.lightChunks[i] = newlightChunk;
            GameObject lightChunk = Instantiate(lightchunkPrefab);
            lightChunk.name = i.ToString();
            lightChunk.transform.parent = transform;
            HelperClass.lightChunksGameobject[i] = lightChunk;

            GameObject bgChunk = Instantiate(bgchunkPrefab);
            HelperClass.bgChunks[i] = bgChunk.GetComponent<Tilemap>();
            bgChunk.name = i.ToString();
            bgChunk.transform.parent = transform;
            HelperClass.bgChunksGameobject[i] = bgChunk;
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
    public int[,] GenerateArray(int width, int height, bool useArray, bool bg)
    {
        int[,] map = new int[width, height];                // Устанавливаем размеры мира
        int[,] bgMap = new int[width, height];              // Устанавливаем размеры мира

        // Генерируем мир по ширине
        for (int i = 0; i < width; i++)
        {
            // Генерируем мир по высоте
            for (int j = 0; j < height; j++)
            {
                map[i, j] = (useArray) ? 0 : 1;             // Если в массиве карты false = 0, true = 1
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

    public int[,] TerrainGeneration(int[,] map)     // Генерация земли
    {
        int perlinHeight;   // Высота перлина
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
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed + height) * height / 2.5f);
            //Debug.Log(HelperClass.worldSeed);
            perlinHeight += height / 2;
            //perlinHeight = perlinHeight / 2;

            for (int j = 0; j <= perlinHeight + 1; j++)
            {

                if (j < perlinHeight)
                {
                    map[i, j] = 1;
                    // Устанавливаем твердый блок для физики воды
                    //HelperClass.Cells[i, j].SetType(CellType.Solid);
                    //cell.Set(i, j);
                    //cell.SetType(CellType.Solid);

                    //HelperClass.Cells[i, j] = cell;
                    ////HelperClass.Cells[100, 100].SetType(CellType.Solid);
                    ////HelperClass.Cells[120, 120].SetType(CellType.Solid);
                }

                if (j == perlinHeight)
                {
                    map[i, j] = 2;
                    //HelperClass.Cells[i, j].SetType(CellType.Solid);
                }

                //if (j > perlinHeight)
                //{
                //    map[i, j] = 0;
                //}
            }
        }

        // Дополнительные горы
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
                    map[i, j] = 1;
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

    public int[,] StoneGeneration(int[,] map)     // Генерация пещер
    {
        int perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, HelperClass.worldSeed * 3) * height / 2.3f);
            perlinHeight += height / 3;

            for (int j = 0; j <= perlinHeight; j++)
            {
                if (j < perlinHeight && map[i,j] == 1)
                {
                    map[i, j] = 3;
                }
            }
        }
        return map;
    }

    public int[,] CavesGeneration(int[,] map)     // Генерация пещер
    {
        float perlinHeightCaves;   // Высота перлина
        float perlinHeightGround;   // Высота перлина поверхностных пещер
        float perlinHeightOres;   // Высота перлина руд
        float perlinHeightTeleportium;
        float perlinHeightWaterCaves;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeightCaves = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes, (j + HelperClass.worldSeed) / cavessmothes);
                if (perlinHeightCaves < 0.4 && map[i, j] == 3)
                {
                    map[i, j] = 4;
                    //HelperClass.Cells[i, j].AddLiquid(5);
                    //lightMap[i, j] = 4;
                    //Debug.Log(lightMap[i, j]);
                }

                perlinHeightGround = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes / 2, (j + HelperClass.worldSeed) / cavessmothes / 2);
                if (perlinHeightGround < 0.4 && map[i, j] <= 2 && map[i, j] != 0)
                {
                    map[i, j] = 4;
                }

                // Генерация руды
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 2) / ironOre, (j + HelperClass.worldSeed / 2) / ironOre);
                //Debug.Log(perlinHeightOres);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 6;
                }

                // Генерация руды телепортации
                perlinHeightTeleportium = Mathf.PerlinNoise((i + HelperClass.worldSeed) / teleportiumOre / 0.5f, (j + HelperClass.worldSeed) / teleportiumOre / 0.5f);

                //perlinHeightTeleportium = Mathf.RoundToInt(Mathf.PerlinNoise(j / stonesmothes, seed * 3) * height / 2.1f);
                //Debug.Log(perlinHeightTeleportium);
                if (perlinHeightTeleportium > 0.87 && map[i, j] == 3)
                {
                    map[i, j] = 7;
                }

                // Генерация водных пещер
                //perlinHeightWaterCaves = Mathf.PerlinNoise((i + HelperClass.worldSeed) / cavessmothes / 2, (j + HelperClass.worldSeed)/ cavessmothes / 2);
                ////Debug.Log(perlinHeightOres);
                //if (perlinHeightWaterCaves > 0.8 && map[i, j] == 3)
                //{
                //    map[i, j] = 4;
                //}
            }
        }
        return map;
    }

    public int[,] OresGeneration(int[,] map)     // Генерация пещер
    {
        //float perlinHeight;   // Высота перлина
        //for (int i = 0; i < width; i++)
        //{
        //    for (int j = 0; j < height; j++)
        //    {
        //        perlinHeight = Mathf.PerlinNoise((i + seed) / ironOre, (j + seed) / ironOre);

        //        if (perlinHeight > 0.8 && map[i, j] == 3)
        //        {
        //            map[i, j] = 6;
        //        }
        //    }
        //}

        ////for (int i = 0; i < width; i++)
        ////{
        ////    for (int j = 0; j < height; j++)
        ////    {
        ////        perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes / 2, (j + seed) / cavessmothes / 2);

        ////        if (perlinHeight < 0.4 && map[i, j] <= 2)
        ////        {
        ////            map[i, j] = 4;
        ////        }
        ////    }
        ////}
        return map;
    }

    public int[,] TreesGeneration(int[,] map)     // Генерация и травы
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
                    //Debug.Log(map[i, j]);
                }
            }
        }
        return map;
    }

    public int[,] GrassGeneration(int[,] map)     // Генерация травы
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
            int structureCoordX = (int)Random.RandomRange(0, width);
            int structureCoordY = (int)Random.RandomRange(0, 100);
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
    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // Массив, тайлмап, тайлы блока (список блоков)
    {
        //lightTilemap = groundTilemap;

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
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // Устанавливаем тайл камня
                    //    break;
                    case 1:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
                        break;
                    case 2:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // Устанавливаем тайл травы

                        break;
                    case 3:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                        break;
                    //case 4:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // Устанавливаем тайл камня
                    //    break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 5, 0);
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // Устанавливаем деревья

                        Instantiate(Trees[(int)Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
                        break;
                    case 6:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // Устанавливаем тайл железной руды
                        break;
                    case 7:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[5]);       // Устанавливаем тайл руды камня пространства
                        break;
                    case 8:
                        tileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[7]);       // Устанавливаем тайл барьера
                        break;
                }

                HelperClass.Chunks[chunkCoord] = tileMap;

                if (bgMap[i, j] == 1)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // Устанавливаем тайл земли
                }
                if (bgMap[i, j] == 2)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
                }
                if (bgMap[i, j] == 3)
                {
                    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                }


                //if (bgMap[i, j] == 8)
                //{
                //    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // Устанавливаем тайл травы
                //}
            }
        }
    }
}
