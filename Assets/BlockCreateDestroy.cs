using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockCreateDestroy : MonoBehaviour
{
    // ����� ������ � ����
    public Tilemap tilemap;
    public TileBase[] Block;
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // ��� ������� ��� ������������� ����
        if (Input.GetMouseButtonDown(1))
        {
            SetTile();
        }
    }

    void SetTile()
    {
        // ������� �������
        Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int x = (int)Tilepos.x;
        int y = (int)Tilepos.y;
        Vector3Int[] block = new Vector3Int[1];
        block[0] = new Vector3Int(x, y);
        // ������������� ���� � ������ �������
        tilemap.SetTiles(block, Block);
        //Debug.Log("Tile");
        //tilemap.SetTilesBlock();
    }
}
