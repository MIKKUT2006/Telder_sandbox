using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private GameObject selectWorldsPanel;
    [SerializeField] private GameObject settingPanel;

    [SerializeField] private GameObject loadWorldPanel;
    [SerializeField] private GameObject createNewWorldPanel;
    [SerializeField] private GameObject multiplayerPanel;

    public void CreateWorld()
    {
        SceneManager.LoadScene("WorldOne");
        HelperClass.worldSeed = int.Parse(WorldSeed.text);
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

    public void LoadWorld()
    {
        createNewWorldPanel.SetActive(false);
        loadWorldPanel.SetActive(true);
        multiplayerPanel.SetActive(false);
    }

    public void Multiplayer()
    {
        createNewWorldPanel.SetActive(false);
        loadWorldPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }
}
