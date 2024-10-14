using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombScript : MonoBehaviour
{
    [SerializeField] public GameObject[] ChunksGameobject;
    void Start()
    {
        Explosion();
        ChunksGameobject = HelperClass.ChunksGameobject;
    }

    public void Explosion()
    {
        //Tilemap structureTilemap = GetComponentInChildren<Tilemap>();
        //Debug.Log(structureTilemap.gameObject.name);
        //int chunkSize = 20;
        //BoundsInt bounds = structureTilemap.cellBounds;
        //for (int x = bounds.xMin; x < bounds.xMax; x++)
        //{
        //    for (int y = bounds.yMin; y < bounds.yMax; y++)
        //    {
        //        Vector3Int tilePos = new Vector3Int(x, y, 0);
        //        TileBase tile = structureTilemap.GetTile(tilePos);
                
        //        if (tile != null)
        //        {
        //            Debug.Log("Тут стоит тайл" + tile.name);
        //            //Debug.Log("Tile at position " + tilePos + " is " + tile.name);

        //            int chunkCoord = tilePos.x / chunkSize;

        //            int ostatok = chunkCoord % 100;
        //            if (ostatok != 0)
        //            {
        //                chunkCoord -= (chunkCoord - ostatok) + 1;
        //            }

        //            Debug.Log("Бум в чанке: " + chunkCoord);
        //            Tilemap tilemap = ChunksGameobject[chunkCoord].GetComponent<Tilemap>();

        //            tilemap.SetTile(tilePos, null);
        //        }
        //    }
        //}
    }
}
