#if UNITY_EDITOR

using System.Collections.Generic;

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

using Game.Chests;
using Game.Chests.UI;
using Game.Inventory;
using Game.Inventory.UI;


namespace Game.EditorTools
{

    public static class ChestSystemSetupWizard
    {

        [MenuItem(
            "Tools/Game/Create Chest System"
        )]
        public static void CreateChestSystem()
        {

            PlayerInventory playerInventory =
                Object.FindFirstObjectByType<
                    PlayerInventory
                >();


            InventoryUI inventoryUI =
                Object.FindFirstObjectByType<
                    InventoryUI
                >();


            if (
                playerInventory == null
            )
            {

                EditorUtility.DisplayDialog(
                    "Chest System",
                    "PlayerInventory не найден.",
                    "OK"
                );


                return;

            }


            if (
                inventoryUI == null
            )
            {

                EditorUtility.DisplayDialog(
                    "Chest System",
                    "InventoryUI не найден. Сначала создай Inventory UI.",
                    "OK"
                );


                return;

            }


            // =================================================
            // PLAYER COMPONENTS
            // =================================================

            GameObject player =
                playerInventory.gameObject;


            ChestManager manager =
                player.GetComponent<
                    ChestManager
                >();


            if (
                manager == null
            )
            {

                manager =
                    player.AddComponent<
                        ChestManager
                    >();

            }


            ChestInteraction interaction =
                player.GetComponent<
                    ChestInteraction
                >();


            if (
                interaction == null
            )
            {

                interaction =
                    player.AddComponent<
                        ChestInteraction
                    >();

            }


            // =================================================
            // UI ROOT
            // =================================================

            GameObject root =
                inventoryUI.gameObject;


            Transform oldPanel =
                root.transform.Find(
                    "ChestPanel"
                );


            if (
                oldPanel != null
            )
            {

                bool replace =
                    EditorUtility.DisplayDialog(
                        "Chest System",
                        "ChestPanel уже существует.\n\nПересоздать его?",
                        "Пересоздать",
                        "Оставить"
                    );


                if (
                    replace
                )
                {

                    Object.DestroyImmediate(
                        oldPanel.gameObject
                    );

                }
                else
                {

                    Selection.activeGameObject =
                        root;


                    return;

                }

            }


            ChestUIController oldController =
                root.GetComponent<
                    ChestUIController
                >();


            if (
                oldController != null
            )
            {

                Object.DestroyImmediate(
                    oldController
                );

            }


            ChestUIController controller =
                root.AddComponent<
                    ChestUIController
                >();


            // =================================================
            // PANEL
            // =================================================

            GameObject panel =
                CreateRectObject(
                    "ChestPanel",
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


            // InventoryPanel from v9/v11 is 660x300.
            // This panel sits directly above it.
            panelRect.anchoredPosition =
                new Vector2(
                    0f,
                    280f
                );


            panelRect.sizeDelta =
                new Vector2(
                    660f,
                    230f
                );


            Image panelBackground =
                panel.AddComponent<
                    Image
                >();


            panelBackground.color =
                new Color(
                    0.025f,
                    0.025f,
                    0.035f,
                    0.94f
                );


            // =================================================
            // TITLE
            // =================================================

            GameObject titleObject =
                CreateRectObject(
                    "Title",
                    panel.transform
                );


            RectTransform titleRect =
                titleObject.GetComponent<
                    RectTransform
                >();


            titleRect.anchorMin =
                new Vector2(
                    0.5f,
                    1f
                );


            titleRect.anchorMax =
                new Vector2(
                    0.5f,
                    1f
                );


            titleRect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );


            titleRect.anchoredPosition =
                new Vector2(
                    0f,
                    -10f
                );


            titleRect.sizeDelta =
                new Vector2(
                    500f,
                    34f
                );


            TMP_Text title =
                titleObject.AddComponent<
                    TextMeshProUGUI
                >();


            title.text =
                "СУНДУК";


            title.fontSize =
                22f;


            title.alignment =
                TextAlignmentOptions.Center;


            title.raycastTarget =
                false;


            // =================================================
            // GRID
            // =================================================

            GameObject gridObject =
                CreateRectObject(
                    "ChestGrid",
                    panel.transform
                );


            RectTransform gridRect =
                gridObject.GetComponent<
                    RectTransform
                >();


            gridRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            gridRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            gridRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            gridRect.anchoredPosition =
                new Vector2(
                    0f,
                    -12f
                );


            gridRect.sizeDelta =
                new Vector2(
                    9f * 54f +
                    8f * 4f,

                    3f * 54f +
                    2f * 4f
                );


            GridLayoutGroup grid =
                gridObject.AddComponent<
                    GridLayoutGroup
                >();


            grid.cellSize =
                new Vector2(
                    54f,
                    54f
                );


            grid.spacing =
                new Vector2(
                    4f,
                    4f
                );


            grid.constraint =
                GridLayoutGroup.Constraint
                    .FixedColumnCount;


            grid.constraintCount =
                9;


            grid.childAlignment =
                TextAnchor.MiddleCenter;


            List<ChestSlotUI> slots =
                new List<
                    ChestSlotUI
                >();


            for (
                int i = 0;
                i < ChestInventoryRuntime.SlotCount;
                i++
            )
            {

                slots.Add(
                    CreateSlot(
                        "ChestSlot_" +
                        i,
                        gridObject.transform
                    )
                );

            }


            // =================================================
            // WIRE CONTROLLER
            // =================================================

            SerializedObject serialized =
                new SerializedObject(
                    controller
                );


            serialized
                .FindProperty(
                    "inventoryUI"
                )
                .objectReferenceValue =
                inventoryUI;


            serialized
                .FindProperty(
                    "playerInventory"
                )
                .objectReferenceValue =
                playerInventory;


            serialized
                .FindProperty(
                    "chestManager"
                )
                .objectReferenceValue =
                manager;


            serialized
                .FindProperty(
                    "chestPanel"
                )
                .objectReferenceValue =
                panel;


            serialized
                .FindProperty(
                    "chestTitle"
                )
                .objectReferenceValue =
                title;


            SerializedProperty slotsProperty =
                serialized.FindProperty(
                    "chestSlots"
                );


            slotsProperty.arraySize =
                slots.Count;


            for (
                int i = 0;
                i < slots.Count;
                i++
            )
            {

                slotsProperty
                    .GetArrayElementAtIndex(
                        i
                    )
                    .objectReferenceValue =
                    slots[i];

            }


            serialized
                .ApplyModifiedPropertiesWithoutUndo();


            panel.SetActive(
                false
            );


            // Keep cursor item above chest panel.
            Transform cursor =
                root.transform.Find(
                    "CursorItem"
                );


            if (
                cursor != null
            )
            {

                cursor.SetAsLastSibling();

            }


            EditorUtility.SetDirty(
                root
            );


            EditorUtility.SetDirty(
                player
            );


            Selection.activeGameObject =
                root;


            EditorUtility.DisplayDialog(
                "Chest System",
                "Готово.\n\nДобавлено:\n• ChestManager на Player\n• ChestInteraction на Player\n• ChestPanel 9x3 над инвентарём\n• ChestUIController\n• 27 слотов сундука\n\nПо умолчанию Chest Block ID = game:chest.\n\nPlay Mode -> Tools/Game/Chest Debug -> Выдать сундук.",
                "OK"
            );

        }


