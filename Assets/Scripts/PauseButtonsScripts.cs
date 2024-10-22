using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseButtonsScripts : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
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
        string lightMapPath = Application.persistentDataPath + "/lightMap.json";

        // Проверяем, существует ли файл
        if (!File.Exists(mapPath))
        {
            // Если файл не существует, создаем и записываем JSON
            JsonFile(mapPath);
            JsonFile(bgMapPath);
            JsonFile(lightMapPath);
            Debug.Log("JSON файл создан: " + mapPath);
        }
        else
        {
            JsonFile(mapPath);
            JsonFile(bgMapPath);
            JsonFile(lightMapPath);
            Debug.Log("Файл перезаписан: " + mapPath);
        }

        // Загрузка сцены меню
        SceneManager.LoadScene("_MainMenu");
    }
    // Метод создания json файла
    void JsonFile(string path)
    {
        // Преобразование массива в JSON
        string json = JsonHelper.ToJson(ProceduralGeneration.map);

        // Запись JSON в файл
        File.WriteAllText(path, json);
    }
}


// Класс для записи массива в json
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
