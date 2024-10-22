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
        // ���������� ���� � json
        // �������������� ������� � JSON

        // ���� � �����
        string mapPath = Application.persistentDataPath + "/map.json";
        string bgMapPath = Application.persistentDataPath + "/bgMap.json";

        // ���������, ���������� �� ����
        if (!File.Exists(mapPath))
        {
            // ���� ���� �� ����������, ������� � ���������� JSON
            JsonFile(mapPath, ProceduralGeneration.map);
            JsonFile(bgMapPath, ProceduralGeneration.bgMap);
            Debug.Log("JSON ���� ������: " + mapPath);
        }
        else
        {
            JsonFile(mapPath, ProceduralGeneration.map);
            JsonFile(bgMapPath, ProceduralGeneration.bgMap);
            Debug.Log("���� �����������: " + mapPath);
        }

        // �������� ����� ����
        SceneManager.LoadScene("_MainMenu");
    }
    // ����� �������� json �����
    void JsonFile(string path, int[,] map)
    {
        // �������������� ������� � JSON
        string json = JsonHelper.ToJson(map);

        // ������ JSON � ����
        File.WriteAllText(path, json);
    }
}


// ����� ��� ������ ������� � json
public static class JsonHelper
{
    public static string ToJson<T>(T[,] array)
    {
        // ������������ ���������� ������� � JSON
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
