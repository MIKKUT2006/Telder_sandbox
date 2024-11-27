using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CraftiScript : MonoBehaviour
{
    public GameObject buttonPrefab; // Префаб кнопки
    public Transform buttonsParent; // Родитель для кнопок
    public GameObject ingredientPanel;
    public TextMeshProUGUI ingredientText;

    private void Start()
    {
        PopulateCraftingRecipes();
    }

    // Вывод всех рецептов крафта
    private void PopulateCraftingRecipes()
    {
        foreach (CraftingRecipe recipe in BlocksData.craftingRecipes) 
        {
            GameObject button = Instantiate(buttonPrefab, buttonsParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = recipe.item.name; // Название предмета
            button.GetComponent<InventoryElementSelect>().recipe = recipe;
            //button.GetComponent<InventoryElementSelect>().ingredientPanel = this.gameObject;
            button.GetComponent<InventoryElementSelect>().ingredientText = ingredientText;
            button.GetComponent<Button>().onClick.AddListener(() => CraftItem(recipe));
            // Здесь вы можете также добавить событие наведения мыши для показа необходимых ингредиентов
        }
    }
    private void CraftItem(CraftingRecipe recipe)
    {
        Debug.Log("Гойда СВО");
        // Проверка, есть ли у игрока необходимые ингредиенты
        if (HaveIngredients(recipe.ingredients))
        {
            // Уменьшаем количество ингредиентов в инвентаре
            DeductIngredients(recipe.ingredients);
            // Создаём новый предмет и добавляем в инвентарь
            AddItemToInventory(recipe.item.name);
        }
    }

    private bool HaveIngredients(List<Ingredient> ingredients)
    {
        foreach (Ingredient ingredient in ingredients)
        {
            int countInInventory = 0;

            // Считаем, сколько у игрока этого ингредиента
            foreach (AllItemsAndBlocks item in HelperClass.playerInventory)
            {
                if (item != null && item.name == ingredient.item.name)
                {
                    countInInventory += item.count; // учитываем количество предметов в инвентаре
                }
            }

            // Проверяем, достаточно ли ингредиента
            if (countInInventory < ingredient.quantity)
            {
                return false;
            }
        }
        return true;
    }

    // Уменьшение количества ингредиентов
    private void DeductIngredients(List<Ingredient> ingredients)
    {
        foreach (Ingredient ingredient in ingredients)
        {
            int remaining = ingredient.quantity;

            // Уменьшаем количество ингредиентов из инвентаря
            for (int i = 0; i < HelperClass.playerInventory.Length; i++)
            {
                if (HelperClass.playerInventory[i] != null && HelperClass.playerInventory[i].name == ingredient.item.name)
                {
                    if (HelperClass.playerInventory[i].count >= remaining)
                    {
                        HelperClass.playerInventory[i].count -= remaining;
                        remaining = 0;
                        break; // Достигли нужного количества, выходим
                    }
                    else
                    {
                        remaining -= HelperClass.playerInventory[i].count;
                        HelperClass.playerInventory[i].count = 0; // Обнуляем предмет, если он израсходован
                    }
                }
            }
        }
    }

    private void AddItemToInventory(string itemName)
    {
        // Ваша логика добавления предмета в инвентарь
    }
}
