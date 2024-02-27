using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockCreateDestroy : MonoBehaviour
{
    // ����� ������ � ����
    public Tilemap tilemap;
    public TileBase[] Block;
    public TileBase Light;

    int chunkSize;
    void Start()
    {
        //chunkSize = ProceduralGeneration.chunkSize;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // ��� ������� ��� ������������� ����
        if (Input.GetMouseButtonDown(1))
        {
            SetTile();
        }

        if (Input.GetMouseButtonDown(0))
        {
            BreakTile();
        }
    }

    void SetTile()
    {
        //// ������� �������
        //Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //int x = (int)Tilepos.x;
        //int y = (int)Tilepos.y;
        //Vector3Int[] blockPosition = new Vector3Int[1];

        //// �������� ���������� �����
        //int chunkCoord = x / chunkSize;   // �������� ���������� �����
        //                                  //chunkCoord = chunkCoord * chunkSize;
        //int ostatok = chunkCoord % 100;
        //if (ostatok != 0)
        //{
        //    chunkCoord -= (chunkCoord - ostatok) + 1;
        //}
        //Tilemap tilemap = ProceduralGeneration.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
        ////Tilemap lightTilemap = ProceduralGeneration.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

        //blockPosition[0] = new Vector3Int(x, y);
        //// ������������� ���� � ������ �������

        //if (tilemap.GetTile(blockPosition[0]) == null)
        //{
        //    tilemap.SetTiles(blockPosition, Block);
        //    //lightTilemap.SetTile(blockPosition[0], null);
        //    ProceduralGeneration.map[x, y] = 1;
        //}
        //ProceduralGeneration.Chunks[chunkCoord] = tilemap;
    }

    void BreakTile()
    {
        //// ������� �������
        //Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //int x = (int)Tilepos.x;
        //int y = (int)Tilepos.y;
        //Vector3Int blockPosition = new Vector3Int(x, y);

        //// �������� ���������� �����
        //int chunkCoord = x / chunkSize;   // �������� ���������� �����
        //                                  //chunkCoord = chunkCoord * chunkSize;
        //int ostatok = chunkCoord % 100;
        //if (ostatok != 0)
        //{
        //    chunkCoord -= (chunkCoord - ostatok) + 1;
        //}
        //Tilemap tilemap = ProceduralGeneration.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
        //if (tilemap.GetTile(blockPosition) != null)
        //{
        //    Tilemap lightTilemap = ProceduralGeneration.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

        //    if (ProceduralGeneration.map[x, y + 1] == 0)                    // ���� �� ������ ���� ����������� �������, �� ������� ��� ���������� ����
        //    {
        //        if (ProceduralGeneration.map[x, y - 1] == 4)
        //        {
        //            for (int i = y; i < ProceduralGeneration.height; i++)
        //            {
        //                if (ProceduralGeneration.map[x, i] == 4)
        //                {
        //                    lightTilemap.SetTile(blockPosition, Light);
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //        tilemap.SetTile(blockPosition, null);
        //        lightTilemap.SetTile(blockPosition, Light);
        //        ProceduralGeneration.map[x, y] = 0;
        //    }
        //    else
        //    {
        //        //Debug.Log(ProceduralGeneration.map[x, y + 1]);
        //        tilemap.SetTile(blockPosition, null);
        //        ProceduralGeneration.map[x, y] = 4;
        //    }
        //    ProceduralGeneration.Chunks[chunkCoord] = tilemap;
        //    ProceduralGeneration.lightChunks[chunkCoord] = lightTilemap;
        //}
    }
}

