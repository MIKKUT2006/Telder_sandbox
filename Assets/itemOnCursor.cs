using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class itemOnCursor : MonoBehaviour
{
    public RectTransform rectTransform; // ������ �� RectTransform �������
    public Canvas canvas;              // ������ �� Canvas
    void Start()
    {
        HelperClass.itemOnCursorGameObject = gameObject;
        gameObject.GetComponent<Image>().enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);

        // ����������� ������� ���� �� �������� ��������� � ���������� Canvas
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), Input.mousePosition, canvas.worldCamera, out pos);

        // ��������� ���������� ������� � RectTransform
        rectTransform.anchoredPosition = pos;

    }
}
