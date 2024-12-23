using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;


public class Cursor : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    private Vector3Int cellSize;

    void Start()
    {
        //tilemap = GetComponent<Tilemap>();
        cellSize = Vector3Int.RoundToInt(tilemap.cellSize);
        HelperClass.Cursor = gameObject;
        HelperClass.Cursor.GetComponent<SpriteRenderer>().enabled = false;
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector3 cellPosition = tilemap.WorldToCell(mouseWorldPosition);

        cellPosition.x -= 0.5f;
        cellPosition.y -= 0.5f;

        Vector3 cellCenter = cellPosition + Vector3Int.RoundToInt(cellSize);

        transform.position = cellCenter;
        //Debug.Log(cellCenter);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) { HelperClass.barrierPlaceBlock = true; Debug.Log("Вам мешает игрок"); }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) { HelperClass.barrierPlaceBlock = false; Debug.Log("Преград нет"); }
    }
}
