using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class HelperClass : MonoBehaviour
{
    // ����������� � ��
    [SerializeField] public static MySqlConnection mySqlConnection
        = new MySqlConnection("Database=sql7740887; Data Source = sql7.freemysqlhosting.net; " +
        "User Id=sql7740887; Password=iE9GIRF1ma; port=3306; charset=utf8");


    public const string url = "https://saoxywsrefjufpncyivz.supabase.co";
    public const string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNhb3h5d3NyZWZqdWZwbmN5aXZ6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzI1NDc3MDMsImV4cCI6MjA0ODEyMzcwM30.Qin9g5rC-SgOg3xdyF4yP9344t54uMBc0HQlUZxInJg";


        [SerializeField] public static string login = null;
    [SerializeField] public static int userId;


    // ��������� ������ �����
    [SerializeField] public static int[,] map;
    [SerializeField] public static int[,] bgMap;   
    //[SerializeField] public static int[,] lightMap;

    // ��������� ����
    [SerializeField] public static bool isNewGame = true;
    [SerializeField] public static int worldHeight = 100;
    [SerializeField] public static int worldWidth = 200;
    [SerializeField] public static int worldSeed;
    [SerializeField] public static int chunkSize = 20;
    [SerializeField] public static int numChunks;

    [SerializeField] public static string worldName;
    [SerializeField] public static int worldId;

    // ����� ������
    [SerializeField] public static Tilemap[] Chunks;
    [SerializeField] public static GameObject[] ChunksGameobject;
    [SerializeField] public static GameObject chunkPrefab;

    // ������ ������
    public enum Biomes { Desert, Forest, Crystal, None }
    public static Biomes[] biomeMap;

    // ������ � ���������/������� ������
    [SerializeField] public static Cell[,] Cells;

    // ����� ���������
    [SerializeField] public static Tilemap[] lightChunks;
    [SerializeField] public static GameObject[] lightChunksGameobject;
    [SerializeField] public static GameObject lightchunkPrefab;

    // ����� ������� �����
    [SerializeField] public static Tilemap[] bgChunks;
    [SerializeField] public static GameObject[] bgChunksGameobject;
    [SerializeField] public static GameObject bgchunkPrefab;

    // ���������� ������������ ����� 
    [SerializeField] public static bool barrierPlaceBlock = false;

    // ��������� �����
    [SerializeField] public static GameObject Cursor;
    [SerializeField] public static bool setBlock = false;

    // ������� � ����
    [SerializeField] public static GameObject equippedItem;
    [SerializeField] public static Image equippedCellImage;
    [SerializeField] public static TextMeshProUGUI itemName;

    // ���������
    [SerializeField] public static AllItemsAndBlocks[] playerInventory = new AllItemsAndBlocks[30];
    [SerializeField] public static GameObject playerInventoryGameObject;
    [SerializeField] public static GameObject itemOnCursorGameObject;
    [SerializeField] public static int selectedInventoryCell = 0;

    // �������� ������ ���������
    [SerializeField] public static Animation equippedCellAnimator;
}

public struct StructureTileData
{
    public int x { get; set; }
    public int y { get; set; }
    public int z { get; set; }
    public int id { get; set; }

}
