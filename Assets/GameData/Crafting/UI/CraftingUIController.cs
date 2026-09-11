using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Blocks;
using Game.Inventory;
using Game.Inventory.UI;

namespace Game.Crafting.UI
{
    public class CraftingUIController : MonoBehaviour
    {
        public static CraftingUIController Instance { get; private set; }

        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private GameObject craftPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TMP_FontAsset pixelFont;
        [SerializeField] private int columns = 3;
        [SerializeField] private Vector2 cardSize = new Vector2(116f, 132f);
        [SerializeField] private Vector2 spacing = new Vector2(7f, 7f);

        private readonly List<GameObject> generated = new List<GameObject>();
        private bool lastOpen;
        private bool lastWorkbench;
        private bool suppressRefresh;

        private void Awake()
        {
            Instance = this;
            if (inventoryUI == null) inventoryUI = GetComponent<InventoryUI>();
            if (playerInventory == null && inventoryUI != null) playerInventory = inventoryUI.PlayerInventory;
            ConfigureGrid();
            if (craftPanel != null) craftPanel.SetActive(false);
        }

        private void Start()
        {
            if (playerInventory != null) playerInventory.Changed += OnInventoryChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (playerInventory != null) playerInventory.Changed -= OnInventoryChanged;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool open = inventoryUI != null && inventoryUI.IsOpen;
            if (!open && CraftingSession.HasWorkbench) CraftingSession.Clear();
            bool workbench = CraftingSession.HasWorkbench;
            if (open != lastOpen || workbench != lastWorkbench)
            {
                lastOpen = open; lastWorkbench = workbench;
                if (craftPanel != null) craftPanel.SetActive(open);
                Refresh();
            }
        }

        public void OpenWorkbench()
        {
            CraftingSession.SetWorkbench(true);
            inventoryUI?.OpenInventory();
            Refresh();
        }

        private void OnInventoryChanged()
        {
            if (!suppressRefresh && inventoryUI != null && inventoryUI.IsOpen) Refresh();
        }

        private void ConfigureGrid()
        {
            if (contentRoot == null)
                return;

            // LayoutGroup has DisallowMultipleComponent in Unity.
            // Destroy(...) is deferred until end of frame, therefore the old
            // VerticalLayoutGroup was still present when GridLayoutGroup was
            // added and AddComponent could return null.
            //
            // This is initialization/migration code, so remove incompatible
            // layout components immediately before adding the grid.
            LayoutGroup[] layouts =
                contentRoot.GetComponents<LayoutGroup>();

            for (int i = 0; i < layouts.Length; i++)
            {
                LayoutGroup layout = layouts[i];

                if (layout == null ||
                    layout is GridLayoutGroup)
                {
                    continue;
                }

                DestroyImmediate(layout);
            }

            GridLayoutGroup grid =
                contentRoot.GetComponent<GridLayoutGroup>();

            if (grid == null)
            {
                grid =
                    contentRoot.gameObject
                        .AddComponent<GridLayoutGroup>();
            }

            if (grid == null)
            {
                Debug.LogError(
                    "CRAFTING UI: Failed to create GridLayoutGroup on Content."
                );

                return;
            }

            grid.cellSize = cardSize;
            grid.spacing = spacing;
            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount =
                Mathf.Max(1, columns);
            grid.startCorner =
                GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis =
                GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment =
                TextAnchor.UpperLeft;

            ContentSizeFitter fitter =
                contentRoot.GetComponent<ContentSizeFitter>();

            if (fitter == null)
            {
                fitter =
                    contentRoot.gameObject
                        .AddComponent<ContentSizeFitter>();
            }

            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            fitter.horizontalFit =
                ContentSizeFitter.FitMode.Unconstrained;
        }

        public void Refresh()
        {
            if (contentRoot == null || playerInventory == null) return;
            ConfigureGrid();
            for (int i=0;i<generated.Count;i++) if (generated[i] != null) Destroy(generated[i]);
            generated.Clear();
            bool workbench = CraftingSession.HasWorkbench;
            if (titleText != null) titleText.text = workbench ? "КРАФТ — ВЕРСТАК" : "КРАФТ";
            List<BlockDefinition> recipes = CraftingService.GetVisibleRecipes(workbench);
            for (int i=0;i<recipes.Count;i++) CreateCard(recipes[i]);
        }

