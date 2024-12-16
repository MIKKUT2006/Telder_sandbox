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

    // ������� ������
    [SerializeField] bool isCraftCell = false;
    public CraftingRecipe recipe; // ������, ��������� � ������ �������
    //public GameObject ingredientPanel; // ������ ��� ����������� ������������
    public TextMeshProUGUI ingredientText; // �����, ������������ �����������
    public TextMeshProUGUI recipeItemName; // ������, ��������� � ������ �������


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
        // �������� �� ������� ������ ������ ����
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // ��������, ��� �������� ��� ���� �������
            if (ItemOnCursor.selecteditem != null)
            {
                // ��������, ��� ��� ���� ������� � ���� �����
                if (HelperClass.playerInventoryGameObject.transform.Find(gameObject.name).transform.Find("Image").GetComponent<Image>().enabled == true)
                {
                    // ��������, ��� ������� � ����� ��������� � ��������� � �������
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
                    Debug.Log($"���������� � ������ {HelperClass.playerInventory[int.Parse(gameObject.name)].count}");
                    ItemOnCursor.selecteditem.count--;
                    Debug.Log($"� ������� �������� {ItemOnCursor.selecteditem.count} ��������");
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
                // ����� �������� � ���������
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
                        Debug.Log("���������� ���������� �������� " + tempItemCount);
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
                        Debug.Log("�� ����� ������� " + HelperClass.playerInventory[int.Parse(gameObject.name)].name + " � ����������: " + ItemOnCursor.selecteditem.count);
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
        // ��������� �����, ����� ���������� ����������� � �� ����������
        recipeItemName.text = recipe.item.name;
        ingredientText.text = "����������� �����������:\n";
        foreach (Ingredient ingredient in recipe.ingredients)
        {
            ingredientText.text += $"{ingredient.item.name}: {ingredient.quantity}\n";
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
