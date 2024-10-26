using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class HelperClass : MonoBehaviour
{
    // Подключение к бд
    [SerializeField] public static MySqlConnection mySqlConnection
        = new MySqlConnection("Database=sql7740887; Data Source = sql7.freemysqlhosting.net; " +
        "User Id=sql7740887; Password=iE9GIRF1ma; port=3306; charset=utf8");
    [SerializeField] public static string login = null;


    // Двумерный массив карты
    [SerializeField] public static int[,] map;
    [SerializeField] public static int[,] bgMap;   
    //[SerializeField] public static int[,] lightMap;

    // Параметры мира
    [SerializeField] public static bool isNewGame = true;
    [SerializeField] public static int worldHeight = 100;
    [SerializeField] public static int worldWidth = 200;
    [SerializeField] public static int worldSeed;
    [SerializeField] public static int chunkSize = 20;
    [SerializeField] public static int numChunks;

    // Чанки блоков
    [SerializeField] public static Tilemap[] Chunks;
    [SerializeField] public static GameObject[] ChunksGameobject;
    [SerializeField] public static GameObject chunkPrefab;

    // Клетки с жидкостью/твердым блоком
    [SerializeField] public static Cell[,] Cells;

    // Чанки освещения
    [SerializeField] public static Tilemap[] lightChunks;
    [SerializeField] public static GameObject[] lightChunksGameobject;
    [SerializeField] public static GameObject lightchunkPrefab;

    // Чанки заднего плана
    [SerializeField] public static Tilemap[] bgChunks;
    [SerializeField] public static GameObject[] bgChunksGameobject;
    [SerializeField] public static GameObject bgchunkPrefab;

    // Блокировка расположения блока 
    [SerializeField] public static bool barrierPlaceBlock = false;

    // Установка блока
    [SerializeField] public static GameObject Cursor;
    [SerializeField] public static bool setBlock = false;

    // Предмет в руке
    [SerializeField] public static GameObject equippedItem;
    [SerializeField] public static Image equippedCellImage;
    [SerializeField] public static TextMeshProUGUI itemName;

    // Инвентарь
    [SerializeField] public static AllItemsAndBlocks[] playerInventory = new AllItemsAndBlocks[30];
    [SerializeField] public static GameObject playerInventoryGameObject;
    [SerializeField] public static GameObject itemOnCursorGameObject;
    [SerializeField] public static int selectedInventoryCell = 0;

    // Анимация ячейки инвентаря
    [SerializeField] public static Animation equippedCellAnimator;
}

public struct StructureTileData
{
    public int x { get; set; }
    public int y { get; set; }
    public int z { get; set; }
    public int id { get; set; }

}
