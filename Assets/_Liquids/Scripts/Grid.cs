using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Drawing;
using System;
using System.Reflection;
using System.Collections;

public class Grid : MonoBehaviour
{

    public static int Size = 64;

    public static Cell[,] Cells;

    public static LiquidSimulator LiquidSimulator;

    public static Cell CellPrefab;

    public static Tilemap LiquidTilemap;
    [SerializeField] static Tile BlockTile;
    [SerializeField] public Tile[] WaterTiles;

    bool Fill;

    public float UpdateDelayTime = 0.5f;


    private void Awake()
    {
        // Находим игровые объекты по тегам
        LiquidSimulator = GameObject.Find("LiquidSimulator").GetComponent<LiquidSimulator>();
        //CellPrefab = GameObject.Find("").GetComponent<Cell>();
        BlockTile = Resources.Load<Tile>("Tiles");
        LiquidTilemap = GameObject.Find("Liquid Tilemap").GetComponent<Tilemap>();
        //BlockTile = GameObject.Find("").GetComponent<Tilemap>();
    }
    void Start()
    {

        Size = HelperClass.worldWidth;
        Cells = HelperClass.Cells;
        // Generate our cells 
        CreateGrid();

        // Initialize the liquid simulator
        if (LiquidSimulator == null)
            LiquidSimulator = new LiquidSimulator();

        
        LiquidSimulator.Initialize(HelperClass.Cells);
        StartCoroutine(DelayExecuteSim(UpdateDelayTime));
        CreateWaterTiles();
    }
    public void CreateWaterTiles()
    {
        // Шум перлина для водных пещер
        float perlinHeightCaves;
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < HelperClass.worldHeight; y++)
            {
                perlinHeightCaves = Mathf.PerlinNoise((x + HelperClass.worldSeed) / 2, (y + HelperClass.worldSeed) / 2);

                if (y <= HelperClass.worldHeight && perlinHeightCaves < 0.4)
                {
                    if (ProceduralGeneration.map[x, y] == 4)
                    {
                        HelperClass.Cells[x, y].AddLiquid(1);
                    }
                }
            }
        }
    }

    public static void CreateGrid()
    {
        

        // Cells
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                LiquidTilemap.SetTile(new Vector3Int(x, y, 0), null);

                Cell cell = new Cell();

                cell.Set(x, y);

                // Add border
                if (x == 0 || y == 0 || x == Size - 1 || y == Size - 1)
                {
                    LiquidTilemap.SetTile(new Vector3Int(x, y), BlockTile);
                    cell.SetType(CellType.Solid);
                }

                HelperClass.Cells[x, y] = cell;
            }
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < HelperClass.worldHeight; y++)
            {
                Cell cell = new Cell();

                cell.Set(x, y);

                // Add border

                if (y <= HelperClass.worldHeight)
                {
                    if (ProceduralGeneration.map[x, y] != 4 && ProceduralGeneration.map[x, y] != 0)
                    {
                        LiquidTilemap.SetTile(new Vector3Int(x, y), BlockTile);
                        cell.SetType(CellType.Solid);
                    }
                }
                HelperClass.Cells[x, y] = cell;
            }
        }

        UpdateNeighbors();
    }

    // Sets neighboring cell references
    static void UpdateNeighbors()
    {
        for (int x = 0; x < Size; x++)
        {

            for (int y = 0; y < Size; y++)
            {

                // Left most cells do not have left neighbor
                if (x > 0)
                {
                    HelperClass.Cells[x, y].Left = HelperClass.Cells[x - 1, y];
                }
                // Right most cells do not have right neighbor
                if (x < Size - 1)
                {
                    HelperClass.Cells[x, y].Right = HelperClass.Cells[x + 1, y];
                }
                // bottom most cells do not have bottom neighbor
                if (y > 0)
                {
                    HelperClass.Cells[x, y].Bottom = HelperClass.Cells[x, y - 1];
                }
                // Top most cells do not have top neighbor
                if (y < Size - 1)
                {
                    HelperClass.Cells[x, y].Top = HelperClass.Cells[x, y + 1];
                }

            }
        }
    }

    void LateUpdate()
    {

        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int x = (int)((pos.x));
        int y = (int)((pos.y));

        // Check if we are filling or erasing walls
        //if (Input.GetMouseButtonDown(0))
        //{
        //    if ((x > 0 && x < HelperClass.Cells.GetLength(0)) && (y > 0 && y < HelperClass.Cells.GetLength(1)))
        //    {
        //        if (HelperClass.Cells[x, y].Type == CellType.Blank)
        //        {
        //            Fill = true;
        //        }
        //        else
        //        {
        //            Fill = false;
        //        }
        //    }
        //}

        //// Left click draws/erases walls
        //if (Input.GetMouseButton(0))
        //{
        //    if (x != 0 && y != 0 && x != Size - 1 && y != Size - 1)
        //    {
        //        if ((x > 0 && x < HelperClass.Cells.GetLength(0)) && (y > 0 && y < HelperClass.Cells.GetLength(1)))
        //        {
        //            if (Fill)
        //            {
        //                HelperClass.Cells[x, y].SetType(CellType.Solid);
        //            }
        //            else
        //            {
        //                HelperClass.Cells[x, y].SetType(CellType.Blank);
        //            }
        //        }
        //    }
        //}

        // Right click places liquid
        if (Input.GetMouseButton(1))
        {
            if ((x > 0 && x < HelperClass.Cells.GetLength(0)) && (y > 0 && y < HelperClass.Cells.GetLength(1)))
            {
                HelperClass.Cells[x, y].AddLiquid(5);
            }
        }


    }


    IEnumerator DelayExecuteSim(float time)
    {
        yield return new WaitForSeconds(time);


        Vector3Int[] positions = new Vector3Int[Size * Size];
        TileBase[] tileArray = new TileBase[positions.Length];

        int posIndex = 0;

        // Determine sprite for each cell
        for (int cx = 0; cx < HelperClass.Cells.GetLength(0); cx++)
        {
            for (int cy = 0; cy < HelperClass.Cells.GetLength(1); cy++)
            {
                tileArray[cx * Size + cy] = HelperClass.Cells[cx, cy].CellUpdate(LiquidTilemap, WaterTiles, BlockTile);
                positions[posIndex] = new Vector3Int(cx, cy, 0);
                posIndex++;
            }

        }

        LiquidTilemap.SetTiles(positions, tileArray);

        yield return 0;

        // Run our liquid simulation 
        LiquidSimulator.Simulate(ref HelperClass.Cells);

        // Repeat
        yield return StartCoroutine(DelayExecuteSim(UpdateDelayTime));
    }

}