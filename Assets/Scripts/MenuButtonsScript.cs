using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtonsScript : MonoBehaviour
{
    [SerializeField] private AudioSource AudioSource;
    [SerializeField] private TMP_InputField WorldSeed;
    [SerializeField] private TMP_InputField WorldName;

    [SerializeField] private Button playBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private GameObject btnSelection;

    [SerializeField] private GameObject buttonsPanel;

    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject selectWorldsPanel;
    [SerializeField] private GameObject settingPanel;

    [SerializeField] private GameObject loadWorldPanel;
    [SerializeField] private GameObject createNewWorldPanel;
    [SerializeField] private GameObject multiplayerPanel;

    // ������ � ��
    // ���� ������ � ������
    [SerializeField] private TMP_InputField registrationLoginText;
    [SerializeField] private TMP_InputField registrationPasswordText;

    [SerializeField] private TMP_InputField enterLoginText;
    [SerializeField] private TMP_InputField enterPasswordText;

    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private GameObject authorizationPanel;
    

    private int userId;

    private void Start()
    {
        HelperClass.mySqlConnection.Open();

        if (HelperClass.login == null)
        {
            buttonsPanel.SetActive(false);
            selectWorldsPanel.SetActive(false);
            registrationPanel.SetActive(true);
        }
    }

    // �����������
    public void Registration()
    {
        // �������� �� ������ ����
        if (registrationLoginText.text != "" || registrationPasswordText.text != "")
        {
            string sql = $"Insert into users (login, password) values ('{registrationLoginText.text}', '{registrationPasswordText.text}')";
            MySqlCommand mySqlCommand = new MySqlCommand(sql, HelperClass.mySqlConnection);
            Debug.Log(mySqlCommand.ExecuteNonQuery());

            string test = $"Select * from users Where login = '{registrationLoginText.text}'";
            MySqlCommand testmySqlCommand = new MySqlCommand(test, HelperClass.mySqlConnection);
            MySqlDataReader mySqlDataReader = testmySqlCommand.ExecuteReader();
            mySqlDataReader.Read();
            userId = mySqlDataReader.GetInt32("id");
            Debug.Log($"�������� �����������: {mySqlDataReader.GetString("login")}");

            // ����� �������� ����
            buttonsPanel.SetActive(true);

            // �������� ������ ������ �����
            selectWorldsPanel.SetActive(true);
            registrationPanel.SetActive(false);
            authorizationPanel.SetActive(false);

            createNewWorldPanel.SetActive(true);
            loadWorldPanel.SetActive(false);
            multiplayerPanel.SetActive(false);
        }
        else
        {
            Debug.Log("�� ��� ���� ���������!");
        }
        
    }
    // �����������
    public void Authorization()
    {

    }

    public void CreateWorld()
    {
        // ���������� ��������� � �������� ����
        loadPanel.SetActive(true);
        selectWorldsPanel.SetActive(false);
        // �������� ����� �������� ����
        HelperClass.worldSeed = int.Parse(WorldSeed.text);
        SceneManager.LoadScene("WorldOne");
    }
    public void OpenWorldsPanel()
    {
        // ������� �� ������ "������"
        setBtnSelectorPosition(playBtn);
        selectWorldsPanel.SetActive(true);
        settingPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        setBtnSelectorPosition(settingsBtn);
        selectWorldsPanel.SetActive(false);
        settingPanel.SetActive(true);
    }

    private void setBtnSelectorPosition(Button btn)
    {
        btnSelection.transform.position = new Vector2(btn.transform.position.x, btn.transform.position.y - 0.5f);
    }


    // ������ � ������ ���� ����
    public void CreateNewWorld()
    {
        createNewWorldPanel.SetActive(true);
        loadWorldPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
    }

    public void Multiplayer()
    {
        createNewWorldPanel.SetActive(false);
        loadWorldPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    public void LoadWorld()
    {
        createNewWorldPanel.SetActive(false);
        loadWorldPanel.SetActive(true);
        multiplayerPanel.SetActive(false);

        // �������� �������� �������� ����
        HelperClass.isNewGame = false;
        HelperClass.map =  LoadJson(Application.persistentDataPath + "/map.json");
        HelperClass.bgMap =  LoadJson(Application.persistentDataPath + "/bgMap.json");
        HelperClass.playerInventory = LoadInventory(Application.persistentDataPath + "/playerInventory.json");

        SceneManager.LoadScene("WorldOne");
    }

    // ������� ��� ������ ���� � json
    int[,] LoadJson(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            int[] loadedArray = JsonUtility.FromJson<Wrapper<int>>(json).Items;

            // �������������� ����������� ������� ������� � ���������
            int[,] map = new int[HelperClass.worldWidth, HelperClass.worldHeight];
            Debug.Log(map.GetLength(0));

            for (int i = 0; i < HelperClass.worldWidth; i++)
            {
                for (int j = 0; j < HelperClass.worldHeight; j++)
                {
                    map[i, j] = loadedArray[i * HelperClass.worldHeight + j];
                }
            }
            return map;
        }
        else
        {
            Debug.Log("���� �� ������");
            return null;
        }
    }

    AllItemsAndBlocks[] LoadInventory(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            AllItemsAndBlocks[] loadedArray = JsonUtility.FromJson<Wrapper<AllItemsAndBlocks>>(json).Items;
            return loadedArray;
        }
        else
        {
            Debug.Log("���� �� ������");
            return null;
        }
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    
}
