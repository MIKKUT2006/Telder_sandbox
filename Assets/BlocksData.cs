using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// �����, ������� ��������� ���� ������ � ����
public class BlocksData : MonoBehaviour
{
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
         new AllItemsAndBlocks(9, "�����", 1, true, 100),   
         new AllItemsAndBlocks(10, "����������� ��������", 3, true, 100),   
         new AllItemsAndBlocks(11, "����", 3, true, 100),   
         new AllItemsAndBlocks(12, "���", 3, true, 100),
         // �������� �������� ����
         new AllItemsAndBlocks(13, "����� �������", "��� ����� ����������� ������� ���������������� �� ���������"),
         new AllItemsAndBlocks(14, "�����������", "���� �������� �������� ��������, ������� ����"),
    };

    public static List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>
    {
        new CraftingRecipe(allBlocks[5], new List<Ingredient> {
            new Ingredient(allBlocks[1], 3), // 1 �����
        }),
        new CraftingRecipe(allBlocks[0], new List<Ingredient> {
            new Ingredient(allBlocks[3], 3), // 3 �����
            new Ingredient(allBlocks[1], 2)  // 2 ����� ��� ������ ���������, ��������, ��� �����
        }),
        // �������� ������ ������� �����
    };
}

// ����� ������� ���������
[System.Serializable]
public class AllItemsAndBlocks
{
    public int blockIndex;
    public string name;
    public string description;
    public int blocksSolidity;
    public bool isBlock = true;

    // ��� ������� ��� ����������
    public AllItemsAndBlocks drop;
    public int dropCount;

    public int damage = 0;
    public bool stackable;
    public int maxStack;
    public int count = 0;
    // ��������� ��������� �������� � ����������� ��������� ��������
    public int startDurable = 0;
    public int durable = 0;

    // ���� ��� �������� ���� � �����������
    public string imagePath;

    // ������������ ������ ��� ������
    public AllItemsAndBlocks(int _blockIndex, string _name,  int _damage, string _description)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
        damage = _damage;
        description = _description;
    }

    // ������������ ������ ��� ��������
    public AllItemsAndBlocks(int _blockIndex, string _name, string _description)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
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

    // ����������� ������ ��� ����� � ��������� ������� ��������
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack, AllItemsAndBlocks _drop, int _dropCount)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;

        drop = _drop;
        dropCount = _dropCount;
    }
}

// ������������ ���������
[System.Serializable]
public class CraftingRecipe
{
    public AllItemsAndBlocks item; // �������� ������������ ��������
    public List<Ingredient> ingredients; // ������ ������������ � �����������

    public CraftingRecipe(AllItemsAndBlocks item, List<Ingredient> ingredients)
    {
        this.item = item;
        this.ingredients = ingredients;
    }
}

[System.Serializable]
public class Ingredient
{
    public AllItemsAndBlocks item;
    public int quantity;

    public Ingredient(AllItemsAndBlocks item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}