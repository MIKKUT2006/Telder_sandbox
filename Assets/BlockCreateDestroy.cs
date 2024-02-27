using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockCreateDestroy : MonoBehaviour
{
    // Карта тайлов и блок
    public Tilemap tilemap;
    public TileBase[] Block;
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // При нажатии пкм устанавливаем блок
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
        // Позиция курсора
        Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int x = (int)Tilepos.x;
        int y = (int)Tilepos.y;
        Vector3Int[] block = new Vector3Int[1];
        block[0] = new Vector3Int(x, y);
        // Устанавливаем блок в позици курсора
        tilemap.SetTiles(block, Block);
        //Debug.Log("Tile");
        //tilemap.SetTilesBlock();
    }

    void BreakTile()
    {
        // Позиция курсора
        Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int x = (int)Tilepos.x;
        int y = (int)Tilepos.y;
        Vector3Int block = new Vector3Int(x, y);
        //block[0] = 
        // Устанавливаем блок в позици курсора
        tilemap.SetTile(block, null);
        //Debug.Log("Tile");
        //tilemap.SetTilesBlock();
    }
}
