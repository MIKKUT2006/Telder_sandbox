using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Rendering.VolumeComponent;

public class ProceduralGeneration : MonoBehaviour
{
    [SerializeField] public static int height = 400;
    public static int width = 400;     // ������ � ������ (����)

    [SerializeField] float smoothes;        // ��������
    [SerializeField] float cavessmothes;        // �������� �����
    [SerializeField] float stonesmothes;        // �������� �����

    [SerializeField] float ironOre;        // �������� �������� ����

    [SerializeField] float seed;            // ��� ����
    [SerializeField] List<TileBase> groundTile;   // ����
    [SerializeField] TileBase lightTile;
    [SerializeField] Tilemap tilemap;       // ����� ������
    [SerializeField] Tilemap bgTilemap;       // ����� ������ ������� ����
    [SerializeField] Tilemap lightTilemap;       // ����� ������ ��� ���������

    [SerializeField] public static int[,] map; // ��������� ������ �����
    [SerializeField] int[,] bgMap; // ��������� ������ ����� ������� �����
    [SerializeField] int[,] lightMap; // ��������� ������ ����� ������� �����

    [SerializeField] public static int chunkSize = 20;
    [SerializeField] public static Tilemap[] Chunks;
    [SerializeField] public static GameObject[] ChunksGameobject;
    [SerializeField] GameObject chunkPrefab;
    int numChunks;

    [SerializeField] public static Tilemap[] lightChunks;
    [SerializeField] public static GameObject[] lightChunksGameobject;
    [SerializeField] GameObject lightchunkPrefab;


    [SerializeField] GameObject mainTilemap;

    [SerializeField] List<GameObject> Trees;

    // 1 = �����
    // 2 = �����
    // 3 = ������
    // 4 = �������
    // 5 = ����� � ���������
    // 1 = �����
    // 1 = �����


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

        CreateChunks();                                        // ������ �����
        lightMap = GenerateArray(width, height, true, true);   // ���������� ������
        lightTilemap.ClearAllTiles();                    // ������� ��� ����� ����� ����������

        // �������� ����
        tilemap.ClearAllTiles();                    // ������� ��� ����� ����� ����������
        map = GenerateArray(width, height, true, false);   // ���������� ������
        map = TerrainGeneration(map);               // ���������� ���
        map = StoneGeneration(map);               // ���������� ������
        map = CavesGeneration(map);               // ���������� ������
        map = OresGeneration(map);               // ���������� ����
        DestroyStructures();
        // ������� ����
        bgTilemap.ClearAllTiles();                    // ������� ��� ����� ����� ����������
        bgMap = GenerateArray(width, height, true, true);   // ���������� ������
        bgMap = TerrainGeneration(bgMap);               // ���������� ���
        bgMap = StoneGeneration(bgMap);               // ���������� ���
        //bgMap = CavesGeneration(bgMap);               // ���������� ������

        map = StructuresGeneration(map);

