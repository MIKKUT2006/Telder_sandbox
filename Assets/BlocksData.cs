using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// �����, ������� ��������� ���� ������ � ����
public class BlocksData : MonoBehaviour
{
    public static GameObject torchPrefab = null;
    public static GameObject bombprefab = null;
    public static string currentDataPack = "TheGame";
    static string firstWorldBlocks = $"{currentDataPack}/_Blocks/Firstworld/";
    public static void GetPrefabs()
    {
        torchPrefab = (GameObject)Resources.Load($"Torch");
        allBlocks[19].prefab = torchPrefab;

        bombprefab = (GameObject)Resources.Load($"Prefabs/Projectiles/Bomb/miniBomb");
        bombprefab = (GameObject)Resources.Load($"miniBomb");
        //Assets/Resources/Prefabs/Projectiles/Bomb/miniBomb.prefab
        allBlocks[20].prefab = bombprefab;
    }



    public static List<AllItemsAndBlocks> allBlocks = new List<AllItemsAndBlocks> {
        // ����� �������� ����
         new AllItemsAndBlocks(0, "����", 0, true, 100, $"{firstWorldBlocks}Light", 0),
         //new AllItemsAndBlocks(1, "�����", 3, true,100, $"{firstWorldBlocks}Dirt.png"),
         new AllItemsAndBlocks(1, "�����", 3, true,100, $"{firstWorldBlocks}Dirt", 3),
         new AllItemsAndBlocks(2, "�����", 3, true, 100, $"{firstWorldBlocks}Grass", 3),
         new AllItemsAndBlocks(3, "������", 15, true, 100, $"{firstWorldBlocks}Stone", 1),
         new AllItemsAndBlocks(4, "�������", 0, true, 100, "", 0),
         new AllItemsAndBlocks(5, "����� � ���������", 3, true, 100, $"{firstWorldBlocks}Dirt", 3),
         new AllItemsAndBlocks(6, "�������� ����", 20, true, 100, $"{firstWorldBlocks}Ores/Iron Ore", new List<int>{14,14,14}, 3, 1),
         // ���� � ���������� �� ���� ���������
         new AllItemsAndBlocks(7, "���� ������������", 30, true, 100, $"{firstWorldBlocks}Ores/TeleportiumOre", new List<int>{1,1,1}, 3, 1),
         new AllItemsAndBlocks(8, "�������", int.MaxValue, true, 100, "", 0),
         new AllItemsAndBlocks(9, "�����", 1, true, 100, $"{firstWorldBlocks}Sand", 3),   
         new AllItemsAndBlocks(10, "����������� ��������", 3, true, 100, "", 1),   
         new AllItemsAndBlocks(11, "����", 3, true, 100, $"{firstWorldBlocks}Snow", 3),   
         new AllItemsAndBlocks(12, "������", 2, true, 100, $"{firstWorldBlocks}Leaves", 0),
         // �������� �������� ����
         new AllItemsAndBlocks(13, "����� �������", "��� ����� ����������� ������� ���������������� �� ���������", $"Tools/GrassPickaxe", 1, 2, 30),
         new AllItemsAndBlocks(14, "����� ������", "������ ����� �������", $"{firstWorldBlocks}Ores/Iron"),
         // �������
         new AllItemsAndBlocks(15, "�������� ���������", 3, true, 100, $"{firstWorldBlocks}Trees/PinePlanks", 3),
         new AllItemsAndBlocks(16, "�����", new List<int>{15,15,15,15,15}, 5, 2),
         new AllItemsAndBlocks(17, "�������� ����", 15, true, 100,$"{firstWorldBlocks}Ores/CoalOre", new List<int>{18,18,18}, 3, 1),
         new AllItemsAndBlocks(18, "����� ����", "�������� �������", $"{firstWorldBlocks}Ores/Coal"),
         new AllItemsAndBlocks(19, "�����", "�������� ����", $"{firstWorldBlocks}Furniture/torch", 0, torchPrefab, true),
         new AllItemsAndBlocks(20, "�����", "������ ���", $"Prefabs/Projectiles/Bomb/Bomb", 0, bombprefab, true),
         new AllItemsAndBlocks(21, "��� �������", "�� �� ���������� �������", $"Tools/GrassSword", 4, 9, 0),
         new AllItemsAndBlocks(22, "����� �������", "��� ��� ���", $"Tools/ElderAxe", 2, 3, 3),
         new AllItemsAndBlocks(23, "�����", "������� �����", $"Items/Coal"),
    };

