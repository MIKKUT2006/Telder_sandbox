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

    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject selectWorldsPanel;
    [SerializeField] private GameObject settingPanel;

    [SerializeField] private GameObject loadWorldPanel;
    [SerializeField] private GameObject createNewWorldPanel;
    [SerializeField] private GameObject multiplayerPanel;

    public void CreateWorld()
    {
        // Показываем сообщение о загрузке мира
        loadPanel.SetActive(true);
        selectWorldsPanel.SetActive(false);
        // Запускам сцену игрового мира
        HelperClass.worldSeed = int.Parse(WorldSeed.text);
        SceneManager.LoadScene("WorldOne");
    }
    public void OpenWorldsPanel()
    {
        // Нажатие на кнопку "Играть"
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


    // Кнопки в выборе типа игры
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

        // Тестовая загрузка игрового мира
        HelperClass.isNewGame = false;
        HelperClass.map =  LoadJson(Application.persistentDataPath + "/map.json");
        HelperClass.bgMap =  LoadJson(Application.persistentDataPath + "/bgMap.json");

        SceneManager.LoadScene("WorldOne");
    }

    // Функция для чтения мира с json
    int[,] LoadJson(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
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
        else
        {
            Debug.Log("Файл не найден");
            return null;
        }
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    
}
