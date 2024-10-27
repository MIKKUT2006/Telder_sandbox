using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PauseButtonsScripts : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        
    }
    public void Resume()
    {
        pausePanel.SetActive(false);
    }

    public void ToMenu()
    {
        // Сохранение мира в json
        // Преобразование массива в JSON

        // Путь к файлу
        string mapPath = Application.persistentDataPath + "/map.json";
        string bgMapPath = Application.persistentDataPath + "/bgMap.json";
        string playerInventoryPath = Application.persistentDataPath + "/playerInventory.json";
        // Проверяем, существует ли файл
        if (!File.Exists(mapPath))
        {
            // Если файл не существует, создаем и записываем JSON
            JsonWorld(mapPath, ProceduralGeneration.map);
            JsonWorld(bgMapPath, ProceduralGeneration.bgMap);
            JsonInventory(playerInventoryPath, HelperClass.playerInventory);
            Debug.Log("JSON файл создан: " + mapPath);
        }
        else
        {
            JsonWorld(mapPath, ProceduralGeneration.map);
            JsonWorld(bgMapPath, ProceduralGeneration.bgMap);
            JsonInventory(playerInventoryPath, HelperClass.playerInventory);
            Debug.Log("Файл перезаписан: " + mapPath);
        }
        HelperClass.mySqlConnection.Open();
        Debug.Log($"айди пользователя {HelperClass.userId}");
        string saveWorld = $"UPDATE worlds SET data = '{GetJson(ProceduralGeneration.map)}', bg_data  = '{GetJson(ProceduralGeneration.bgMap)}'" +
            $" WHERE user_id = {HelperClass.userId} AND name = '{HelperClass.worldName}', inventory = '{GetInventoryJson(HelperClass.playerInventory)}'";
        MySqlCommand mySqlCommand = new MySqlCommand(saveWorld, HelperClass.mySqlConnection);
        Debug.Log(mySqlCommand.ExecuteNonQuery());
        HelperClass.mySqlConnection.Close();
        // Загрузка сцены меню
        SceneManager.LoadScene("_MainMenu");
    }

    // Методы для json для бд
    string GetJson(int[,] map)
    {
        // Преобразование массива в JSON
        string json = JsonHelper.ToJson(map);
        return json;
    }

    string GetInventoryJson(AllItemsAndBlocks[] inventory)
    {
        // Преобразование массива в JSON
        string json = JsonHelperArray.ToJson(inventory);
        return json;
    }

    // Метод создания json файла
    void JsonWorld(string path, int[,] map)
    {
        // Преобразование массива в JSON
        string json = JsonHelper.ToJson(map);
        // Запись JSON в файл
        File.WriteAllText(path, json);
    }

    // Метод создания json файла
    void JsonInventory(string path, AllItemsAndBlocks[] inventory)
    {
        // Преобразование массива в JSON
        string json = JsonHelperArray.ToJson(inventory);

        // Запись JSON в файл
        File.WriteAllText(path, json);
    }
}


// Класс для записи двумерного массива в json
public static class JsonHelper
{
    public static string ToJson<T>(T[,] array)
    {
        // Сериализация двумерного массива в JSON
        var list = new List<T>();
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                list.Add(array[i, j]);
            }
        }
        return JsonUtility.ToJson(new Wrapper<T> { Items = list.ToArray() });
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

// Класс для записи одномерного массива в json
public static class JsonHelperArray
{
    public static string ToJson(AllItemsAndBlocks[] array)
    {
        // Сериализация двумерного массива в JSON
        List<AllItemsAndBlocks> list = array.ToList();

        return JsonUtility.ToJson(new Wrapper<AllItemsAndBlocks> { Items = list.ToArray() });
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}