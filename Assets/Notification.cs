using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour
{
    public TMP_InputField inputField; // ���������� Input Field � ����������
    public Canvas notificationCanvas; // ���������� Image (���) � ����������
    public TextMeshProUGUI notificationText; // ���������� Text (�����) � ����������
    public float displayTime = 2f; // ����� ����������� �����������

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
        notificationText.text = $"���� {gameObject.name} ���� ������!";
        Invoke("HideNotification", displayTime);
    }

    private void HideNotification()
    {
        notificationCanvas.gameObject.SetActive(false);
        notificationText.gameObject.SetActive(false);
    }
}
