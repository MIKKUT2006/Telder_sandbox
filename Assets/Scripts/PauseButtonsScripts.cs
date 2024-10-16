using System.Collections;
using System.Collections.Generic;
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
        SceneManager.LoadScene("_MainMenu");
    }
}
