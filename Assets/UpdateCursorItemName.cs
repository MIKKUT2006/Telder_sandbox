using TMPro;
using UnityEngine;

public class UpdateCursorItemName : MonoBehaviour
{
    public void OnMouseEnter()
    {
        GameObject.FindGameObjectWithTag("itemName").GetComponent<TextMeshProUGUI>().text = gameObject.name;
    }

    public void OnMouseExit() 
    {
        GameObject.FindGameObjectWithTag("itemName").GetComponent<TextMeshProUGUI>().text = "";
    }
}
