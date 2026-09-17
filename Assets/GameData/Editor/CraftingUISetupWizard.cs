
#if UNITY_EDITOR

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

using Game.Crafting;
using Game.Crafting.UI;
using Game.Inventory;
using Game.Inventory.UI;


namespace Game.EditorTools
{

    public static class CraftingUISetupWizard
    {

        [MenuItem(
            "Tools/Game/Create Crafting UI"
        )]
        public static void CreateCraftingUI()
        {

            InventoryUI inventoryUI =
                Object.FindFirstObjectByType<
                    InventoryUI
                >();


            PlayerInventory inventory =
                Object.FindFirstObjectByType<
                    PlayerInventory
                >();


            if (
                inventoryUI ==
                null
                ||
                inventory ==
                null
            )
            {

                EditorUtility.DisplayDialog(
                    "Crafting UI",
                    "InventoryUI или PlayerInventory не найден. Сначала создай систему инвентаря.",
                    "OK"
                );


                return;

            }


            GameObject root =
                inventoryUI.gameObject;


            Transform old =
                root.transform.Find(
                    "CraftPanel"
                );


            if (
                old !=
                null
            )
            {

                if (
                    EditorUtility.DisplayDialog(
                        "Crafting UI",
                        "CraftPanel уже существует. Пересоздать?",
                        "Пересоздать",
                        "Отмена"
                    )
                )
                {

                    Object.DestroyImmediate(
                        old.gameObject
                    );

                }
                else
                {

                    return;

                }

            }


            CraftingUIController oldController =
                root.GetComponent<
                    CraftingUIController
                >();


            if (
                oldController !=
                null
            )
            {

                Object.DestroyImmediate(
                    oldController
                );

            }


            CraftingUIController controller =
                root.AddComponent<
                    CraftingUIController
                >();


            WorkbenchInteraction workbenchInteraction =
                inventory.gameObject
                    .GetComponent<
                        WorkbenchInteraction
                    >();


            if (
                workbenchInteraction ==
                null
            )
            {

                inventory.gameObject
                    .AddComponent<
                        WorkbenchInteraction
                    >();

            }


            // =================================================
            // PANEL
            // =================================================

            GameObject panel =
                CreateRect(
                    "CraftPanel",
                    root.transform
                );


            RectTransform panelRect =
                panel.GetComponent<
                    RectTransform
                >();


            panelRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            panelRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            panelRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            // Left of the regular 660px inventory.
            panelRect.anchoredPosition =
                new Vector2(
                    -525f,
                    0f
                );


            panelRect.sizeDelta =
                new Vector2(
                    360f,
                    300f
                );


            Image background =
                panel.AddComponent<
                    Image
                >();


            background.color =
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.96f
                );


            // =================================================
            // TITLE
            // =================================================

            GameObject titleObject =
                CreateRect(
                    "Title",
                    panel.transform
                );


            RectTransform titleRect =
                titleObject.GetComponent<
                    RectTransform
                >();


            titleRect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );


            titleRect.anchorMax =
                new Vector2(
                    1f,
                    1f
                );


            titleRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );


            titleRect.offsetMin =
                new Vector2(
                    10f,
                    -40f
                );


            titleRect.offsetMax =
                new Vector2(
                    -10f,
                    -6f
                );


            TMP_Text title =
                titleObject.AddComponent<
                    TextMeshProUGUI
                >();


            title.text =
                "КРАФТ";


            title.fontSize =
                22f;


            title.alignment =
                TextAlignmentOptions.Center;


            title.raycastTarget =
                false;


            // =================================================
            // SCROLL VIEW
            // =================================================

            GameObject scrollObject =
                CreateRect(
                    "RecipeScroll",
                    panel.transform
                );


            RectTransform scrollRectTransform =
                scrollObject.GetComponent<
                    RectTransform
                >();


            scrollRectTransform.anchorMin =
                Vector2.zero;


            scrollRectTransform.anchorMax =
                Vector2.one;


            scrollRectTransform.offsetMin =
                new Vector2(
                    10f,
                    10f
                );


            scrollRectTransform.offsetMax =
                new Vector2(
                    -10f,
                    -48f
                );


            Image scrollBackground =
                scrollObject.AddComponent<
                    Image
                >();


            scrollBackground.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.12f
                );


            ScrollRect scroll =
                scrollObject.AddComponent<
                    ScrollRect
                >();


            GameObject viewport =
                CreateRect(
                    "Viewport",
                    scrollObject.transform
                );


            RectTransform viewportRect =
                viewport.GetComponent<
                    RectTransform
                >();


            Stretch(
                viewportRect
            );


            viewport.AddComponent<
                RectMask2D
            >();


            GameObject content =
                CreateRect(
                    "Content",
                    viewport.transform
                );


            RectTransform contentRect =
                content.GetComponent<
                    RectTransform
                >();


            contentRect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );


            contentRect.anchorMax =
                new Vector2(
                    1f,
                    1f
                );


            contentRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );


            contentRect.anchoredPosition =
                Vector2.zero;


            contentRect.sizeDelta =
                new Vector2(
                    0f,
                    0f
                );


            GridLayoutGroup contentLayout =
                content.AddComponent<
                    GridLayoutGroup
                >();


            contentLayout.cellSize =
                new Vector2(
                    116f,
                    132f
                );


            contentLayout.spacing =
                new Vector2(
                    7f,
                    7f
                );


            contentLayout.padding =
                new RectOffset(
                    3,
                    3,
                    3,
                    3
                );


            contentLayout.constraint =
                GridLayoutGroup.Constraint
                    .FixedColumnCount;


            contentLayout.constraintCount =
                3;


            contentLayout.startCorner =
                GridLayoutGroup.Corner
                    .UpperLeft;


            contentLayout.startAxis =
                GridLayoutGroup.Axis
                    .Horizontal;


            contentLayout.childAlignment =
                TextAnchor.UpperLeft;


            ContentSizeFitter fitter =
                content.AddComponent<
                    ContentSizeFitter
                >();


            fitter.verticalFit =
                ContentSizeFitter.FitMode
                    .PreferredSize;


            scroll.viewport =
                viewportRect;


            scroll.content =
                contentRect;


            scroll.horizontal =
                false;


            scroll.vertical =
                true;


            // =================================================
            // WIRE
            // =================================================

            SerializedObject serialized =
                new SerializedObject(
                    controller
                );


            serialized.FindProperty(
                "inventoryUI"
            ).objectReferenceValue =
                inventoryUI;


            serialized.FindProperty(
                "playerInventory"
            ).objectReferenceValue =
                inventory;


            serialized.FindProperty(
                "craftPanel"
            ).objectReferenceValue =
                panel;


            serialized.FindProperty(
                "titleText"
            ).objectReferenceValue =
                title;


            serialized.FindProperty(
                "contentRoot"
            ).objectReferenceValue =
                contentRect;


            serialized
                .ApplyModifiedPropertiesWithoutUndo();


            panel.SetActive(
                false
            );


            Transform cursor =
                root.transform.Find(
                    "CursorItem"
                );


            if (
                cursor !=
                null
            )
            {

                cursor.SetAsLastSibling();

            }


            EditorUtility.SetDirty(
                root
            );


            EditorUtility.SetDirty(
                inventory.gameObject
            );


            Selection.activeGameObject =
                root;


            EditorUtility.DisplayDialog(
                "Crafting UI",
                "Готово.\n\nСлева от инвентаря создан CraftPanel.\nНа Player добавлен WorkbenchInteraction.\n\nОбычный E показывает рецепты CraftWithoutWorkbench=true.\nПКМ по блоку с тегом workbench открывает расширенный крафт.",
                "OK"
            );

        }


        private static GameObject CreateRect(
            string name,
            Transform parent
        )
        {

            GameObject gameObject =
                new GameObject(
                    name,
                    typeof(
                        RectTransform
                    )
                );


            gameObject.transform.SetParent(
                parent,
                false
            );


            return gameObject;

        }


        private static void Stretch(
            RectTransform rect
        )
        {

            rect.anchorMin =
                Vector2.zero;


            rect.anchorMax =
                Vector2.one;


            rect.offsetMin =
                Vector2.zero;


            rect.offsetMax =
                Vector2.zero;

        }

    }

}

#endif
