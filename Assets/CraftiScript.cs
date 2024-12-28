using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CraftiScript : MonoBehaviour
{
    public GameObject buttonPrefab; // Префаб кнопки
    public Transform buttonsParent; // Родитель для кнопок
    public GameObject ingredientPanel;
    public GameObject ingredientPrefab;
    public TextMeshProUGUI ingredientText;
    public TextMeshProUGUI recipeItemName;

    private void Start()
    {
        PopulateCraftingRecipes();
    }

    // Вывод всех рецептов крафта всех предметов
    private void PopulateCraftingRecipes()
    {
        foreach (CraftingRecipe recipe in BlocksData.craftingRecipes)
        {
            GameObject button = Instantiate(buttonPrefab, buttonsParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = recipe.item.name; // Название предмета
            button.name = recipe.item.blockIndex.ToString();
            button.GetComponent<InventoryElementSelect>().recipe = recipe;
            //button.GetComponent<InventoryElementSelect>().ingredientPanel = this.gameObject;
            button.GetComponent<InventoryElementSelect>().ingredientPanel = ingredientPanel;
            button.GetComponent<InventoryElementSelect>().ingredientPrefab = ingredientPrefab;
            button.GetComponent<InventoryElementSelect>().ingredientText = ingredientText;
            button.GetComponent<InventoryElementSelect>().recipeItemName = recipeItemName;
            button.GetComponent<Button>().onClick.AddListener(() => CraftItem(recipe, button));
            button.transform.Find("Image").GetComponent<Image>().enabled = true;
            // Здесь вы можете также добавить событие наведения мыши для показа необходимых ингредиентов

            float pixelsPerUnit = 16;

            if (!string.IsNullOrEmpty(recipe.item.imagePath) && File.Exists(recipe.item.imagePath) && recipe.item.imagePath != null)
            {
                // �������� �������� �� �����
                byte[] imageData = File.ReadAllBytes(recipe.item.imagePath);
                Texture2D texture = new Texture2D(16, 16);
                texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
                texture.filterMode = FilterMode.Point;

                // ������������ ������� ������� � ������ pixelsPerUnit
                float width = texture.width / 16;
                float height = texture.height / 16;

                // �������� ������� �� ��������
                Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                //playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                button.transform.Find("Image").GetComponent<Image>().sprite = newSprite;
            }
        }
    }
    private void CraftItem(CraftingRecipe recipe, GameObject cellGameObject)
    {

        // Проверка, есть ли у игрока необходимые ингредиенты
        if (HaveIngredients(recipe.ingredients))
        {
            // Уменьшаем количество ингредиентов в инвентаре
            DeductIngredients(recipe.ingredients);
            // Создаём новый предмет и добавляем в инвентарь
            AddItemToInventory(cellGameObject);
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

    private void AddItemToInventory(GameObject cell)
    {
        // Ваша логика добавления предмета в инвентарь
        Debug.Log("Предмет создан");
        HelperClass.AddCraftedItemToInventory(cell);
    }
}
