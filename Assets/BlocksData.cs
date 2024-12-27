using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Класс, хранящй крепкость всех блоков в игре
public class BlocksData : MonoBehaviour
{

    // Пути к изображениям
    //"Assets/Blocks/Firstworld/"{ИМЯ}".png"
    // Верхний мир блоки


    static string firstWorldBlocks = "Assets/_Blocks/Firstworld/";

    public static List<AllItemsAndBlocks> allBlocks = new List<AllItemsAndBlocks> {
        // Блоки верхнего мира
         new AllItemsAndBlocks(0, "Свет", 0, true, 100, $"{firstWorldBlocks}Light.png"),
         //new AllItemsAndBlocks(1, "Земля", 3, true,100, $"{firstWorldBlocks}Dirt.png"),
         new AllItemsAndBlocks(1, "Земля", 3, true,100, new List<int>{3,3,6}, 3),
         new AllItemsAndBlocks(2, "Трава", 3, true, 100, $"{firstWorldBlocks}Grass.png"),
         new AllItemsAndBlocks(3, "Камень", 15, true, 100, $"{firstWorldBlocks}Stone.png"),
         new AllItemsAndBlocks(4, "Пустота", 0, true, 100, ""),
         new AllItemsAndBlocks(5, "Трава с деревьями", 3, true, 100, $"{firstWorldBlocks}Dirt.png"),
         new AllItemsAndBlocks(6, "Железная руда", 20, true, 100, $"{firstWorldBlocks}/Ores/Iron Ore.png"),
         // Блок с выпадением из него предметов
         new AllItemsAndBlocks(7, "Руда телепортиума", 30, true, 100, new List<int>{1,1,1}, 3),
         new AllItemsAndBlocks(8, "Баррьер", int.MaxValue, true, 100, ""),
         new AllItemsAndBlocks(9, "Песок", 1, true, 100, $"{firstWorldBlocks}Sand.png"),   
         new AllItemsAndBlocks(10, "Окаменевший кристалл", 3, true, 100, ""),   
         new AllItemsAndBlocks(11, "Снег", 3, true, 100, $"{firstWorldBlocks}Snow.png"),   
         new AllItemsAndBlocks(12, "Листва", 2, true, 100, $"{firstWorldBlocks}Leaves.png"),
         // Предметы верхнего мира
         new AllItemsAndBlocks(13, "Кирка древних", "Эта кирка принадлежит древним путешественникам по вселенным", $"{firstWorldBlocks}/Tools/GrassPickaxe.png"),
         //new AllItemsAndBlocks(14, "Телепортиум", "Этот кристалл излучает странный, манящий свет"),
    };

    public static List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>
    {
        // Кирка древних
        new CraftingRecipe(allBlocks[5], new List<Ingredient> {
            new Ingredient(allBlocks[12], 5), // Листва
            new Ingredient(allBlocks[12], 5), // Листва
        }),
        // Добавьте другие рецепты здесь
    };
}

// Класс игровых предметов
[System.Serializable]
public class AllItemsAndBlocks
{
    public int blockIndex;
    public string name;
    public string description;
    public int blocksSolidity;
    public bool isBlock = true;

    // Что выпадет при разрушении
    //public List<AllItemsAndBlocks> drop = new List<AllItemsAndBlocks>();
    public List<int> dropId = new List<int>();
    public int dropCount;

    public int damage = 0;
    public bool stackable;
    public int maxStack;
    public int count = 0;
    // Начальная прочность предмета и последующая прочность предмета
    public int startDurable = 0;
    public int durable = 0;

    public int toolType;
    // 1 - Кирка
    // 2 - Топор
    // 3 - Оружие

    // Поле для хранения пути к изображению
    public string imagePath;

    // Конструкторы класса для оружия
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

    // Конструкторы класса для предмета (ингредиенты для крафта)
    public AllItemsAndBlocks(int _blockIndex, string _name, string _description, string _imagePath)
    {
        blockIndex = _blockIndex;
        name = _name;
        isBlock = false;
        description = _description;
        imagePath= _imagePath;
    }

    // Конструктор класса для блока
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

    // Конструктор класса для блока с выпаднием другого предмета
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

// Предложенное нейронкой
[System.Serializable]
public class CraftingRecipe
{
    public AllItemsAndBlocks item; // Название создаваемого предмета
    public List<Ingredient> ingredients; // Список ингредиентов с количеством

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