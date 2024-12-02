﻿using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class HelperClass : MonoBehaviour
{
    // Подключение к бд
    [SerializeField] public static MySqlConnection mySqlConnection
        = new MySqlConnection("Database=sql7747943; Data Source = sql7.freemysqlhosting.net; " +
        "User Id=sql7747943; Password=xK8jSrD9bA; port=3306; charset=utf8");

    public const string url = "https://saoxywsrefjufpncyivz.supabase.co";
    public const string key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNhb3h5d3NyZWZqdWZwbmN5aXZ6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzI1NDc3MDMsImV4cCI6MjA0ODEyMzcwM30.Qin9g5rC-SgOg3xdyF4yP9344t54uMBc0HQlUZxInJg";


    [SerializeField] public static string login = null;
    [SerializeField] public static int userId;


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

    [SerializeField] public static string worldName;
    [SerializeField] public static int worldId;

    // Чанки блоков
    [SerializeField] public static Tilemap[] Chunks;
    [SerializeField] public static GameObject[] ChunksGameobject;
    [SerializeField] public static GameObject chunkPrefab;

    // Список биомов
    public enum Biomes { Desert, Forest, Crystal, Snow, None }
    public static Biomes[] biomeMap;
    public static Biomes currentBiome;

    // Клетки с жидкостью/твердым блоком
    [SerializeField] public static Cell[,] Cells;

    // Скорость падения погодных явлений
    [SerializeField] public static float weatherFallSpeed = 5f;

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

    // Игровой объект игрока
    [SerializeField] public static GameObject playerGameObject;
    [SerializeField] public static Vector3 playerEnterPosition;
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

    // Интерфейс
    [SerializeField] public static bool pausePanelIsShow = false;

    public static Vector3 StringToVector3(string convertString)
    {
        string[] strings = convertString.Split('|');
        Vector3 resultVector = new Vector3 {
        x = float.Parse(strings[0]),
        y = float.Parse(strings[1]),
        z = float.Parse(strings[2]),
        };

        return resultVector;
    }

    public static void AddItemToInventory(GameObject item)
    {
        bool inventoryIsFull = false;
        int InventoryCell = HelperClass.playerInventory.GetLength(0) - 1;
        // Перебор всех ячеек инвентаря
        for (int i = HelperClass.playerInventory.GetLength(0) - 1; i >= 0; i--)
        {
            Debug.Log("ячейка номер" + InventoryCell);
            Debug.Log("В инвентаре " + HelperClass.playerInventory[i]);
            if (HelperClass.playerInventory[i] != null && HelperClass.playerInventory[i].name == BlocksData.allBlocks.Find(x => x.blockIndex == int.Parse(item.name)).name)
            {
                HelperClass.playerInventory[i].count++;
                Debug.Log($"В инвентаре {HelperClass.playerInventory[i].count} предмета {HelperClass.playerInventory[i].name}");
                playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();
                Destroy(item);
                return;
            }
            else if (HelperClass.playerInventory[i] == null && int.Parse(item.name) != 0)
            {
                InventoryCell = i;
                Debug.Log("Добавляем предмет в ячейку " + InventoryCell);
            }
            else
            {
                Debug.Log("Не прошло по условию");
            }
        }
        if (!inventoryIsFull && InventoryCell != -1)
        {
            HelperClass.playerInventory[InventoryCell] = BlocksData.allBlocks.Find(x => x.blockIndex == int.Parse(item.name));
            HelperClass.playerInventory[InventoryCell].count = 1;
            Debug.Log("В инвентарь был добавлен: " + HelperClass.playerInventory[InventoryCell].name);
            playerInventoryGameObject.transform.Find(InventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
            playerInventoryGameObject.transform.Find(InventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = item.GetComponent<SpriteRenderer>().sprite;
            HelperClass.playerInventory[InventoryCell].imagePath = "Assets/Blocks/Firstworld/" + item.GetComponent<SpriteRenderer>().sprite.name + ".png";
            Destroy(item);
            if (HelperClass.selectedInventoryCell == InventoryCell)
            {
                HelperClass.Cursor.SetActive(true);
            }
            playerInventoryGameObject.transform.Find(InventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
            playerInventoryGameObject.transform.Find(InventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[InventoryCell].count.ToString();
        }
    }

    // ��������� ������ ��������� � ����� ���������� ���������
    public static void LoadInventoryImages()
    {
        float pixelsPerUnit = 16;

        for (int i = 0; i < HelperClass.playerInventory.Count(); i++)
        {
            if (HelperClass.playerInventory[i] != null)
            {
                if (!string.IsNullOrEmpty(HelperClass.playerInventory[i].imagePath) && File.Exists(HelperClass.playerInventory[i].imagePath) && HelperClass.playerInventory[i].imagePath != null)
                {
                    // �������� �������� �� �����
                    byte[] imageData = File.ReadAllBytes(HelperClass.playerInventory[i].imagePath);
                    Texture2D texture = new Texture2D(16, 16);
                    texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
                    texture.filterMode = FilterMode.Point;

                    // ������������ ������� ������� � ������ pixelsPerUnit
                    float width = texture.width / 16;
                    float height = texture.height / 16;

                    // �������� ������� �� ��������
                    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                    playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                    playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().sprite = newSprite;

                    playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                    playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();
                }
            }
        }

    }
}

public struct StructureTileData
{
    public int x { get; set; }
    public int y { get; set; }
    public int z { get; set; }
    public int id { get; set; }

}


