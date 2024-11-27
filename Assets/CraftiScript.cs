using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CraftiScript : MonoBehaviour
{
    public GameObject buttonPrefab; // ������ ������
    public Transform buttonsParent; // �������� ��� ������
    public GameObject ingredientPanel;
    public TextMeshProUGUI ingredientText;

    private void Start()
    {
        PopulateCraftingRecipes();
    }

    // ����� ���� �������� ������
    private void PopulateCraftingRecipes()
    {
        foreach (CraftingRecipe recipe in BlocksData.craftingRecipes) 
        {
            GameObject button = Instantiate(buttonPrefab, buttonsParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = recipe.item.name; // �������� ��������
            button.GetComponent<InventoryElementSelect>().recipe = recipe;
            //button.GetComponent<InventoryElementSelect>().ingredientPanel = this.gameObject;
            button.GetComponent<InventoryElementSelect>().ingredientText = ingredientText;
            button.GetComponent<Button>().onClick.AddListener(() => CraftItem(recipe));
            // ����� �� ������ ����� �������� ������� ��������� ���� ��� ������ ����������� ������������
        }
    }
    private void CraftItem(CraftingRecipe recipe)
    {
        Debug.Log("����� ���");
        // ��������, ���� �� � ������ ����������� �����������
        if (HaveIngredients(recipe.ingredients))
        {
            // ��������� ���������� ������������ � ���������
            DeductIngredients(recipe.ingredients);
            // ������ ����� ������� � ��������� � ���������
            AddItemToInventory(recipe.item.name);
        }
    }

    private bool HaveIngredients(List<Ingredient> ingredients)
    {
        foreach (Ingredient ingredient in ingredients)
        {
            int countInInventory = 0;

            // �������, ������� � ������ ����� �����������
            foreach (AllItemsAndBlocks item in HelperClass.playerInventory)
            {
                if (item != null && item.name == ingredient.item.name)
                {
                    countInInventory += item.count; // ��������� ���������� ��������� � ���������
                }
            }

            // ���������, ���������� �� �����������
            if (countInInventory < ingredient.quantity)
            {
                return false;
            }
        }
        return true;
    }

    // ���������� ���������� ������������
    private void DeductIngredients(List<Ingredient> ingredients)
    {
        foreach (Ingredient ingredient in ingredients)
        {
            int remaining = ingredient.quantity;

            // ��������� ���������� ������������ �� ���������
            for (int i = 0; i < HelperClass.playerInventory.Length; i++)
            {
                if (HelperClass.playerInventory[i] != null && HelperClass.playerInventory[i].name == ingredient.item.name)
                {
                    if (HelperClass.playerInventory[i].count >= remaining)
                    {
                        HelperClass.playerInventory[i].count -= remaining;
                        remaining = 0;
                        break; // �������� ������� ����������, �������
                    }
                    else
                    {
                        remaining -= HelperClass.playerInventory[i].count;
                        HelperClass.playerInventory[i].count = 0; // �������� �������, ���� �� ������������
                    }
                }
            }
        }
    }

    private void AddItemToInventory(string itemName)
    {
        // ���� ������ ���������� �������� � ���������
    }
}