    public static List<GameObject> objects = new List<GameObject> {
    };

    //------------------------------------------------------

    public static List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>
    {
        // ����� �������
        new CraftingRecipe(allBlocks[21], new List<Ingredient> {
            new Ingredient(allBlocks[15], 3), // ���������
        }),
        // �����
        new CraftingRecipe(allBlocks[19], new List<Ingredient> {
            new Ingredient(allBlocks[15], 1), // ���������
        }),
        // �����
        new CraftingRecipe(allBlocks[20], new List<Ingredient> {
            new Ingredient(allBlocks[18], 6), // �����
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

    // ��� ������������ ����������� ��� ����������
    public int needsToolType;
    // 0 - �����
    // 1 - �����
    // 2 - �����
    // 3 - ������
    // 3 - ������

    // ��� ������� ��� ����������
    //public List<AllItemsAndBlocks> drop = new List<AllItemsAndBlocks>();
    public List<int> dropId = new List<int>();
    public int dropCount;

    public bool stackable;
    public int maxStack;
    public int count = 0;
    // ��������� ��������� �������� � ����������� ��������� ��������
    public int startDurable = 0;
    public int durable = 0;

    // ��� ����������� ��� ��������
    public int toolType;
    // 1 - �����
    // 2 - �����
    // 3 - ������
    // 4 - ������

    // ���� �����������
    public int toolPower = 0;
    //public int axePower = 0;
    //public int shovelPower = 0;

    public int damage = 0;

    // ������ ��� �������
    public GameObject prefab;
    public bool isObject = false;

    // ���� ��� �������� ���� � �����������
    public string imagePath;

    // ������������ ������ ��� �����������/������
    public AllItemsAndBlocks(int _blockIndex, string _name, string _description, string _imagePath, int _toolType, int _damage, int _toolPower)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
        damage = _damage;
        description = _description;
        this.toolType = _toolType;
        imagePath = _imagePath;

        // �������� ������������
        this.toolPower = _toolPower;
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
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack, string _imagePath, int needsToolType)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;
        imagePath = _imagePath;
        dropId.Add(this.blockIndex);
        this.needsToolType = needsToolType;
    }

    // ����������� ������ ��� ����� � ��������� ������� ��������
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack, string _imagePath, List<int> _dropId, int _dropCount, int needsToolType)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;
        imagePath = _imagePath;
        dropId = _dropId;
        dropCount = _dropCount;

        this.needsToolType = needsToolType;
    }

    // ����������� ������ ��� ������� � ��������� ������� �������� (�������� ������)
    public AllItemsAndBlocks(int _blockIndex, string _name, List<int> _dropId, int _dropCount, int needsToolType)
    {
        blockIndex = _blockIndex;
        name = _name;

        dropId = _dropId;
        dropCount = _dropCount;

        this.needsToolType = needsToolType;
    }

    // ����������� ������ ��� �������
    public AllItemsAndBlocks(int _blockIndex, string _name, string _description, string _imagePath, int needsToolType, GameObject prefab, bool isObject)
    {
        blockIndex = _blockIndex;
        name = _name;

        dropId.Add(this.blockIndex);
        this.needsToolType = needsToolType;
        this.prefab = prefab;
        this.imagePath = _imagePath;
        this.description = _description;
        this.isObject = isObject;
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