        private static ChestSlotUI CreateSlot(
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
                    0.10f,
                    0.10f,
                    0.13f,
                    0.96f
                );


            // Icon
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
                Vector2.zero;


            iconRect.anchorMax =
                Vector2.one;


            iconRect.offsetMin =
                new Vector2(
                    5f,
                    5f
                );


            iconRect.offsetMax =
                new Vector2(
                    -5f,
                    -5f
                );


            Image icon =
                iconObject.AddComponent<
                    Image
                >();


            icon.preserveAspect =
                true;


            icon.raycastTarget =
                false;


            icon.enabled =
                false;


            // Count
            GameObject countObject =
                CreateRectObject(
                    "Count",
                    slotObject.transform
                );


            RectTransform countRect =
                countObject.GetComponent<
                    RectTransform
                >();


            countRect.anchorMin =
                Vector2.zero;


            countRect.anchorMax =
                Vector2.one;


            countRect.offsetMin =
                new Vector2(
                    2f,
                    1f
                );


            countRect.offsetMax =
                new Vector2(
                    -4f,
                    -2f
                );


            TMP_Text count =
                countObject.AddComponent<
                    TextMeshProUGUI
                >();


            count.fontSize =
                16f;


            count.alignment =
                TextAlignmentOptions.BottomRight;


            count.raycastTarget =
                false;


            ChestSlotUI slot =
                slotObject.AddComponent<
                    ChestSlotUI
                >();


            SerializedObject serialized =
                new SerializedObject(
                    slot
                );


            serialized
                .FindProperty(
                    "backgroundImage"
                )
                .objectReferenceValue =
                background;


            serialized
                .FindProperty(
                    "iconImage"
                )
                .objectReferenceValue =
                icon;


            serialized
                .FindProperty(
                    "countText"
                )
                .objectReferenceValue =
                count;


            serialized
                .ApplyModifiedPropertiesWithoutUndo();


            return slot;

        }


        private static GameObject CreateRectObject(
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

    }

}

#endif
