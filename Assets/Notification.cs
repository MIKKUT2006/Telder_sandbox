using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour
{
    public TMP_InputField inputField; // Прикрепите Input Field в инспекторе
    public Canvas notificationCanvas; // Прикрепите Image (фон) в инспекторе
    public TextMeshProUGUI notificationText; // Прикрепите Text (текст) в инспекторе
    public float displayTime = 2f; // Время отображения уведомления

    private void Start()
    {
        notificationCanvas.gameObject.SetActive(false);
        notificationText.gameObject.SetActive(false);
    }

    public void CheckInputField()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            ShowNotification();
        }
    }


    private void ShowNotification()
    {
        notificationCanvas.gameObject.SetActive(true);
        notificationText.gameObject.SetActive(true);
        notificationText.text = $"Поле {gameObject.name} было пустое!";
        Invoke("HideNotification", displayTime);
    }

    private void HideNotification()
    {
        notificationCanvas.gameObject.SetActive(false);
        notificationText.gameObject.SetActive(false);
    }
}
