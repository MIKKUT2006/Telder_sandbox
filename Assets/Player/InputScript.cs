using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using PUSHKA.MySQL;
using System.Data;
using MySql.Data.MySqlClient;
using static HelperClass;
public class InputScript : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private float playerSpeed = 3f;
    [SerializeField] private float playerDefaultSpeed = 3f;
    [SerializeField] private float playerSprintSpeed = 5f;
    private bool isFlip = false;
    public GameObject bullet;
    private CinemachineVirtualCamera cinemachineVirtualCamera;
    private GameObject inventoryGameObject;
    private GameObject inventoryLightGameObject;

    // ������ �����
    [SerializeField] private GameObject pausePanel;

    // ������ ��� ��������� ���������� �����
    [SerializeField] GameObject selectedBlock;

    [SerializeField] public Tilemap tilemap;
    private Vector3 cellSize;

    // ���� ��� ������
    public Sprite desertBackground; // ��� ��� �������
    public Sprite forestBackground; // ��� ��� ����
    public Sprite crystalBackground; // ��� ��� ������������ �����
    public SpriteRenderer backgroundImage;
    public SpriteRenderer tempBackgroundImage;
    Biomes currentBiome;
    private float fadeDuration = 3.0f; // ������������ ����� � ��������

    // �������� ����
    private bool inventoryOpen = false;
    private float pixelsPerUnit = 16;
    private void Awake()
    {
        Camera.main.gameObject.transform.localPosition = transform.localPosition;
        cinemachineVirtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        inventoryGameObject = GameObject.FindGameObjectWithTag("Inventory");
        HelperClass.playerInventoryGameObject = inventoryGameObject;
        inventoryGameObject.SetActive(false);
        HelperClass.equippedItem = gameObject.transform.Find("Player_1").transform.Find("Item").gameObject;
        HelperClass.itemName = GameObject.FindGameObjectWithTag("ItemName").GetComponent<TextMeshProUGUI>();
        if (HelperClass.isNewGame == false) {
            LoadInventoryImages();
            gameObject.transform.position = HelperClass.playerEnterPosition;
        }

        HelperClass.playerGameObject = gameObject;

        //spriteRenderer = GetComponent<SpriteRenderer>();
        SetBackground(Biomes.Desert); // ��������� ���������� �����, ����� �������� � ����������� �� ������

        // �������� ����������� � ��
        //SqlDataBase db = new SqlDataBase("sql7.freemysqlhosting.net", "sql7740887", "sql7740887", "iE9GIRF1ma");
        //db.RunQuery("Insert into users (login, password) values ('SVO','ANTISVO')");
        //db.SelectQuery("Select * from users", out DataTable dataTable);

        //Debug.Log(dataTable.Rows);

        //MySqlConnection mySqlConnection = new MySqlConnection("Database=sql7740887; Data Source = sql7.freemysqlhosting.net; " +
        //    "User Id=sql7740887; Password=iE9GIRF1ma; port=3306; charset=utf8");

        //mySqlConnection.Open();

        //string sql = "Select * from users";
        //MySqlCommand mySqlCommand = new MySqlCommand(sql, mySqlConnection);
        //MySqlDataReader mySqlDataReader = mySqlCommand.ExecuteReader();
        //while (mySqlDataReader.Read())
        //{
        //    string login = mySqlDataReader.GetString("login");
        //    Debug.Log(login);
        //}

        //Debug.Log(mySqlCommand.ToString());

    }

    // ��������� ���� ����� ��� ����� �����
    public void UpdateBiome(Biomes newBiome)
    {
        SetBackground(newBiome);
    }

    public void SetBackground(Biomes currentBiome)
    {
        switch (currentBiome)
        {
            case Biomes.Desert:
                StartCoroutine(FadeToBackground(desertBackground));
                break;
            case Biomes.Forest:
                StartCoroutine(FadeToBackground(forestBackground));
                break;
            case Biomes.Crystal:
                StartCoroutine(FadeToBackground(crystalBackground));
                break;
        }
    }

    private IEnumerator FadeToBackground(Sprite newBackground)
    {
        tempBackgroundImage.sprite = backgroundImage.sprite;

        //// ������� ��������� �������� ����
        yield return StartCoroutine(FadeOut());

        // ����� ����
        backgroundImage.sprite = newBackground;

        // ������� ��������� ������ ����
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        Color color = backgroundImage.color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            backgroundImage.color = new Color(color.r, color.g, color.b, 1 - normalizedTime);
            yield return null;
        }
        backgroundImage.color = new Color(color.r, color.g, color.b, 0); // ���������, ��� �����-����� ��������� ����������
    }

    private IEnumerator FadeIn()
    {
        Color color = backgroundImage.color;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            backgroundImage.color = new Color(color.r, color.g, color.b, normalizedTime);
            yield return null;
        }
        backgroundImage.color = new Color(color.r, color.g, color.b, 1); // ���������, ��� �����-����� ����� ����������
    }

    // ��������� ������ ��������� � ����� ���������� ���������
    void LoadInventoryImages()
    {
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

                    inventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                    inventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().sprite = newSprite;

                    inventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                    inventoryGameObject.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();
                }
                //else
                //{
                //    Debug.LogError("������: �������� ���� � ����������� ��� ���� �� ������: " + HelperClass.playerInventory[i].imagePath);
                //}
            }
        }
        
    }

    void Start()
    {
        //gameObject.transform.position = new Vector3(HelperClass.worldWidth / 2, HelperClass.worldHeight, 0);
        rb = GetComponent<Rigidbody2D>();
        cellSize = tilemap.cellSize;
    }
    
    void Update()
    {
        rb.linearVelocity = new Vector2 (Input.GetAxis("Horizontal") * playerSpeed, rb.linearVelocity.y);

        GetCurrentBiome(transform.position);

        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log(HelperClass.playerInventory[1].count);
        }

        if (Input.GetMouseButtonDown(0))
        {
            GetComponent<Animator>().SetBool("Attack", true);
            StartCoroutine(attackCooldown());
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            playerSpeed = playerSprintSpeed;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            playerSpeed = playerDefaultSpeed;
        }

        // ��������� ���� �����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pausePanel.SetActive(true);
        }

        // �������� ��������� �� ������� ������� I
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryOpen == false)
            {
                inventoryOpen = true;
                inventoryGameObject.SetActive(true);
            }
            else
            {
                inventoryOpen = false;
                inventoryGameObject.SetActive(false);
            }
        }
        // ������ �������� ������� ���������
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            HelperClass.selectedInventoryCell = 0;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            HelperClass.selectedInventoryCell = 1;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            HelperClass.selectedInventoryCell = 2;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            HelperClass.selectedInventoryCell = 3;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            HelperClass.selectedInventoryCell = 4;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            HelperClass.selectedInventoryCell = 5;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            HelperClass.selectedInventoryCell = 6;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            HelperClass.selectedInventoryCell = 7;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            HelperClass.selectedInventoryCell = 8;
            equipInventoryCell();
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            HelperClass.selectedInventoryCell = 9;
            equipInventoryCell();
        }
    }

    // �������� ���� � ������� ���������
    public Biomes GetCurrentBiome(Vector2 position)
    {
        int xIndex = Mathf.RoundToInt(position.x); // �������� ������ �� X

        // ���������, �� ������� �� �� ������� �������
        if (xIndex >= 0 && xIndex < HelperClass.worldWidth)
        {
            if (currentBiome != biomeMap[xIndex])
            {
                currentBiome = biomeMap[xIndex];
                SetBackground(currentBiome);
            }
            return biomeMap[xIndex]; // ���������� ���� ��� ������� �������
        }

        return Biomes.None; // ���� ������� ��� ������, ���������� None
    }

    private void equipInventoryCell()
    {
        if (HelperClass.equippedCellImage != null)
        {
            HelperClass.equippedCellImage.color = Color.white;
        }
        Image cellImage;
        cellImage = HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).GetComponent<Image>();
        cellImage.color = new Color32(47, 192, 255, 255);
        HelperClass.equippedCellImage = cellImage;
        HelperClass.equippedItem.GetComponent<SpriteRenderer>().enabled = true;
        HelperClass.equippedItem.GetComponent<SpriteRenderer>().sprite = HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
        if (HelperClass.playerInventory[HelperClass.selectedInventoryCell] != null && HelperClass.playerInventory[HelperClass.selectedInventoryCell].isBlock == true)
        {
            HelperClass.Cursor.SetActive(true);
        }
        else
        {
            HelperClass.Cursor.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetAxis("Horizontal") < 0 && isFlip == false)
        {
            Flip();
        }
        if (Input.GetAxis("Horizontal") > 0 && isFlip == true)
        {
            Flip();
        }

        if (rb.linearVelocity.x == 0)
        {
            GetComponent<Animator>().SetBool("Run", false);
        }
        else
        {
            GetComponent<Animator>().SetBool("Run", true);
        }
    }

    private void Flip()
    {
        isFlip = !isFlip;
        transform.localScale = new Vector3(transform.localScale.x * -1, 1, 1);
    }

    IEnumerator attackCooldown()
    {
        yield return new WaitForSeconds(0.3f);
        GetComponent<Animator>().SetBool("Attack", false);
    }

}
