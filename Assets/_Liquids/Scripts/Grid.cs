using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Drawing;
using System;
using System.Reflection;
using System.Collections;

public class Grid : MonoBehaviour {

	private int Size = 100;

	public Cell[,] Cells;

	public LiquidSimulator LiquidSimulator;

	public Cell CellPrefab;

	public Tilemap LiquidTilemap;
	public Tile BlockTile;
	public Tile[] WaterTiles;

    bool Fill;

	public float UpdateDelayTime = 0.5f;

    void Start() {

		// Generate our cells 
		CreateGrid();

		// Initialize the liquid simulator
		if(LiquidSimulator == null)
			LiquidSimulator = new LiquidSimulator();

		LiquidSimulator.Initialize (Cells);

		StartCoroutine(DelayExecuteSim(UpdateDelayTime));
        //Cells = HelperClass.Cells;
        Size = Cells.GetLength(0) * Cells.GetLength(1);

        //
    }

	void CreateGrid() {
		// Устанавливаем размеры сетки клеток
		Cells = new Cell[200, 500];

		// Cells
		for (int x = 0; x < 500; x++)
		{
			for (int y = 0; y < 200; y++)
			{
				LiquidTilemap.SetTile(new Vector3Int(x,y,0), null);
				
				Cell cell = new Cell();

				cell.Set (x, y);

				

				// Add border
				if (x == 0 || y == 0 || x == 500 - 1 || y == 200 - 1)
				{
                    Debug.Log($"{x} {y}");
                    LiquidTilemap.SetTile(new Vector3Int(x, y), BlockTile);
					cell.SetType(CellType.Solid);
				}
                    Cells[y, x] = cell;
            }
		}
		UpdateNeighbors ();
	}

    // Устанавливает ссылки на соседние ячейки
    void UpdateNeighbors() {
		for (int x = 0; x < Cells.GetLength(0); x++) {

			for (int y = 0; y < Cells.GetLength(1); y++) {

				// Left most cells do not have left neighbor
				if (x > 0 )
				{
					Cells[x, y].Left = Cells [x - 1, y];
				}
				// Right most cells do not have right neighbor
				if (x < Size - 1)
				{
					Cells[x, y].Right = Cells [x + 1, y];
				}
                // bottom most cells do not have bottom neighbor
                if (y > 0)
                {
                    Cells[x, y].Bottom = Cells[x, y - 1];
                }
                // Top most cells do not have top neighbor
                if (y < Size - 1)
                {
					//Debug.Log($"x={x}");
					//Debug.Log($"y={y}");
					//Debug.Log($"клетка равна {Cells[x, y].Top}");
                    Cells[x, y].Top = Cells[x, y + 1];
                }

            }
		}
	}

	void LateUpdate() {

		Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		int x = (int)((pos.x));
		int y = (int)((pos.y));

		// Check if we are filling or erasing walls
		//if (Input.GetMouseButtonDown(0))
		//{
		//	if ((x > 0 && x < Cells.GetLength(0)) && (y > 0 && y < Cells.GetLength(1)))
		//	{
		//		if (Cells[x, y].Type == CellType.Blank)
		//		{
		//			Fill = true;
		//		}
		//		else
		//		{
		//			Fill = false;
		//		}
		//	}
		//}

		//// Left click draws/erases walls
		//if (Input.GetMouseButton(0))
		//{
		//	if (x != 0 && y != 0 && x != Size - 1 && y != Size - 1)
		//	{
		//		//if ((x > 0 && x < Cells.GetLength(0)) && (y > 0 && y < Cells.GetLength(1)))
		//		//{
		//			if (Fill)
		//			{
		//				Cells[x, y].SetType(CellType.Solid);
		//			}
		//			else
		//			{
		//				Cells[x, y].SetType(CellType.Blank);
		//			}
		//		//}
		//	}
		//}

		// Right click places liquid
		if (Input.GetMouseButton(1)) {
			//Debug.Log($"Позиция х= {x}, позиция y= {y} а размеры клеток {Cells.GetLength(0)}, {Cells.GetLength(1)}");


			if ((x > 0 && x < Cells.GetLength(0)) && (y > 0 && y < Cells.GetLength(1)))
			{
				Debug.Log(Cells[1, 1]);
				Cells[x, y].AddLiquid(2);
			}
		}
    }


    IEnumerator DelayExecuteSim(float time)
    {
        yield return new WaitForSeconds(time);

        // Массив позиций тайлов (размер массива = ширина * высоту мира)
        Vector3Int[] positions = new Vector3Int[Cells.GetLength(0) * Cells.GetLength(1)];
        TileBase[] tileArray = new TileBase[positions.Length];

        int posIndex = 0;

		//Debug.Log(Cells[10, 10].CellUpdate(LiquidTilemap, WaterTiles, BlockTile));
        // Определите спрайт для каждой ячейки
        for (int cx = 0; cx < Cells.GetLength(0); cx++)
        {
            for (int cy = 0; cy < Cells.GetLength(1); cy++)
            {
                tileArray[cx + cy] = Cells[cx, cy].CellUpdate(LiquidTilemap, WaterTiles, BlockTile);
                positions[posIndex] = new Vector3Int(cx, cy, 0);
                posIndex++;
            }
            
        }

        LiquidTilemap.SetTiles(positions, tileArray);

        yield return 0;

        // Run our liquid simulation 
        LiquidSimulator.Simulate(ref Cells);

		// Repeat
        yield return StartCoroutine(DelayExecuteSim(UpdateDelayTime));
    }

}