        RenderMap(map, tilemap, groundTile, bgMap);        // ���������� ���������
    }

    public void CreateChunks()                            // �������� ������
    {
        numChunks = width / chunkSize;                    // ������������� ���������� ������

        Chunks = new Tilemap[numChunks];
        ChunksGameobject = new GameObject[numChunks];

        lightChunks = new Tilemap[numChunks];
        lightChunksGameobject = new GameObject[numChunks];

        // ���� �� ���������� ������
        for (int i = 0; i < numChunks; i++)
        {
            //------------------
            Tilemap newChunk = new Tilemap();
            Chunks[i] = newChunk;
            GameObject Chunk = Instantiate(chunkPrefab);
            Chunk.name = i.ToString();
            Chunk.transform.parent = transform;
            ChunksGameobject[i] = Chunk;
            //------------------
            Tilemap newlightChunk = new Tilemap();
            lightChunks[i] = newlightChunk;
            GameObject lightChunk = Instantiate(lightchunkPrefab);
            lightChunk.name = i.ToString();
            lightChunk.transform.parent = transform;
            lightChunksGameobject[i] = lightChunk;
            //=================
        }
    }

    void DestroyStructures()
    {
        List <GameObject> trees = GameObject.FindGameObjectsWithTag("tree").ToList();

        foreach (var item in trees)
        {
            Destroy(item);
        }
    }

    // ������ ������ ��������� ����, ��������, ��� � ���� ���� ����� (������� ��� ������ �������� �����)
    // ����� �� ��������� ������� ���� ������
    public int[,] GenerateArray(int width, int height, bool useArray, bool bg)
    {
        int[,] map = new int[width, height]; // ������������� ������� ����
        int[,] bgMap = new int[width, height]; // ������������� ������� ����

        // ���������� ��� �� ������
        for (int i = 0; i < width; i++)
        {
            // ���������� ��� �� ������
            for (int j = 0; j < height; j++)
            {
                map[i, j] = (useArray) ? 0 : 1;     // ���� � ������� ����� false = 0, true = 1
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
    public int[,] TerrainGeneration(int[,] map)     // ��������� �����
    {
        int perlinHeight;   // ������ �������
        for (int i = 0; i < width; i++)
        {
            // �������� ���������� �����
            int chunkCoord = i / chunkSize;   // �������� ���������� �����
            //chunkCoord = chunkCoord * chunkSize;

            int ostatok = chunkCoord % 100;
            if (ostatok != 0)
            {
                chunkCoord -= (chunkCoord - ostatok) + 1;
            }
            Tilemap lighttilemap = lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / smoothes, seed) * height / 2);
            perlinHeight += height / 2;

            for (int j = 0; j <= perlinHeight + 1; j++)
            {
                if (j < perlinHeight)
                {
                    map[i, j] = 1;
                }

                if (j == perlinHeight)
                {
                    map[i, j] = 2;
                }

                if (j > perlinHeight)
                {
                    map[i, j] = 0;
                }
            }
            for (int g = perlinHeight; g < height; g++)
            {
                lighttilemap.SetTile(new Vector3Int(i, g, 0), lightTile);
            }
            lightChunks[chunkCoord] = tilemap;
        }

        return map;
    }

    public int[,] StoneGeneration(int[,] map)     // ��������� �����
    {
        int perlinHeight;   // ������ �������
        for (int i = 0; i < width; i++)
        {
            perlinHeight = Mathf.RoundToInt(Mathf.PerlinNoise(i / stonesmothes / 0.5f, seed * 3) * height / 2.1f);
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

    public int[,] CavesGeneration(int[,] map)     // ��������� �����
    {
        float perlinHeight;   // ������ �������
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);

                if (perlinHeight < 0.4 && map[i, j] == 3)
                {
                    map[i, j] = 4;
                    //lightMap[i, j] = 4;
                    //Debug.Log(lightMap[i, j]);
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

    public int[,] OresGeneration(int[,] map)     // ��������� �����
    {
        float perlinHeight;   // ������ �������
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

    public int[,] StructuresGeneration(int[,] map)     // ��������� �������� (�������)
    {
        float perlinHeight;   // ������ �������

        //for (int i = 0; i < width; i++)
        //{
        //    for (int j = 0; j < height; j++)
        //    {
        //        perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);

        //        if (perlinHeight < 0.4 && map[i, j] == 3)
        //        {
        //            //Debug.Log("asdasdasd");
        //            map[i, j] = 4;
        //        }
        //    }
        //}

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                perlinHeight = Mathf.PerlinNoise((i + seed) / cavessmothes, (j + seed) / cavessmothes);
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

    // ����������� ������ � ����������� �� ����
    public void RenderMap(int[,] map, Tilemap groundTilemap, List<TileBase> groundTileBase, int[,] bgMap)   // ������, �������, ����� ����� (������ ������)
    {
        //lightTilemap = groundTilemap;

        for (int i = 0; i < width; i++)
        {
            // �������� ���������� �����
            int chunkCoord = i / chunkSize;   // �������� ���������� �����
            //chunkCoord = chunkCoord * chunkSize;

            int ostatok = chunkCoord % 100;
            if (ostatok != 0)
            {
                chunkCoord -= (chunkCoord - ostatok) + 1;
            }
            Tilemap tilemap = ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
            Tilemap lighttilemap = lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

            for (int j = 0; j < height; j++)
            {
                //Debug.Log(map[i, j]);

                switch (map[i, j])
                {
                    //case 0:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // ������������� ���� �����
                    //    break;
                    case 1:
                            tilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);
                        break;
                    case 2:
                        tilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);            // ������������� ���� �����

                        break;
                    case 3:
                        tilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                        break;
                    //case 4:
                    //    lightTilemap.SetTile(new Vector3Int(i, j, 0), lightTile);       // ������������� ���� �����
                    //    break;
                    case 5:
                        Vector3 pos = new Vector3(i + 0.5f, j + 3, 0);
                        tilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);      // ������������� �������
                        Instantiate(Trees[0], pos, Quaternion.identity);
                        break;
                    case 6:
                        tilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[4]);       // ������������� ���� �������� ����
                        break;
                }

                Chunks[chunkCoord] = tilemap;

                if (bgMap[i, j] == 1)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[0]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 2)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[1]);       // ������������� ���� �����
                }
                if (bgMap[i, j] == 3)
                {
                    bgTilemap.SetTile(new Vector3Int(i, j, 0), groundTileBase[2]);       // ������������� ���� �����
                }
                
                //if (map[i, j] == 4)
                //{
                //    Debug.Log("asd");
                //    groundTilemap.SetTile(new Vector3Int(i, j, 0), null);       // ������������� ������� (������)
                //}
            }
        }
    }
}
