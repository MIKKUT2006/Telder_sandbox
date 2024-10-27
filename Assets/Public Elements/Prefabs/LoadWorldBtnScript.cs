using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadWorldBtnScript : MonoBehaviour
{
    public void loadWorld()
    {
        HelperClass.mySqlConnection.Open();
        string getWorld = $"Select * from worlds Where id = '{gameObject.name}'";
        MySqlCommand world = new MySqlCommand(getWorld, HelperClass.mySqlConnection);
        MySqlDataReader worldReader = world.ExecuteReader();
        worldReader.Read();
        // Загрузка мира из бд
        HelperClass.worldId = worldReader.GetInt32("id");
        HelperClass.worldName = worldReader.GetString("name");
        HelperClass.map = LoadJson(worldReader.GetString("data"));
        HelperClass.bgMap = LoadJson(worldReader.GetString("bg_data"));
        worldReader.Close();
        HelperClass.mySqlConnection.Close();
        HelperClass.isNewGame = false;
        SceneManager.LoadScene("WorldOne");
    }
    int[,] LoadJson(string json)
    {
            int[] loadedArray = JsonUtility.FromJson<Wrapper<int>>(json).Items;

            // Преобразование одномерного массива обратно в двумерный
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
    
    [System.Serializable]
    public class Wrapper<T>
    {
        public T[] Items;
    }
}


