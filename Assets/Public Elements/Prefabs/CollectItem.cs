using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CollectItem : MonoBehaviour
{
    [SerializeField] private GameObject Inventory;

    private void Start()
    {
        Inventory = HelperClass.playerInventoryGameObject;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ��������� ������� � ���������
        if (collision.gameObject.CompareTag("Player"))
        {
            HelperClass.AddItemToInventory(gameObject);
        }
    }
}
