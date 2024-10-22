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

    [Header ("�������� ��������� ���")]
    [SerializeField] float ironOre;                 // �������� �������� ����
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
    //[SerializeField] int[,] lightMap;             // ��������� ������ ����� ������� �����

    

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

    // ��� �������� ������ ���� �������
    public int x = 0, y = 0;
    public int worldSeed;

    void Awake()
    {
        HelperClass.Cells = new Cell[HelperClass.worldWidth, HelperClass.worldWidth];

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

            Debug.Log(bgMap.GetLength(0));
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

        
        //lightMap = GenerateArray(width, height, true, true);    // ���������� ������
        lightTilemap.ClearAllTiles();                           // ������� ��� ����� ����� ����������

        // �������� ����
        tilemap.ClearAllTiles();                                // ������� ��� ����� ����� ����������
        map = GenerateArray(width, height, true, false);        // ���������� ������
        map = TerrainGeneration(map);                           // ���������� ���
        map = StoneGeneration(map);                             // ���������� ������
        map = CavesGeneration(map);                             // ���������� ������
        map = OresGeneration(map);                              // ���������� ����
        map = BarrierGeneration(map);
        DestroyStructures();
        // ������� ����
        bgTilemap.ClearAllTiles();                              // ������� ��� ����� ����� ����������
        bgMap = GenerateArray(width, height, true, true);       // ���������� ������
        bgMap = TerrainGeneration(bgMap);                       // ���������� ���
        bgMap = StoneGeneration(bgMap);                         // ���������� ���
        bgMap = GrassGeneration(bgMap);                         // ���������� ���

        map = TreesGeneration(map);
        //StructuresGeneration(testStructure);
        StructuresGeneration(testStructure, 12);
        StructuresGeneration(mapleHouse, 2);
        Debug.Log("�� ������");
    }

    public void CreateChunks()                                  // �������� ������
    {
        HelperClass.numChunks = width / HelperClass.chunkSize;  // ������������� ���������� ������

        HelperClass.Chunks = new Tilemap[HelperClass.numChunks];
        HelperClass.ChunksGameobject = new GameObject[HelperClass.numChunks];

        HelperClass.lightChunks = new Tilemap[HelperClass.numChunks];
        HelperClass.lightChunksGameobject = new GameObject[HelperClass.numChunks];

        HelperClass.bgChunks = new Tilemap[HelperClass.numChunks];
        HelperClass.bgChunksGameobject = new GameObject[HelperClass.numChunks];

        // ���� �� ���������� ������
        for (int i = 0; i < HelperClass.numChunks; i++)
        {
            // ��������� ������ � ������� ������� 1- � x+1 �� ����� �������

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

    public int[,] TerrainGeneration(int[,] map)     // ��������� �����
    {
        int perlinHeight;   // ������ �������
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
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes / 2, HelperClass.worldSeed + height) * height / 2.5f);
            //Debug.Log(HelperClass.worldSeed);
            perlinHeight += height / 2;
            //perlinHeight = perlinHeight / 2;

            for (int j = 0; j <= perlinHeight + 1; j++)
            {

                if (j < perlinHeight)
                {
                    map[i, j] = 1;
                    // ������������� ������� ���� ��� ������ ����
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

        // �������������� ����
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

    public int[,] StoneGeneration(int[,] map)     // ��������� �����
    {
        int perlinHeight;   // ������ �������
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

    public int[,] CavesGeneration(int[,] map)     // ��������� �����
    {
        float perlinHeightCaves;   // ������ �������
        float perlinHeightGround;   // ������ ������� ������������� �����
        float perlinHeightOres;   // ������ ������� ���
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

                // ��������� ����
                perlinHeightOres = Mathf.PerlinNoise((i + HelperClass.worldSeed / 2) / ironOre, (j + HelperClass.worldSeed / 2) / ironOre);
                //Debug.Log(perlinHeightOres);
                if (perlinHeightOres > 0.8 && map[i, j] == 3)
                {
                    map[i, j] = 6;
                }

                // ��������� ���� ������������
                perlinHeightTeleportium = Mathf.PerlinNoise((i + HelperClass.worldSeed) / teleportiumOre / 0.5f, (j + HelperClass.worldSeed) / teleportiumOre / 0.5f);

                //perlinHeightTeleportium = Mathf.RoundToInt(Mathf.PerlinNoise(j / stonesmothes, seed * 3) * height / 2.1f);
                //Debug.Log(perlinHeightTeleportium);
                if (perlinHeightTeleportium > 0.87 && map[i, j] == 3)
                {
                    map[i, j] = 7;
                }

                // ��������� ������ �����
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

    public int[,] OresGeneration(int[,] map)     // ��������� �����
    {
        //float perlinHeight;   // ������ �������
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

    public int[,] TreesGeneration(int[,] map)     // ��������� � �����
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
                    //Debug.Log(map[i, j]);
                }
            }
        }
        return map;
    }

    public int[,] GrassGeneration(int[,] map)     // ��������� �����
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
    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // ������, �������, ����� ����� (������ ������)
    {
        //lightTilemap = groundTilemap;

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

                        Instantiate(Trees[(int)Random.RandomRange(0, Trees.Count())], pos, Quaternion.identity);
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


                //if (bgMap[i, j] == 8)
                //{
                //    bgTileMap.SetTile(new Vector3Int(i, j, 0), groundTileBase[6]);       // ������������� ���� �����
                //}
            }
        }
    }
}
