using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Класс, хранящй крепкость всех блоков в игре
public class BlocksData : MonoBehaviour
{
    public static List<int> blocksSolidity = new List<int> {
         0, //солнечный свет
         3, //трава
         3, //земля
         15, //камень
         0, //пустота
         3, //трава с деревьями
         1, //трава
         1, //трава
    };

    public static List<AllItemsAndBlocks> allBlocks = new List<AllItemsAndBlocks> {
        // Блоки верхнего мира
         new AllItemsAndBlocks(0, "Свет", 0, true, 100),
         new AllItemsAndBlocks(1, "Земля", 3, true,100),
         new AllItemsAndBlocks(2, "Трава", 3, true, 100),
         new AllItemsAndBlocks(3, "Камень", 15, true, 100),
         new AllItemsAndBlocks(4, "Пустота", 0, true, 100),
         new AllItemsAndBlocks(5, "Трава с деревьями", 3, true, 100),
         new AllItemsAndBlocks(6, "Железная руда", 20, true, 100),
         new AllItemsAndBlocks(7, "Руда телепортиума", 30, true, 100),
         new AllItemsAndBlocks(8, "Баррьер", int.MaxValue, true, 100),
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
    // Начальная прочность предмета и последующая прочность предмета
    public int startDurable = 0;
    public int durable = 0;

    // Конструкторы класса для оружия

    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, int _damage, string _description)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        isBlock = false;
        damage = _damage;
        description = _description;
    }

    // Конструктор класса для блока
    public AllItemsAndBlocks(int _blockIndex, string _name, int _blocksSolidity, bool _stackable, int _maxStack)
    {
        blockIndex = _blockIndex;
        name = _name;
        blocksSolidity = _blocksSolidity;
        stackable = _stackable;
        maxStack = _maxStack;
    }
}
