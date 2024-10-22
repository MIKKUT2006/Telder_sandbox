using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// �����, ������� ��������� ���� ������ � ����
public class BlocksData : MonoBehaviour
{
    public static List<int> blocksSolidity = new List<int> {
         0, //��������� ����
         3, //�����
         3, //�����
         15, //������
         0, //�������
         3, //����� � ���������
         1, //�����
         1, //�����
    };

    public static List<AllItemsAndBlocks> allBlocks = new List<AllItemsAndBlocks> {
        // ����� �������� ����
         new AllItemsAndBlocks(0, "����", 0, true, 100),
         new AllItemsAndBlocks(1, "�����", 3, true,100),
         new AllItemsAndBlocks(2, "�����", 3, true, 100),
         new AllItemsAndBlocks(3, "������", 15, true, 100),
         new AllItemsAndBlocks(4, "�������", 0, true, 100),
         new AllItemsAndBlocks(5, "����� � ���������", 3, true, 100),
         new AllItemsAndBlocks(6, "�������� ����", 20, true, 100),
         new AllItemsAndBlocks(7, "���� ������������", 30, true, 100),
         new AllItemsAndBlocks(8, "�������", int.MaxValue, true, 100),
    };
}
[System.Serializable]
public class AllItemsAndBlocks
{
    public int blockIndex;
    public string name;
    public string description;
    public int blocksSolidity;
    public bool isBlock = true;
    public int damage = 0;
    public bool stackable;
    public int maxStack;
    public int count = 0;
    // ��������� ��������� �������� � ����������� ��������� ��������
    public int startDurable = 0;
    public int durable = 0;

    // ������������ ������ ��� ������

    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, int _damage, string _description)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        isBlock = false;
        damage = _damage;
        description = _description;
    }

    // ����������� ������ ��� �����
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;
    }
}
