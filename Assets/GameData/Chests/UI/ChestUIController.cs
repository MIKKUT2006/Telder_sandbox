using System.Collections.Generic;

using TMPro;

using UnityEngine;

using Game.Inventory;
using Game.Inventory.UI;


namespace Game.Chests.UI
{

    public class ChestUIController :
        MonoBehaviour
    {

        public static ChestUIController Instance
        {
            get;
            private set;
        }


        [Header("References")]

        [SerializeField]
        private InventoryUI inventoryUI;


        [SerializeField]
        private PlayerInventory playerInventory;


        [SerializeField]
        private ChestManager chestManager;


        [Header("Chest UI")]

        [SerializeField]
        private GameObject chestPanel;


        [SerializeField]
        private TMP_Text chestTitle;


        [SerializeField]
        private ChestSlotUI[] chestSlots;


        [Header("Appearance")]

        [SerializeField]
        private Sprite chestSlotSprite;


        private ChestInventoryRuntime currentChest;


        private ChestSlotUI hoveredSlot;


        private bool rightDragActive;


        private readonly HashSet<int>
            rightDragVisited =
            new HashSet<int>();


        public bool IsOpen
        {
            get;
            private set;
        }


        public int CurrentChestX =>
            currentChest != null
                ? currentChest.WorldX
                : 0;


        public int CurrentChestY =>
            currentChest != null
                ? currentChest.WorldY
                : 0;


        private void Awake()
        {

            Instance =
                this;


            if (
                inventoryUI == null
            )
            {

                inventoryUI =
                    GetComponent<
                        InventoryUI
                    >();

            }


            if (
                playerInventory == null
                &&
                inventoryUI != null
            )
            {

                playerInventory =
                    inventoryUI
                        .PlayerInventory;

            }


            if (
                chestManager == null
            )
            {

                chestManager =
                    ChestManager.Instance;

            }


            if (
                chestPanel != null
            )
            {

                chestPanel.SetActive(
                    false
                );

            }

        }


        private void Start()
        {

            BindSlots();


            RefreshAll();

        }


        private void OnDestroy()
        {

            UnsubscribeCurrentChest();


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

            if (
                !IsOpen
            )
            {

                return;

            }


            // InventoryUI owns E / closing behaviour.
            // When it closes, chest follows automatically.
            if (
                inventoryUI == null
                ||
                !inventoryUI.IsOpen
            )
            {

                CloseChestVisualOnly();

                return;

            }


            HandleRightDrag();

        }


        // =====================================================
        // OPEN
        // =====================================================

        public void OpenChest(
            int worldX,
            int worldY
        )
        {

            if (
                chestManager == null
            )
            {

                chestManager =
                    ChestManager.Instance;

            }


            if (
                chestManager == null
                ||
                !chestManager.IsChestAt(
                    worldX,
                    worldY
                )
            )
            {

                return;

            }


            UnsubscribeCurrentChest();


            currentChest =
                chestManager.GetChest(
                    worldX,
                    worldY
                );


            if (
                currentChest == null
            )
            {

                return;

            }


            currentChest.Changed +=
                HandleChestChanged;


            if (
                inventoryUI != null
            )
            {

                inventoryUI.OpenInventory();

            }


            IsOpen =
                true;


            hoveredSlot =
                null;


            rightDragActive =
                false;


            rightDragVisited.Clear();


            if (
                chestPanel != null
            )
            {

                chestPanel.SetActive(
                    true
                );

            }


            if (
                chestTitle != null
            )
            {

                chestTitle.text =
                    "СУНДУК";

            }


            RefreshAll();

        }


        public void CloseChestVisualOnly()
        {

            UnsubscribeCurrentChest();


            currentChest =
                null;


            IsOpen =
                false;


            hoveredSlot =
                null;


            rightDragActive =
                false;


            rightDragVisited.Clear();


            if (
                chestPanel != null
            )
            {

                chestPanel.SetActive(
                    false
                );

            }

        }


        private void UnsubscribeCurrentChest()
        {

            if (
                currentChest != null
            )
            {

                currentChest.Changed -=
                    HandleChestChanged;

            }

        }


        // =====================================================
        // SHIFT: PLAYER -> CHEST
        // =====================================================

        public bool QuickMoveFromPlayer(
            int playerSlotIndex
        )
        {

            if (
                !IsOpen
                ||
                currentChest == null
                ||
                playerInventory == null
            )
            {

                return false;

            }


            return
                currentChest
                    .QuickMoveFromPlayer(
                        playerInventory,
                        playerSlotIndex
                    );

        }


        // =====================================================
        // LEFT CLICK
        // =====================================================

        public void LeftClickChest(
            int chestSlotIndex
        )
        {

            if (
                !IsOpen
                ||
                currentChest == null
                ||
                inventoryUI == null
            )
            {

                return;

            }


            ItemStack cursor =
                inventoryUI.CursorStack;


            bool shift =
                Input.GetKey(
                    KeyCode.LeftShift
                )
                ||
                Input.GetKey(
                    KeyCode.RightShift
                );


            // Shift + left click:
            // chest -> whole player inventory.
            if (
                shift
                &&
                cursor.IsEmpty
            )
            {

                currentChest
                    .QuickMoveToPlayer(
                        chestSlotIndex,
                        playerInventory
                    );


                return;

            }


            currentChest.LeftClickSlot(
                chestSlotIndex,
                cursor
            );


            inventoryUI
                .RefreshCursorExternal();

        }


        // =====================================================
        // RIGHT CLICK / DRAG
        // =====================================================

        public void RightPointerDown(
            int chestSlotIndex
        )
        {

            if (
                !IsOpen
                ||
                currentChest == null
                ||
                inventoryUI == null
            )
            {

                return;

            }


            ItemStack cursor =
                inventoryUI.CursorStack;


            if (
                cursor.IsEmpty
            )
            {

                currentChest.RightClickSlot(
                    chestSlotIndex,
                    cursor
                );


                inventoryUI
                    .RefreshCursorExternal();


                return;

            }


            rightDragActive =
                true;


            rightDragVisited.Clear();


            TryRightDragSlot(
                chestSlotIndex
            );

        }


        private void HandleRightDrag()
        {

            if (
                !rightDragActive
            )
            {

                return;

            }


            if (
                !Input.GetMouseButton(
                    1
                )
                ||
                inventoryUI ==
                null
                ||
                inventoryUI
                    .CursorStack
                    .IsEmpty
            )
            {

                rightDragActive =
                    false;


                rightDragVisited.Clear();


                return;

            }


            if (
                hoveredSlot != null
            )
            {

                TryRightDragSlot(
                    hoveredSlot.SlotIndex
                );

            }

        }


        private void TryRightDragSlot(
            int slotIndex
        )
        {

            if (
                rightDragVisited.Contains(
                    slotIndex
                )
            )
            {

                return;

            }


            rightDragVisited.Add(
                slotIndex
            );


            currentChest
                .PlaceOneFromCursor(
                    slotIndex,
                    inventoryUI.CursorStack
                );


            inventoryUI
                .RefreshCursorExternal();

        }


        public void SetHoveredSlot(
            ChestSlotUI slot
        )
        {

            hoveredSlot =
                slot;


            if (
                rightDragActive
                &&
                Input.GetMouseButton(
                    1
                )
                &&
                slot != null
            )
            {

                TryRightDragSlot(
                    slot.SlotIndex
                );

            }

        }


        public void ClearHoveredSlot(
            ChestSlotUI slot
        )
        {

            if (
                hoveredSlot ==
                slot
            )
            {

                hoveredSlot =
                    null;

            }

        }


        // =====================================================
        // REFRESH
        // =====================================================

        private void BindSlots()
        {

            if (
                chestSlots == null
            )
            {

                return;

            }


            for (
                int i = 0;
                i < chestSlots.Length
                &&
                i < ChestInventoryRuntime.SlotCount;
                i++
            )
            {

                if (
                    chestSlots[i] != null
                )
                {

                    chestSlots[i].Bind(
                        this,
                        i
                    );

                }

            }

        }


        private void HandleChestChanged()
        {

            RefreshAll();

        }


        private void RefreshAll()
        {

            if (
                chestSlots == null
            )
            {

                return;

            }


            for (
                int i = 0;
                i < chestSlots.Length
                &&
                i < ChestInventoryRuntime.SlotCount;
                i++
            )
            {

                ChestSlotUI slot =
                    chestSlots[i];


                if (
                    slot == null
                )
                {

                    continue;

                }


                ItemStack stack =
                    currentChest != null
                        ? currentChest.GetSlot(
                            i
                        )
                        : null;


                slot.Refresh(
                    stack,
                    chestSlotSprite
                );

            }

        }

    }

}
