using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryElementSelect : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject Inventory;
    private Image cellImage;
    Sprite tempSprite = null;
    AllItemsAndBlocks tempItem = null;
    int tempItemCount = 0;

    // Рецепты крафта
    [SerializeField] bool isCraftCell = false;
    public CraftingRecipe recipe; // Рецепт, связанный с данной кнопкой
    public GameObject ingredientPanel; // Панель для отображения ингредиентов

    public GameObject ingredientPrefab;     // Префаб для ингредиента крафта

    public TextMeshProUGUI ingredientText; // Текст, отображающий ингредиенты
    public TextMeshProUGUI recipeItemName; // Рецепт, связанный с данной кнопкой


    private void Awake()
    {
        cellImage = GetComponent<Image>();
        if (gameObject.GetComponentInParent<ContentSizeFitter>().CompareTag("Inventory"))
        {
            isCraftCell = false;
        }
        else
        {
            isCraftCell = true;
        }

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Проверка на нажатие правой кнопки мыши
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Проверка, что курсором был взят предмет
            if (ItemOnCursor.selecteditem != null)
            {
                // Проверка, что уже есть предмет в этом слоте
                if (HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled == true)
                {
                    // Проверка, что предмет в слоте совпадает с предметом в курсоре
                    if (HelperClass.playerInventory[int.Parse(gameObject.name)].blockIndex == ItemOnCursor.selecteditem.blockIndex)
                    {
                        //Debug.Log(HelperClass.playerInventory[int.Parse(gameObject.name)].name);

                        HelperClass.playerInventory[int.Parse(gameObject.name)].count++;
                        ItemOnCursor.selecteditem.count--;
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[int.Parse(gameObject.name)].count.ToString();

                        if (ItemOnCursor.selecteditem.count == 0)
                        {
                            ItemOnCursor.selecteditem = null;
                            HelperClass.itemOnCursorGameObject.GetComponent<Image>().enabled = false;
                        }
                    }
                }
                else
                {
                    HelperClass.playerInventory[int.Parse(gameObject.name)] = InventoryItemClone(ItemOnCursor.selecteditem);
                    HelperClass.playerInventory[int.Parse(gameObject.name)].count = 1;
                    Debug.Log($"Количество в ячейке {HelperClass.playerInventory[int.Parse(gameObject.name)].count}");
                    ItemOnCursor.selecteditem.count--;
                    Debug.Log($"В курсоре осталось {ItemOnCursor.selecteditem.count} предмета");
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[int.Parse(gameObject.name)].count.ToString();
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled = true;
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite = ItemOnCursor.sprite;

                    if (ItemOnCursor.selecteditem.count == 0)
                    {
                        ItemOnCursor.selecteditem = null;
                        HelperClass.itemOnCursorGameObject.GetComponent<Image>().enabled = false;
                    }
                }
            }
            else
            {
                // Выбор предмета в инвентаре
                HelperClass.selectedInventoryCell = int.Parse(gameObject.name);
                HelperClass.equippedCellAnimator = GetComponent<Animation>();

                if (HelperClass.playerInventory[int.Parse(gameObject.name)] != null)
                {
                    HelperClass.itemName.text = HelperClass.playerInventory[int.Parse(gameObject.name)].name;
                }
                else
                {
                    HelperClass.itemName.text = "";
                }
                Debug.Log(HelperClass.equippedCellAnimator);
                if (HelperClass.equippedCellImage != null)
                {
                    HelperClass.equippedCellImage.color = Color.white;
                }
                cellImage.color = new Color32(47, 192, 255, 255);
                HelperClass.equippedCellImage = cellImage;
                HelperClass.equippedItem.GetComponent<SpriteRenderer>().enabled = true;
                HelperClass.equippedItem.GetComponent<SpriteRenderer>().sprite = HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
                if (HelperClass.playerInventory[HelperClass.selectedInventoryCell] != null && HelperClass.playerInventory[HelperClass.selectedInventoryCell].isBlock == true)
                {
                    HelperClass.Cursor.GetComponent<SpriteRenderer>().enabled = true;
                }
                else
                {
                    HelperClass.Cursor.GetComponent<SpriteRenderer>().enabled = false;
                }
            }
        }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isCraftCell == false)
            {
                if (HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled != false)
                {
                    if (ItemOnCursor.selecteditem != null)
                    {
                        HelperClass.playerInventoryGameObject.transform.Find(ItemOnCursor.firstCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                        HelperClass.playerInventoryGameObject.transform.Find(ItemOnCursor.firstCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;

                        HelperClass.playerInventoryGameObject.transform.Find(ItemOnCursor.firstCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite;
                        HelperClass.playerInventoryGameObject.transform.Find(ItemOnCursor.firstCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().text;

                        //Debug.Log(HelperClass.playerInventory[int.Parse(gameObject.name)].count);
                        //Debug.Log(HelperClass.playerInventory[ItemOnCursor.firstCell].count);
                        //
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled = true;
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;

                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite = ItemOnCursor.sprite;
                        Debug.Log("Количество забранного предмета " + tempItemCount);
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = ItemOnCursor.selecteditem.count.ToString();

                        ItemOnCursor.selecteditem = null;
                        ItemOnCursor.sprite = null;
                        ItemOnCursor.firstCell = 0;

                        HelperClass.itemOnCursorGameObject.GetComponent<Image>().enabled = false;
                    }
                    else
                    {

                        //AllItemsAndBlocks tempItem = HelperClass.playerInventory[int.Parse(gameObject.name)];
                        ItemOnCursor.selecteditem = InventoryItemClone(HelperClass.playerInventory[int.Parse(gameObject.name)]);
                        Debug.Log("Вы взяли предмет " + HelperClass.playerInventory[int.Parse(gameObject.name)].name + " В количестве: " + ItemOnCursor.selecteditem.count);
                        //ItemOnCursor.selecteditem = HelperClass.playerInventory[int.Parse(gameObject.name)];
                        HelperClass.playerInventory[int.Parse(gameObject.name)] = null;
                        Debug.Log(ItemOnCursor.selecteditem.name);
                        //
                        ItemOnCursor.sprite = HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite;
                        Debug.Log(ItemOnCursor.sprite);
                        ItemOnCursor.firstCell = int.Parse(gameObject.name);
                        tempSprite = ItemOnCursor.sprite;
                        tempItem = ItemOnCursor.selecteditem;
                        tempItemCount = ItemOnCursor.selecteditem.count;
                        //Debug.Log(tempItemCount);
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled = false;
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                        HelperClass.itemOnCursorGameObject.GetComponent<Image>().sprite = tempSprite;
                        HelperClass.itemOnCursorGameObject.GetComponent<Image>().enabled = true;
                        HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite = null;
                    }
                }
                else if (ItemOnCursor.selecteditem != null)
                {
                    HelperClass.playerInventory[int.Parse(gameObject.name)] = InventoryItemClone(ItemOnCursor.selecteditem);
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled = true;
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = true;

                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().sprite = ItemOnCursor.sprite;
                    HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = ItemOnCursor.selecteditem.count.ToString();

                    ItemOnCursor.selecteditem = null;
                    HelperClass.itemOnCursorGameObject.GetComponent<Image>().enabled = false;
                }
            }
            else
            {

            }
        }
    }

    public void OnMouseEnter()
    {
        HelperClass.barrierPlaceBlock = true;
        if (isCraftCell == true)
        {
            //ingredientPanel.SetActive(true);
            UpdateIngredientText();
        }
    }

    private void UpdateIngredientText()
    {
        while (ingredientPanel.transform.childCount > 0)
        {
            DestroyImmediate(ingredientPanel.transform.GetChild(0).gameObject);
        }

        // Обновляем текст, чтобы отобразить ингредиенты и их количество
        recipeItemName.text = recipe.item.name;
        ingredientText.text = "Необходимые ингредиенты:\n";
        foreach (Ingredient ingredient in recipe.ingredients)
        {
            // Создаём префаб
            GameObject newIngredient = Instantiate(ingredientPrefab, ingredientPanel.transform);
            Image ingredientImage = newIngredient.GetComponentInChildren<LayoutElement>().gameObject.GetComponent<Image>();

            // Загрузка изображения
            float pixelsPerUnit = 16;

            if (!string.IsNullOrEmpty(ingredient.item.imagePath) && File.Exists(ingredient.item.imagePath) && ingredient.item.imagePath != null)
            {
                // �������� �������� �� �����
                byte[] imageData = File.ReadAllBytes(ingredient.item.imagePath);
                Texture2D texture = new Texture2D(16, 16);
                texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
                texture.filterMode = FilterMode.Point;

                // ������������ ������� ������� � ������ pixelsPerUnit
                float width = texture.width / 16;
                float height = texture.height / 16;

                // �������� ������� �� ��������
                Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                //playerInventoryGameObject.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().enabled = true;
                ingredientImage.sprite = newSprite;
            }
            // Количество ингредиента
            TextMeshProUGUI ingredientCount = newIngredient.GetComponentInChildren<TextMeshProUGUI>();
            ingredientCount.text = ingredient.quantity.ToString();

            //ingredientText.text += $"{ingredient.item.name}: {ingredient.quantity}\n";
        }
    }

    public void OnMouseExit()
    {
        HelperClass.barrierPlaceBlock = false;
        if (isCraftCell == true)
        {
            //ingredientPanel.SetActive(false);
        }
    }

    public AllItemsAndBlocks InventoryItemClone(AllItemsAndBlocks itemInCell)
    {
        AllItemsAndBlocks itemOnCursor = new AllItemsAndBlocks(itemInCell.blockIndex, itemInCell.name,
            itemInCell.blocksSolidity, itemInCell.stackable, itemInCell.maxStack, itemInCell.imagePath);

        itemOnCursor.count = itemInCell.count;
        //itemOnCursor.description = itemInCell.description;

        return itemOnCursor;
    }
}
