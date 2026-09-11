
using System.Collections.Generic;
using System.Text;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Game.Blocks;
using Game.Crafting;
using Game.Inventory;
using Game.Inventory.UI;


namespace Game.Crafting.UI
{

    public class CraftingUIController :
        MonoBehaviour
    {

        public static CraftingUIController Instance
        {
            get;
            private set;
        }


        [Header("References")]

        [SerializeField]
        private InventoryUI inventoryUI;


        [SerializeField]
        private PlayerInventory playerInventory;


        [Header("Craft Panel")]

        [SerializeField]
        private GameObject craftPanel;


        [SerializeField]
        private TMP_Text titleText;


        [SerializeField]
        private RectTransform contentRoot;


        [Header("Appearance")]

        [SerializeField]
        private Sprite rowBackground;


        [SerializeField]
        private TMP_FontAsset pixelFont;


        private readonly List<GameObject>
            generatedRows =
            new List<GameObject>();


        private bool lastInventoryOpen;


        private bool lastWorkbenchState;


        private bool suppressInventoryRefresh;


        private void Awake()
        {

            Instance =
                this;


            if (
                inventoryUI ==
                null
            )
            {

                inventoryUI =
                    GetComponent<
                        InventoryUI
                    >();

            }


            if (
                playerInventory ==
                null
                &&
                inventoryUI !=
                null
            )
            {

                playerInventory =
                    inventoryUI.PlayerInventory;

            }


            if (
                craftPanel !=
                null
            )
            {

                craftPanel.SetActive(
                    false
                );

            }

        }


        private void Start()
        {

            if (
                playerInventory !=
                null
            )
            {

                playerInventory.Changed +=
                    HandleInventoryChanged;

            }


            Refresh();

        }


        private void OnDestroy()
        {

            if (
                playerInventory !=
                null
            )
            {

                playerInventory.Changed -=
                    HandleInventoryChanged;

            }


            if (
                Instance ==
                this
            )
            {

                Instance =
                    null;

            }

        }


        private void Update()
        {

            bool inventoryOpen =
                inventoryUI !=
                null
                &&
                inventoryUI.IsOpen;


            if (
                !inventoryOpen
                &&
                CraftingSession.HasWorkbench
            )
            {

                CraftingSession.Clear();

            }


            bool workbench =
                CraftingSession.HasWorkbench;


            if (
                inventoryOpen !=
                lastInventoryOpen
                ||
                workbench !=
                lastWorkbenchState
            )
            {

                lastInventoryOpen =
                    inventoryOpen;


                lastWorkbenchState =
                    workbench;


                if (
                    craftPanel !=
                    null
                )
                {

                    craftPanel.SetActive(
                        inventoryOpen
                    );

                }


                Refresh();

            }

        }


        public void OpenWorkbench()
        {

            CraftingSession.SetWorkbench(
                true
            );


            if (
                inventoryUI !=
                null
            )
            {

                inventoryUI.OpenInventory();

            }


            Refresh();

        }


        private void HandleInventoryChanged()
        {

            if (
                suppressInventoryRefresh
            )
            {

                return;

            }


            if (
                inventoryUI !=
                null
                &&
                inventoryUI.IsOpen
            )
            {

                Refresh();

            }

        }


        public void Refresh()
        {

            if (
                contentRoot ==
                null
                ||
                playerInventory ==
                null
            )
            {

                return;

            }


            ClearRows();


            bool workbench =
                CraftingSession.HasWorkbench;


            if (
                titleText !=
                null
            )
            {

                titleText.text =
                    workbench
                        ? "КРАФТ — ВЕРСТАК"
                        : "КРАФТ";

            }


            List<BlockDefinition> recipes =
                CraftingService
                    .GetVisibleRecipes(
                        workbench
                    );


            for (
                int i = 0;
                i < recipes.Count;
                i++
            )
            {

                CreateRecipeRow(
                    recipes[i]
                );

            }

        }


        private void CreateRecipeRow(
            BlockDefinition recipe
        )
        {

            if (
                recipe ==
                null
            )
            {

                return;

            }


            GameObject row =
                new GameObject(
                    "Craft_" +
                    recipe.ID,
                    typeof(
                        RectTransform
                    )
                );


            row.transform.SetParent(
                contentRoot,
                false
            );


            generatedRows.Add(
                row
            );


            LayoutElement layoutElement =
                row.AddComponent<
                    LayoutElement
                >();


            layoutElement.preferredHeight =
                72f;


            Image background =
                row.AddComponent<
                    Image
                >();


            background.color =
                new Color(
                    0.07f,
                    0.07f,
                    0.09f,
                    0.96f
                );


            if (
                rowBackground !=
                null
            )
            {

                background.sprite =
                    rowBackground;

            }


            HorizontalLayoutGroup layout =
                row.AddComponent<
                    HorizontalLayoutGroup
                >();


            layout.padding =
                new RectOffset(
                    8,
                    8,
                    6,
                    6
                );


            layout.spacing =
                8f;


            layout.childAlignment =
                TextAnchor.MiddleLeft;


            layout.childControlWidth =
                false;


            layout.childControlHeight =
                false;


            layout.childForceExpandWidth =
                false;


            layout.childForceExpandHeight =
                false;


            // ICON
            GameObject iconObject =
                new GameObject(
                    "Icon",
                    typeof(
                        RectTransform
                    )
                );


            iconObject.transform.SetParent(
                row.transform,
                false
            );


            RectTransform iconRect =
                iconObject.GetComponent<
                    RectTransform
                >();


            iconRect.sizeDelta =
                new Vector2(
                    50f,
                    50f
                );


            Image icon =
                iconObject.AddComponent<
                    Image
                >();


            icon.preserveAspect =
                true;


            icon.raycastTarget =
                false;


            icon.sprite =
                ItemIconProvider.GetIcon(
                    recipe.ID
                );


            icon.enabled =
                icon.sprite !=
                null;


            // TEXT
            GameObject textObject =
                new GameObject(
                    "RecipeText",
                    typeof(
                        RectTransform
                    )
                );


            textObject.transform.SetParent(
                row.transform,
                false
            );


            RectTransform textRect =
                textObject.GetComponent<
                    RectTransform
                >();


            textRect.sizeDelta =
                new Vector2(
                    205f,
                    56f
                );


            TMP_Text text =
                textObject.AddComponent<
                    TextMeshProUGUI
                >();


            if (
                pixelFont !=
                null
            )
            {

                text.font =
                    pixelFont;

            }


            text.fontSize =
                14f;


            text.alignment =
                TextAlignmentOptions.MidlineLeft;


            text.raycastTarget =
                false;


            text.text =
                BuildRecipeText(
                    recipe
                );


            // BUTTON
            GameObject buttonObject =
                new GameObject(
                    "CraftButton",
                    typeof(
                        RectTransform
                    )
                );


            buttonObject.transform.SetParent(
                row.transform,
                false
            );


            RectTransform buttonRect =
                buttonObject.GetComponent<
                    RectTransform
                >();


            buttonRect.sizeDelta =
                new Vector2(
                    92f,
                    44f
                );


            Image buttonImage =
                buttonObject.AddComponent<
                    Image
                >();


            buttonImage.color =
                new Color(
                    0.12f,
                    0.12f,
                    0.16f,
                    1f
                );


            Button button =
                buttonObject.AddComponent<
                    Button
                >();


            button.targetGraphic =
                buttonImage;


            bool canCraft =
                CraftingService.CanCraft(
                    playerInventory,
                    recipe
                );


            button.interactable =
                canCraft;


            GameObject labelObject =
                new GameObject(
                    "Text",
                    typeof(
                        RectTransform
                    )
                );


            labelObject.transform.SetParent(
                buttonObject.transform,
                false
            );


            RectTransform labelRect =
                labelObject.GetComponent<
                    RectTransform
                >();


            labelRect.anchorMin =
                Vector2.zero;


            labelRect.anchorMax =
                Vector2.one;


            labelRect.offsetMin =
                Vector2.zero;


            labelRect.offsetMax =
                Vector2.zero;


            TMP_Text label =
                labelObject.AddComponent<
                    TextMeshProUGUI
                >();


            if (
                pixelFont !=
                null
            )
            {

                label.font =
                    pixelFont;

            }


            label.text =
                "СОЗДАТЬ";


            label.fontSize =
                13f;


            label.alignment =
                TextAlignmentOptions.Center;


            label.raycastTarget =
                false;


            BlockDefinition captured =
                recipe;


            button.onClick.AddListener(
                () =>
                {
                    suppressInventoryRefresh =
                        true;


                    try
                    {

                        CraftingService.Craft(
                            playerInventory,
                            captured
                        );

                    }
                    finally
                    {

                        suppressInventoryRefresh =
                            false;

                    }


                    Refresh();
                }
            );

        }


        private string BuildRecipeText(
            BlockDefinition recipe
        )
        {

            StringBuilder builder =
                new StringBuilder();


            builder.Append(
                string.IsNullOrWhiteSpace(
                    recipe.Name
                )
                    ? recipe.ID
                    : recipe.Name
            );


            int resultCount =
                recipe.CraftResultCount >
                0
                    ? recipe.CraftResultCount
                    : 1;


            if (
                resultCount >
                1
            )
            {

                builder.Append(
                    " x"
                );


                builder.Append(
                    resultCount
                );

            }


            builder.AppendLine();


            for (
                int i = 0;
                i < recipe.CraftIngredients.Count;
                i++
            )
            {

                CraftIngredientDefinition ingredient =
                    recipe.CraftIngredients[i];


                if (
                    ingredient ==
                    null
                    ||
                    string.IsNullOrWhiteSpace(
                        ingredient.ItemId
                    )
                )
                {

                    continue;

                }


                int need =
                    ingredient.Count >
                    0
                        ? ingredient.Count
                        : 1;


                int have =
                    playerInventory.CountItem(
                        ingredient.ItemId
                    );


                builder.Append(
                    have >=
                    need
                        ? "✓ "
                        : "✕ "
                );


                builder.Append(
                    ingredient.ItemId
                );


                builder.Append(
                    " "
                );


                builder.Append(
                    have
                );


                builder.Append(
                    "/"
                );


                builder.Append(
                    need
                );


                if (
                    i <
                    recipe.CraftIngredients.Count -
                    1
                )
                {

                    builder.Append(
                        "   "
                    );

                }

            }


            return
                builder.ToString();

        }


        private void ClearRows()
        {

            for (
                int i = 0;
                i < generatedRows.Count;
                i++
            )
            {

                if (
                    generatedRows[i] !=
                    null
                )
                {

                    Destroy(
                        generatedRows[i]
                    );

                }

            }


            generatedRows.Clear();

        }

    }

}