        private void CreateCard(BlockDefinition recipe)
        {
            GameObject card = new GameObject("Recipe_"+recipe.ID, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(contentRoot,false); generated.Add(card);
            card.GetComponent<Image>().color = new Color(.07f,.07f,.095f,.98f);
            Button button = card.GetComponent<Button>();
            button.interactable = CraftingService.CanCraft(playerInventory,recipe);
            RectTransform cr=card.GetComponent<RectTransform>(); cr.sizeDelta=cardSize;

            Image resultIcon = CreateImage(card.transform,"ResultIcon",new Vector2(0,28),new Vector2(54,54));
            resultIcon.sprite = ItemIconProvider.GetIcon(recipe.ID); resultIcon.preserveAspect=true; resultIcon.enabled=resultIcon.sprite!=null;
            TMP_Text name = CreateText(card.transform,"Name",new Vector2(0,-8),new Vector2(cardSize.x-8,25),15,recipe.Name);
            name.alignment=TextAlignmentOptions.Center;

            GameObject ingredients = new GameObject("Ingredients",typeof(RectTransform),typeof(HorizontalLayoutGroup));
            ingredients.transform.SetParent(card.transform,false);
            RectTransform ir=ingredients.GetComponent<RectTransform>(); ir.anchorMin=ir.anchorMax=new Vector2(.5f,.5f); ir.anchoredPosition=new Vector2(0,-44); ir.sizeDelta=new Vector2(cardSize.x-8,38);
            HorizontalLayoutGroup hl=ingredients.GetComponent<HorizontalLayoutGroup>(); hl.spacing=3; hl.childAlignment=TextAnchor.MiddleCenter; hl.childControlWidth=false; hl.childControlHeight=false; hl.childForceExpandWidth=false; hl.childForceExpandHeight=false;

            if (recipe.CraftIngredients != null)
            {
                for (int i=0;i<recipe.CraftIngredients.Count;i++)
                {
                    CraftIngredientDefinition ing=recipe.CraftIngredients[i]; if (ing==null || string.IsNullOrWhiteSpace(ing.ItemId)) continue;
                    GameObject g=new GameObject("Ingredient",typeof(RectTransform));g.transform.SetParent(ingredients.transform,false);g.GetComponent<RectTransform>().sizeDelta=new Vector2(34,34);
                    Image icon=CreateImage(g.transform,"Icon",Vector2.zero,new Vector2(26,26)); icon.sprite=ItemIconProvider.GetIcon(ing.ItemId);icon.preserveAspect=true;icon.enabled=icon.sprite!=null;icon.raycastTarget=true;
                    CraftIngredientIconUI trigger=icon.gameObject.AddComponent<CraftIngredientIconUI>(); trigger.Bind(ing.ItemId);
                    TMP_Text count=CreateText(g.transform,"Count",new Vector2(10,-10),new Vector2(26,18),12,(ing.Count>0?ing.Count:1).ToString()); count.alignment=TextAlignmentOptions.BottomRight;
                }
            }

            BlockDefinition captured=recipe;
            button.onClick.AddListener(()=>{
                suppressRefresh=true;
                try { CraftingService.Craft(playerInventory,captured); }
                finally { suppressRefresh=false; }
                Refresh();
            });
        }

        private Image CreateImage(Transform parent,string n,Vector2 pos,Vector2 size)
        {
            GameObject go=new GameObject(n,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return go.GetComponent<Image>();
        }
        private TMP_Text CreateText(Transform parent,string n,Vector2 pos,Vector2 size,float fontSize,string value)
        {
            GameObject go=new GameObject(n,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;TMP_Text t=go.GetComponent<TMP_Text>();if(pixelFont!=null)t.font=pixelFont;t.fontSize=fontSize;t.text=value;t.raycastTarget=false;return t;
        }
    }
}
