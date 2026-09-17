#if UNITY_EDITOR

using System.Collections.Generic;

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Game.Inventory;
using Game.Inventory.UI;


namespace Game.EditorTools
{

    public static class InventoryUISetupWizard
    {

        // =====================================================
        // MENU
        // =====================================================

        [MenuItem(
            "Tools/Game/Create Inventory UI"
        )]
        public static void CreateInventoryUI()
        {

            PlayerInventory inventory =
                Object.FindFirstObjectByType<
                    PlayerInventory
                >();


            if (
                inventory == null
            )
            {

                EditorUtility.DisplayDialog(
                    "Inventory UI",
                    "Сначала добавь PlayerInventory на объект Player.",
                    "OK"
                );


                return;

            }


            Canvas canvas =
                Object.FindFirstObjectByType<
                    Canvas
                >();


            if (
                canvas == null
            )
            {

                canvas =
                    CreateCanvas();

            }


            EnsureEventSystem();


            GameObject oldRoot =
                GameObject.Find(
                    "InventoryUIRoot"
                );


            if (
                oldRoot != null
            )
            {

                bool replace =
                    EditorUtility.DisplayDialog(
                        "Inventory UI",
                        "InventoryUIRoot уже существует. Удалить его и создать заново?",
                        "Создать заново",
                        "Отмена"
                    );


                if (
                    !replace
                )
                {

                    return;

                }


                Object.DestroyImmediate(
                    oldRoot
                );

            }


            // =================================================
            // ROOT
            // =================================================

            GameObject root =
                CreateRectObject(
                    "InventoryUIRoot",
                    canvas.transform
                );


            RectTransform rootRect =
                root.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                rootRect
            );


            InventoryUI inventoryUI =
                root.AddComponent<
                    InventoryUI
                >();


            // =================================================
            // HOTBAR
            // =================================================

            GameObject hotbar =
                CreateRectObject(
                    "Hotbar",
                    root.transform
                );


            RectTransform hotbarRect =
                hotbar.GetComponent<
                    RectTransform
                >();


            hotbarRect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );


