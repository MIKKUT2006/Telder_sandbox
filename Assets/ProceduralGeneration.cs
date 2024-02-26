using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralGeneration : MonoBehaviour
{
    [SerializeField] int height, width;     // Ширина и высота (мира)
    [SerializeField] float smoothes;        // Мягкость
    [SerializeField] float cavessmothes;        // Мягкость Пещер
    [SerializeField] float ironOre;        // Мягкость железной руды


    [SerializeField] float seed;            // Сид мира
    [SerializeField] List<TileBase> groundTile;   // Тайл
    [SerializeField] TileBase lightTile;
    [SerializeField] Tilemap tilemap;       // Карта тайлов
    [SerializeField] Tilemap bgTilemap;       // Карта тайлов заднего фона
    [SerializeField] Tilemap lightTilemap;       // Карта тайлов для освещения

    [SerializeField] int[,] map; // Двумерный массив карты
    [SerializeField] int[,] bgMap; // Двумерный массив карты заднего плана
    [SerializeField] int[,] lightMap; // Двумерный массив карты заднего плана

    [SerializeField] List<GameObject> Trees;

    // 1 = трава
    // 2 = земля
    // 3 = камень
    // 4 = пустота
    // 5 = трава с деревьями
    // 1 = трава
    // 1 = трава


    void Start()
    {
        Generation();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("h"))
        {
            Generation();
        }
    }
    void Generation()
    {


        lightMap = GenerateArray(width, height, true, true);   // Генерируем массив
        lightTilemap.ClearAllTiles();                    // Очищаем все тайлы перед генерацией

        // Основной план
        tilemap.ClearAllTiles();                    // Очищаем все тайлы перед генерацией
        map = GenerateArray(width, height, true, false);   // Генерируем массив
        map = TerrainGeneration(map);               // Генерируем мир
        map = StoneGeneration(map);               // Генерируем камень
        map = CavesGeneration(map);               // Генерируем пещеры
        map = OresGeneration(map);               // Генерируем руды
        DestroyStructures();
        // Задниий план
        bgTilemap.ClearAllTiles();                    // Очищаем все тайлы перед генерацией
        bgMap = GenerateArray(width, height, true, true);   // Генерируем массив
        bgMap = TerrainGeneration(bgMap);               // Генерируем мир
        bgMap = StoneGeneration(bgMap);               // Генерируем мир
        //bgMap = CavesGeneration(bgMap);               // Генерируем пещеры

        //map = StructuresGeneration(map);

        RenderMap(map, tilemap, groundTile, bgMap);        // Показываем изменения
    }

    void DestroyStructures()
    {
        List <GameObject> trees = GameObject.FindGameObjectsWithTag("tree").ToList();

        foreach (var item in trees)
        {
            Destroy(item);
        }
    }

    // Создаём массив размерами мира, указывая, что у него есть тайлы (создано для разных размеров миров)
    // Чтобы не создавать вручную весь массив
    public int[,] GenerateArray(int width, int height, bool useArray, bool bg)
    {
        int[,] map = new int[width, height]; // Устанавливаем размеры мира
        int[,] bgMap = new int[width, height]; // Устанавливаем размеры мира

        // Генерируем мир по ширине
        for (int i = 0; i < width; i++)
        {
            // Генерируем мир по высоте
            for (int j = 0; j < height; j++)
            {
                map[i, j] = (useArray) ? 0 : 1;     // Если в массиве карты false = 0, true = 1
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
    public int[,] TerrainGeneration(int[,] map)     // Генерация земли
    {
        int perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes, seed) * height / 2);
            perlinHeight += height / 2;

            for (int j = 0; j <= perlinHeight; j++)
            {
                if (j < perlinHeight)
                {
                    map[i, j] = 1;
                }

                if (j == perlinHeight)
                {
                    map[i, j] = 2;
                }
            }
            for (int g = perlinHeight; g < height; g++)
            {
                lightTilemap.SetTile(new Vector3Int(i, g, 0), lightTile);
            }
        }
            
        return map;
    }

    public int[,] StoneGeneration(int[,] map)     // Генерация пещер
    {
        int perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 0.5f, seed * 2) * height / 3);
            perlinHeight += height / 3;

            for (int j = 0; j <= perlinHeight; j++)
            {
                if (j < perlinHeight)
                {
                    map[i, j] = 3;
                }
            }
        }
        return map;
    }

    public int[,] CavesGeneration(int[,] map)     // Генерация пещер
    {
        float perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);

                if (perlinHeight < 0.4 && map[i, j] == 3)
                {
                    map[i, j] = 4;
                    //lightMap[i, j] = 4;
                    Debug.Log(lightMap[i, j]);
                }
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes / 2, (j + seed) / cavessmothes / 2);

                if (perlinHeight < 0.4 && map[i, j] <= 2)
                {
                    map[i, j] = 4;
                }
            }
        }
        return map;
    }

    public int[,] OresGeneration(int[,] map)     // Генерация пещер
    {
        float perlinHeight;   // Высота перлина
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + seed) / ironOre, (j + seed) / ironOre);

                if (perlinHeight > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 6;
                }
            }
        }

        //for (int i = 0; i < width; i++)
        //{
        //    for (int j = 0; j < height; j++)
        //    {
        //        perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes / 2, (j + seed) / cavessmothes / 2);

        //        if (perlinHeight < 0.4 && map[i, j] <= 2)
        //        {
        //            map[i, j] = 4;
        //        }
        //    }
        //}
        return map;
    }

    //public int[,] StructuresGeneration(int[,] map)     // Генерация структур (деревья)
    //{
    //    float perlinHeight;   // Высота перлина

    //    //for (int i = 0; i < width; i++)
    //    //{
    //    //    for (int j = 0; j < height; j++)
    //    //    {
    //    //        perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);

    //    //        if (perlinHeight < 0.4 && map[i, j] == 3)
    //    //        {
    //    //            //Debug.Log("asdasdasd");
    //    //            map[i, j] = 4;
    //    //        }
    //    //    }
    //    //}

    //    for (int i = 0; i < width; i++)
    //    {
    //        for (int j = 0; j < height; j++)
    //        {
    //            perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);
    //            //Debug.Log(perlinHeight);

    //            if (perlinHeight < 0.4 && map[i, j] == 2)
    //            {
    //                map[i, j] = 5;
    //                Debug.Log(map[i, j]);
    //            }
    //        }
    //    }
    //    return map;
    //}

    // Расстановка тайлов в зависимости от сида
    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // Массив, тайлмап, тайлы блока (список блоков)
    {
        //lightTilemap = groundTilemap;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //Debug.Log(map[i, j]);

                switch (map[i, j])
                {
                    //case 0:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // Устанавливаем тайл камня
                    //    break;
                    case 1:
                        groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // Устанавливаем тайл земли
                        break;
                    case 2:
                        groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
                        break;
                    case 3:
                        groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                        break;
                    //case 4:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // Устанавливаем тайл камня
                    //    break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 3, 0);
                        groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // Устанавливаем деревья
                        Instantiate(Trees[0], pos, Quaternion.identity);
                        break;
                    case 6:
                        groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // Устанавливаем тайл железной руды
                        break;
                }
                //if (map[i,j] == 1)
                //{
                //}
                //if (map[i, j] == 2)
                //{
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
                //}
                //if (map[i, j] == 3)
                //{
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                //}
                //if (map[i, j] == 6)
                //{
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                //}
                // Деревья
                //if (map[i, j] == 5)
                //{
                //    Vector3 pos = new Vector3(i+0.5f, j+3, 0);
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тестовый тайл

                //    Instantiate(Trees[0], pos, Quaternion.identity);
                //}

                //if (lightMap[i, j] == 4)
                //{
                //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);
                //}

                if (bgMap[i, j] == 1)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // Устанавливаем тайл земли
                }
                if (bgMap[i, j] == 2)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // Устанавливаем тайл травы
                }
                if (bgMap[i, j] == 3)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // Устанавливаем тайл камня
                }


                
                //if (map[i, j] == 4)
                //{
                //    Debug.Log("asd");
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), null);       // Устанавливаем пустоту (пещеры)
                //}
            }
        }
    }
}
