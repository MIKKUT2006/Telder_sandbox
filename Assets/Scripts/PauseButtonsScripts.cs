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
        string playerInventoryPath = Application.persistentDataPath + "/playerInventory.json";
        // ���������, ���������� �� ����
        if (!File.Exists(mapPath))
        {
            // ���� ���� �� ����������, ������� � ���������� JSON
            JsonWorld(mapPath, ProceduralGeneration.map);
            JsonWorld(bgMapPath, ProceduralGeneration.bgMap);
            JsonInventory(playerInventoryPath, HelperClass.playerInventory);
            Debug.Log("JSON ���� ������: " + mapPath);
        }
        else
        {
            JsonWorld(mapPath, ProceduralGeneration.map);
            JsonWorld(bgMapPath, ProceduralGeneration.bgMap);
            JsonInventory(playerInventoryPath, HelperClass.playerInventory);
            Debug.Log("���� �����������: " + mapPath);
        }

        // �������� ����� ����
        SceneManager.LoadScene("_MainMenu");
    }
    // ����� �������� json �����
    void JsonWorld(string path, int[,] map)
    {
        // �������������� ������� � JSON
        string json = JsonHelper.ToJson(map);

        // ������ JSON � ����
        File.WriteAllText(path, json);
    }

    // ����� �������� json �����
    void JsonInventory(string path, AllItemsAndBlocks[] inventory)
    {
        // �������������� ������� � JSON
        string json = JsonHelperArray.ToJson(inventory);

        // ������ JSON � ����
        File.WriteAllText(path, json);
    }
}


// ����� ��� ������ ���������� ������� � json
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

// ����� ��� ������ ����������� ������� � json
public static class JsonHelperArray
{
    public static string ToJson(AllItemsAndBlocks[] array)
    {
        // ������������ ���������� ������� � JSON
        List<AllItemsAndBlocks> list = array.ToList();

        return JsonUtility.ToJson(new Wrapper<AllItemsAndBlocks> { Items = list.ToArray() });
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}