            hotbarRect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );


            hotbarRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );


            hotbarRect.anchoredPosition =
                new Vector2(
                    0f,
                    24f
                );


            hotbarRect.sizeDelta =
                new Vector2(
                    9f * 54f + 8f * 4f,
                    54f
                );


            HorizontalLayoutGroup hotbarLayout =
                hotbar.AddComponent<
                    HorizontalLayoutGroup
                >();


            hotbarLayout.spacing =
                4f;


            hotbarLayout.childAlignment =
                TextAnchor.MiddleCenter;


            hotbarLayout.childControlWidth =
                false;


            hotbarLayout.childControlHeight =
                false;


            hotbarLayout.childForceExpandWidth =
                false;


            hotbarLayout.childForceExpandHeight =
                false;


            List<InventorySlotUI> hotbarSlots =
                new List<InventorySlotUI>();


            for (
                int i = 0;
                i < PlayerInventory.HotbarSize;
                i++
            )
            {

                InventorySlotUI slot =
                    CreateSlot(
                        "HotbarSlot_" +
                        i,
                        hotbar.transform
                    );


                hotbarSlots.Add(
                    slot
                );

            }


            // =================================================
            // INVENTORY PANEL
            // =================================================

            GameObject inventoryPanel =
                CreateRectObject(
                    "InventoryPanel",
                    root.transform
                );


            RectTransform inventoryPanelRect =
                inventoryPanel.GetComponent<
                    RectTransform
                >();


            inventoryPanelRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            inventoryPanelRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            inventoryPanelRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            inventoryPanelRect.sizeDelta =
                new Vector2(
                    660f,
                    300f
                );


            Image panelBackground =
                inventoryPanel.AddComponent<
                    Image
                >();


            panelBackground.color =
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.92f
                );


            // =================================================
            // STORAGE GRID
            // =================================================

            GameObject storage =
                CreateRectObject(
                    "StorageGrid",
                    inventoryPanel.transform
                );


            RectTransform storageRect =
                storage.GetComponent<
                    RectTransform
                >();


            storageRect.anchorMin =
                new Vector2(
                    0f,
                    0.5f
                );


            storageRect.anchorMax =
                new Vector2(
                    0f,
                    0.5f
                );


            storageRect.pivot =
                new Vector2(
                    0f,
                    0.5f
                );


            storageRect.anchoredPosition =
                new Vector2(
                    24f,
                    0f
                );


            storageRect.sizeDelta =
                new Vector2(
                    9f * 54f + 8f * 4f,
                    4f * 54f + 3f * 4f
                );


            GridLayoutGroup storageLayout =
                storage.AddComponent<
                    GridLayoutGroup
                >();


            storageLayout.cellSize =
                new Vector2(
                    54f,
                    54f
                );


            storageLayout.spacing =
                new Vector2(
                    4f,
                    4f
                );


            storageLayout.constraint =
                GridLayoutGroup.Constraint
                    .FixedColumnCount;


            storageLayout.constraintCount =
                9;


            List<InventorySlotUI> storageSlots =
                new List<InventorySlotUI>();


            for (
                int i = 0;
                i < PlayerInventory.StorageSize;
                i++
            )
            {

                InventorySlotUI slot =
                    CreateSlot(
                        "StorageSlot_" +
                        i,
                        storage.transform
                    );


                storageSlots.Add(
                    slot
                );

            }


            // =================================================
            // ARMOR
            // =================================================

            GameObject armor =
                CreateRectObject(
                    "ArmorPanel",
                    inventoryPanel.transform
                );


            RectTransform armorRect =
                armor.GetComponent<
                    RectTransform
                >();


            armorRect.anchorMin =
                new Vector2(
                    1f,
                    0.5f
                );


            armorRect.anchorMax =
                new Vector2(
                    1f,
                    0.5f
                );


            armorRect.pivot =
                new Vector2(
                    1f,
                    0.5f
                );


            armorRect.anchoredPosition =
                new Vector2(
                    -24f,
                    0f
                );


            armorRect.sizeDelta =
                new Vector2(
                    58f,
                    4f * 54f + 3f * 8f
                );


            VerticalLayoutGroup armorLayout =
                armor.AddComponent<
                    VerticalLayoutGroup
                >();


            armorLayout.spacing =
                8f;


            armorLayout.childControlWidth =
                false;


            armorLayout.childControlHeight =
                false;


            armorLayout.childForceExpandWidth =
                false;


            armorLayout.childForceExpandHeight =
                false;


            armorLayout.childAlignment =
                TextAnchor.MiddleCenter;


            for (
                int i = 0;
                i < 4;
                i++
            )
            {

                GameObject armorSlot =
                    CreateRectObject(
                        "ArmorSlot_" +
                        i,
                        armor.transform
                    );


                RectTransform armorSlotRect =
                    armorSlot.GetComponent<
                        RectTransform
                    >();


                armorSlotRect.sizeDelta =
                    new Vector2(
                        54f,
                        54f
                    );


                Image armorImage =
                    armorSlot.AddComponent<
                        Image
                    >();


                armorImage.color =
                    new Color(
                        0.08f,
                        0.08f,
                        0.1f,
                        0.95f
                    );


                Outline outline =
                    armorSlot.AddComponent<
                        Outline
                    >();


                outline.effectColor =
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.28f
                    );


                outline.effectDistance =
                    new Vector2(
                        1f,
                        -1f
                    );

            }


            // =================================================
            // CURSOR ITEM
            // =================================================

            GameObject cursor =
                CreateRectObject(
                    "CursorItem",
                    root.transform
                );


            RectTransform cursorRect =
                cursor.GetComponent<
                    RectTransform
                >();


            cursorRect.sizeDelta =
                new Vector2(
                    44f,
                    44f
                );


            CanvasGroup cursorCanvasGroup =
                cursor.AddComponent<
                    CanvasGroup
                >();


            cursorCanvasGroup.interactable =
                false;


            cursorCanvasGroup.blocksRaycasts =
                false;


            GameObject cursorIconObject =
                CreateRectObject(
                    "Icon",
                    cursor.transform
                );


            RectTransform cursorIconRect =
                cursorIconObject.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                cursorIconRect
            );


            Image cursorIcon =
                cursorIconObject.AddComponent<
                    Image
                >();


            cursorIcon.preserveAspect =
                true;


            cursorIcon.raycastTarget =
                false;


            GameObject cursorCountObject =
                CreateRectObject(
                    "Count",
                    cursor.transform
                );


            RectTransform cursorCountRect =
                cursorCountObject.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                cursorCountRect
            );


            TMP_Text cursorCount =
                cursorCountObject.AddComponent<
                    TextMeshProUGUI
                >();


            cursorCount.fontSize =
                16f;


            cursorCount.alignment =
                TextAlignmentOptions
                    .BottomRight;


            cursorCount.raycastTarget =
                false;


            // =================================================
            // WIRE INVENTORY UI
            // =================================================

            SerializedObject serializedUI =
                new SerializedObject(
                    inventoryUI
                );


            serializedUI.FindProperty(
                "inventory"
            ).objectReferenceValue =
                inventory;


            serializedUI.FindProperty(
                "inventoryPanel"
            ).objectReferenceValue =
                inventoryPanel;


            SetObjectArray(
                serializedUI.FindProperty(
                    "hotbarSlots"
                ),
                hotbarSlots
            );


            SetObjectArray(
                serializedUI.FindProperty(
                    "storageSlots"
                ),
                storageSlots
            );


            serializedUI.FindProperty(
                "cursorItemRoot"
            ).objectReferenceValue =
                cursorRect;


            serializedUI.FindProperty(
                "cursorIcon"
            ).objectReferenceValue =
                cursorIcon;


            serializedUI.FindProperty(
                "cursorCount"
            ).objectReferenceValue =
                cursorCount;


            serializedUI.ApplyModifiedPropertiesWithoutUndo();


            // =================================================
            // FINISH
            // =================================================

            inventoryPanel.SetActive(
                false
            );


            cursor.SetActive(
                false
            );


            Selection.activeGameObject =
                root;


            EditorUtility.SetDirty(
                root
            );


            EditorUtility.DisplayDialog(
                "Inventory UI",
                "Готово.\n\nСозданы:\n• 9 hotbar slots\n• 36 storage slots\n• 4 armor placeholders\n• CursorItem\n• InventoryUI\n\nЗапусти Play Mode и нажми E.",
                "OK"
            );

        }


        // =====================================================
        // CREATE CANVAS
        // =====================================================

        private static Canvas CreateCanvas()
        {

            GameObject canvasObject =
                new GameObject(
                    "GameCanvas"
                );


            Canvas canvas =
                canvasObject.AddComponent<
                    Canvas
                >();


            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;


            canvas.sortingOrder =
                1000;


            CanvasScaler scaler =
                canvasObject.AddComponent<
                    CanvasScaler
                >();


            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;


            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f
                );


            scaler.matchWidthOrHeight =
                0.5f;


            canvasObject.AddComponent<
                GraphicRaycaster
            >();


            return
                canvas;

        }


        // =====================================================
        // EVENT SYSTEM
        // =====================================================

        private static void EnsureEventSystem()
        {

            EventSystem eventSystem =
                Object.FindFirstObjectByType<
                    EventSystem
                >();


            if (
                eventSystem != null
            )
            {

                return;

            }


            GameObject eventObject =
                new GameObject(
                    "EventSystem"
                );


            eventObject.AddComponent<
                EventSystem
            >();


            eventObject.AddComponent<
                StandaloneInputModule
            >();

        }


        // =====================================================
        // CREATE SLOT
        // =====================================================

        private static InventorySlotUI CreateSlot(
            string name,
            Transform parent
        )
        {

            GameObject slotObject =
                CreateRectObject(
                    name,
                    parent
                );


            RectTransform slotRect =
                slotObject.GetComponent<
                    RectTransform
                >();


            slotRect.sizeDelta =
                new Vector2(
                    54f,
                    54f
                );


            Image background =
                slotObject.AddComponent<
                    Image
                >();


            background.color =
                new Color(
                    0.07f,
                    0.07f,
                    0.085f,
                    0.96f
                );


            Outline baseOutline =
                slotObject.AddComponent<
                    Outline
                >();


            baseOutline.effectColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.18f
                );


            baseOutline.effectDistance =
                new Vector2(
                    1f,
                    -1f
                );


            InventorySlotUI slot =
                slotObject.AddComponent<
                    InventorySlotUI
                >();


            // =================================================
            // ICON
            // =================================================

            GameObject iconObject =
                CreateRectObject(
                    "Icon",
                    slotObject.transform
                );


            RectTransform iconRect =
                iconObject.GetComponent<
                    RectTransform
                >();


            iconRect.anchorMin =
                new Vector2(
                    0.12f,
                    0.12f
                );


            iconRect.anchorMax =
                new Vector2(
                    0.88f,
                    0.88f
                );


            iconRect.offsetMin =
                Vector2.zero;


            iconRect.offsetMax =
                Vector2.zero;


            Image icon =
                iconObject.AddComponent<
                    Image
                >();


            icon.preserveAspect =
                true;


            icon.raycastTarget =
                false;


            // =================================================
            // COUNT
            // =================================================

            GameObject countObject =
                CreateRectObject(
                    "Count",
                    slotObject.transform
                );


            RectTransform countRect =
                countObject.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                countRect
            );


            TMP_Text count =
                countObject.AddComponent<
                    TextMeshProUGUI
                >();


            count.text =
                string.Empty;


            count.fontSize =
                15f;


            count.alignment =
                TextAlignmentOptions
                    .BottomRight;


            count.raycastTarget =
                false;


            count.margin =
                new Vector4(
                    2f,
                    2f,
                    4f,
                    2f
                );


            // =================================================
            // SELECTED FRAME
            // =================================================

            GameObject selected =
                CreateRectObject(
                    "SelectedFrame",
                    slotObject.transform
                );


            RectTransform selectedRect =
                selected.GetComponent<
                    RectTransform
                >();


            StretchFullScreen(
                selectedRect
            );


            Image selectedImage =
                selected.AddComponent<
                    Image
                >();


            selectedImage.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0f
                );


            selectedImage.raycastTarget =
                false;


            Outline selectedOutline =
                selected.AddComponent<
                    Outline
                >();


            selectedOutline.effectColor =
                Color.white;


            selectedOutline.effectDistance =
                new Vector2(
                    2f,
                    -2f
                );


            selected.SetActive(
                false
            );


            // =================================================
            // WIRE SLOT
            // =================================================

            SerializedObject serializedSlot =
                new SerializedObject(
                    slot
                );


            serializedSlot.FindProperty(
                "iconImage"
            ).objectReferenceValue =
                icon;


            serializedSlot.FindProperty(
                "countText"
            ).objectReferenceValue =
                count;


            serializedSlot.FindProperty(
                "selectedFrame"
            ).objectReferenceValue =
                selected;


            serializedSlot.ApplyModifiedPropertiesWithoutUndo();


            return
                slot;

        }


        // =====================================================
        // UTILS
        // =====================================================

        private static GameObject CreateRectObject(
            string name,
            Transform parent
        )
        {

            GameObject gameObject =
                new GameObject(
                    name,
                    typeof(RectTransform)
                );


            gameObject.transform.SetParent(
                parent,
                false
            );


            return
                gameObject;

        }


        private static void StretchFullScreen(
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


        private static void SetObjectArray<T>(
            SerializedProperty property,
            List<T> values
        )
            where T :
                Object
        {

            property.arraySize =
                values.Count;


            for (
                int i = 0;
                i < values.Count;
                i++
            )
            {

                property
                    .GetArrayElementAtIndex(
                        i
                    )
                    .objectReferenceValue =
                    values[i];

            }

        }

    }

}

#endif
