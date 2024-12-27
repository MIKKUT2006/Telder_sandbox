using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// �����, ������� ��������� ���� ������ � ����
public class BlocksData : MonoBehaviour
{

    // ���� � ������������
    //"Assets/Blocks/Firstworld/"{���}".png"
    // ������� ��� �����


    static string firstWorldBlocks = "Assets/_Blocks/Firstworld/";

    public static List<AllItemsAndBlocks> allBlocks = new List<AllItemsAndBlocks> {
        // ����� �������� ����
         new AllItemsAndBlocks(0, "����", 0, true, 100, $"{firstWorldBlocks}Light.png"),
         //new AllItemsAndBlocks(1, "�����", 3, true,100, $"{firstWorldBlocks}Dirt.png"),
         new AllItemsAndBlocks(1, "�����", 3, true,100, new List<int>{3,3,6}, 3),
         new AllItemsAndBlocks(2, "�����", 3, true, 100, $"{firstWorldBlocks}Grass.png"),
         new AllItemsAndBlocks(3, "������", 15, true, 100, $"{firstWorldBlocks}Stone.png"),
         new AllItemsAndBlocks(4, "�������", 0, true, 100, ""),
         new AllItemsAndBlocks(5, "����� � ���������", 3, true, 100, $"{firstWorldBlocks}Dirt.png"),
         new AllItemsAndBlocks(6, "�������� ����", 20, true, 100, $"{firstWorldBlocks}/Ores/Iron Ore.png"),
         // ���� � ���������� �� ���� ���������
         new AllItemsAndBlocks(7, "���� ������������", 30, true, 100, new List<int>{1,1,1}, 3),
         new AllItemsAndBlocks(8, "�������", int.MaxValue, true, 100, ""),
         new AllItemsAndBlocks(9, "�����", 1, true, 100, $"{firstWorldBlocks}Sand.png"),   
         new AllItemsAndBlocks(10, "����������� ��������", 3, true, 100, ""),   
         new AllItemsAndBlocks(11, "����", 3, true, 100, $"{firstWorldBlocks}Snow.png"),   
         new AllItemsAndBlocks(12, "������", 2, true, 100, $"{firstWorldBlocks}Leaves.png"),
         // �������� �������� ����
         new AllItemsAndBlocks(13, "����� �������", "��� ����� ����������� ������� ���������������� �� ���������", $"{firstWorldBlocks}/Tools/GrassPickaxe.png"),
         //new AllItemsAndBlocks(14, "�����������", "���� �������� �������� ��������, ������� ����"),
    };

    public static List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>
    {
        // ����� �������
        new CraftingRecipe(allBlocks[5], new List<Ingredient> {
            new Ingredient(allBlocks[12], 5), // ������
            new Ingredient(allBlocks[12], 5), // ������
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
    //public List<AllItemsAndBlocks> drop = new List<AllItemsAndBlocks>();
    public List<int> dropId = new List<int>();
    public int dropCount;

    public int damage = 0;
    public bool stackable;
    public int maxStack;
    public int count = 0;
    // ��������� ��������� �������� � ����������� ��������� ��������
    public int startDurable = 0;
    public int durable = 0;

    public int toolType;
    // 1 - �����
    // 2 - �����
    // 3 - ������

    // ���� ��� �������� ���� � �����������
    public string imagePath;

    // ������������ ������ ��� ������
    public AllItemsAndBlocks(int _blockIndex, string _name,  int _damage, string _description, int _toolType, string _imagePath)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
        damage = _damage;
        description = _description;
        this.toolType = _toolType;
        imagePath = _imagePath;
    }

    // ������������ ������ ��� �������� (����������� ��� ������)
    public AllItemsAndBlocks(int _blockIndex, string _name, string _description, string _imagePath)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
        description = _description;
        imagePath= _imagePath;
    }

    // ����������� ������ ��� �����
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack, string _imagePath)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;
        imagePath = _imagePath;
        dropId.Add(this.blockIndex);
    }

    // ����������� ������ ��� ����� � ��������� ������� ��������
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack, List<int> _dropId, int _dropCount)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;

        dropId = _dropId;
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