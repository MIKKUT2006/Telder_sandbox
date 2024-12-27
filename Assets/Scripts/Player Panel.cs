using UnityEngine;

public class PlayerPanel : MonoBehaviour
{
    public GameObject playerBodyTypePaintPanel;
    public GameObject colorPickerPanel;

    public void ShowColorPicker()
    {
        if (colorPickerPanel.activeSelf == true) 
        {
            colorPickerPanel.SetActive(false);
            playerBodyTypePaintPanel.SetActive(false);
        }
        else
        {
            colorPickerPanel.SetActive(true);
            playerBodyTypePaintPanel.SetActive(true);
        }
    }
}
