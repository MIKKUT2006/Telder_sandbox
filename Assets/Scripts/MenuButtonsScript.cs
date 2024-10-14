using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonsScript : MonoBehaviour
{
    [SerializeField] private AudioSource AudioSource;
    [SerializeField] private TMP_InputField WorldSeed;
    [SerializeField] private TMP_InputField WorldName;
    public void PlayGame()
    {
        // Загрузка игры
        SceneManager.LoadScene("WorldOne");
        HelperClass.worldSeed = int.Parse(WorldSeed.text);
    }

}
