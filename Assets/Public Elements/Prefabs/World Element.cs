using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadWorldBtnScript : MonoBehaviour
{
    public void DeleteWorld()
    {
        HelperClass.mySqlConnection.Open();
        string deleteWorld = $"Delete from worlds Where id = '{gameObject.name}'";
        MySqlCommand world = new MySqlCommand(deleteWorld, HelperClass.mySqlConnection);
        world.ExecuteNonQuery();
        Destroy(gameObject.GetComponentInParent<HorizontalLayoutGroup>().gameObject);
        HelperClass.mySqlConnection.Close();
    }

    public void loadWorld()
    {
        HelperClass.mySqlConnection.Open();
        string getWorld = $"Select * from worlds Where id = '{gameObject.name}'";
        MySqlCommand world = new MySqlCommand(getWorld, HelperClass.mySqlConnection);
        MySqlDataReader worldReader = world.ExecuteReader();
        worldReader.Read();
        // �������� ���� �� ��
        HelperClass.worldId = worldReader.GetInt32("id");
        HelperClass.worldName = worldReader.GetString("name");
        HelperClass.map = LoadJson(worldReader.GetString("world_data"));
        HelperClass.bgMap = LoadJson(worldReader.GetString("world_bg_data"));
        HelperClass.playerEnterPosition = HelperClass.StringToVector3(worldReader.GetString("player_position"));
        Debug.Log(HelperClass.playerEnterPosition);
        HelperClass.playerInventory = LoadInventory(worldReader.GetString("inventory"));
        worldReader.Close();
        HelperClass.mySqlConnection.Close();
        HelperClass.isNewGame = false;
        SceneManager.LoadScene("WorldOne");
    }
    int[,] LoadJson(string json)
    {
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

    AllItemsAndBlocks[] LoadInventory(string json)
    {
        AllItemsAndBlocks[] loadedArray = JsonUtility.FromJson<Wrapper<AllItemsAndBlocks>>(json).Items;
        return loadedArray;
    }

    [System.Serializable]
    public class Wrapper<T>
    {
        public T[] Items;
    }
}


