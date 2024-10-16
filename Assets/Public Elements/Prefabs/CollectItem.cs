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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ��������� ������� � ���������
        if (collision.gameObject.CompareTag("Player"))
        {
            bool inventoryIsFull = false;
            int InventoryCell = HelperClass.playerInventory.GetLength(0) - 1;
            // ������� ���� ����� ���������
            for (int i = HelperClass.playerInventory.GetLength(0) - 1; i >= 0; i--)
            {
                Debug.Log("������ �����" + InventoryCell);
                Debug.Log("� ��������� " + HelperClass.playerInventory[i]);
                if (HelperClass.playerInventory[i] != null && HelperClass.playerInventory[i].name == BlocksData.allBlocks.Find(x => x.blockIndex == int.Parse(gameObject.name)).name)
                {
                    HelperClass.playerInventory[i].count++;
                    Debug.Log($"� ��������� {HelperClass.playerInventory[i].count} �������� {HelperClass.playerInventory[i].name}");
                    Inventory.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                    Inventory.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();
                    Destroy(gameObject);
                    return;
                }
                else if (HelperClass.playerInventory[i] == null && int.Parse(gameObject.name) != 0)
                {
                    InventoryCell = i;
                    Debug.Log("��������� ������� � ������ " + InventoryCell);
                }
                else
                {
                    Debug.Log("�� ������ �� �������");
                }
            }
            if (!inventoryIsFull && InventoryCell != -1)
            {
                HelperClass.playerInventory[InventoryCell] = BlocksData.allBlocks.Find(x => x.blockIndex == int.Parse(gameObject.name));
                HelperClass.playerInventory[InventoryCell].count = 1;
                Debug.Log("� ��������� ��� ��������: " + HelperClass.playerInventory[InventoryCell].name);
                Inventory.transform.Find(InventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                Inventory.transform.Find(InventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = gameObject.GetComponent<SpriteRenderer>().sprite;
                Destroy(gameObject);
                if (HelperClass.selectedInventoryCell == InventoryCell)
                {
                    HelperClass.Cursor.SetActive(true);
                }
                Inventory.transform.Find(InventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                Inventory.transform.Find(InventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[InventoryCell].count.ToString();
            }
        }
    }
}
