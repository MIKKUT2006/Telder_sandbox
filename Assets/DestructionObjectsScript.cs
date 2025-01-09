using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DestructionObjectsScript : MonoBehaviour
{
    [Header ("��������� �������")]
    public int objectDurable = 0;
    [Header("������ ����� � ��������")]
    public List<itemDrop> drop = new List<itemDrop>
    {
        new itemDrop(17, 4),
    };

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name == "Item" && HelperClass.playerGameObject.GetComponent<Animator>().GetBool("Attack"))
        {
            Debug.Log("���� �� �������");
            // ���� ��� �������� ���� ���������� ��������� � �����
            foreach (itemDrop drop in drop)
            {
                for (int i = 0; i < drop.count; i++)
                {
                    Vector3 newpos = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f);
                    AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop.id).FirstOrDefault();
                    Debug.Log(currentDrop.name);
                    GameObject newBlock = Instantiate(HelperClass.BlockGameObject, newpos, Quaternion.identity);
                    newBlock.name = currentDrop.blockIndex.ToString();
                    Sprite sprite = null;
                    // ��������� ��� ��������, ���� ��� ����
                    Sprite newSprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));

                    newBlock.GetComponent<SpriteRenderer>().sprite = newSprite;
                }
            }
            // ����� �����
            Destroy(gameObject);
        }
    }

    public class itemDrop
    {
        public int id;
        public int count;
        public itemDrop(int _id, int _count)
        {
            id = _id;
            count = _count;
        }
    }
}


