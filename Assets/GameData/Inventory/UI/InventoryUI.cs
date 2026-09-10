using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;


namespace Game.Inventory.UI
{

    public class InventoryUI :
        MonoBehaviour
    {

        // =====================================================
        // SINGLETON
        // =====================================================

        public static InventoryUI Instance
        {
            get;
            private set;
        }


        // =====================================================
        // INVENTORY
        // =====================================================

        [Header("Inventory")]

        [SerializeField]
        private PlayerInventory inventory;


        // =====================================================
        // PANELS
        // =====================================================

        [Header("Panels")]

        [SerializeField]
        private GameObject inventoryPanel;


        // =====================================================
        // SLOTS
        // =====================================================

        [Header("Hotbar - 9 slots")]

        [SerializeField]
        private InventorySlotUI[] hotbarSlots;


        [Header("Storage - 36 slots")]

        [SerializeField]
        private InventorySlotUI[] storageSlots;


        // =====================================================
        // APPEARANCE
        // =====================================================

        [Header("Slot Appearance")]

        [Tooltip("Обычная текстура ячейки хотбара.")]
        [SerializeField]
        private Sprite hotbarSlotSprite;


        [Tooltip("Обычная текстура ячейки основного инвентаря.")]
        [SerializeField]
        private Sprite storageSlotSprite;


        [Tooltip("Необязательно. Отдельная текстура выбранной ячейки хотбара.")]
        [SerializeField]
        private Sprite selectedHotbarSlotSprite;


        [Tooltip("1.03 = +3%, 1.04 = +4%, 1.05 = +5%.")]
        [SerializeField]
        [Range(1f, 1.1f)]
        private float selectedHotbarScale =
            1.04f;


        // =====================================================
        // CURSOR
        // =====================================================

        [Header("Cursor Item")]

        [SerializeField]
        private RectTransform cursorItemRoot;


        [SerializeField]
        private Image cursorIcon;


        [SerializeField]
        private TMP_Text cursorCount;


        // =====================================================
        // STATE
        // =====================================================

        private readonly ItemStack cursorStack =
            new ItemStack();


        private InventorySlotUI hoveredSlot;


        // =====================================================
        // RIGHT DRAG
        // =====================================================

        private bool rightDragActive;


        private readonly HashSet<int>
            rightDragVisited =
            new HashSet<int>();


        public bool IsOpen
        {
            get;
            private set;
        }


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            Instance =
                this;


            IsOpen =
                false;


            if (
                inventoryPanel != null
            )
            {

                inventoryPanel.SetActive(
                    false
                );

            }


            if (
                cursorItemRoot != null
            )
            {

                cursorItemRoot.gameObject.SetActive(
                    false
                );

            }

        }


        private void Start()
        {

            if (
                inventory == null
            )
            {

                Debug.LogError(
                    "INVENTORY UI: PlayerInventory is not assigned."
                );


                return;

            }


            BindSlots();


            inventory.Changed +=
                RefreshAll;


            RefreshAll();

        }


        private void OnDestroy()
        {

            if (
                inventory != null
            )
            {

                inventory.Changed -=
                    RefreshAll;

            }


            if (
                Instance == this
            )
            {

                Instance =
                    null;

            }

        }


        private void Update()
        {

            if (
                inventory == null
            )
            {

                return;

            }


            HandleInventoryKey();


            HandleHotbarKeys();


            HandleMouseWheel();


            if (
                !IsOpen
            )
            {

                return;

            }


            UpdateCursorPosition();


            HandleDropKey();


            HandleRightDrag();

        }


        // =====================================================
        // BIND
        // =====================================================

        private void BindSlots()
        {

            if (
                hotbarSlots != null
            )
            {

                for (
                    int i = 0;
                    i < hotbarSlots.Length
                    &&
                    i < PlayerInventory.HotbarSize;
                    i++
                )
                {

                    if (
                        hotbarSlots[i] != null
                    )
                    {

                        hotbarSlots[i].Bind(
                            this,
                            i
                        );

                    }

                }

            }


            if (
                storageSlots != null
            )
            {

                for (
                    int i = 0;
                    i < storageSlots.Length
                    &&
                    i < PlayerInventory.StorageSize;
                    i++
                )
                {

                    if (
                        storageSlots[i] != null
                    )
                    {

                        storageSlots[i].Bind(
                            this,
                            PlayerInventory.HotbarSize +
                            i
                        );

                    }

                }

            }

        }


        // =====================================================
        // OPEN / CLOSE
        // =====================================================

        private void HandleInventoryKey()
        {

            if (
                !Input.GetKeyDown(
                    KeyCode.E
                )
            )
            {

                return;

            }


            if (
                IsOpen
            )
            {

                CloseInventory();

            }
            else
            {

                OpenInventory();

            }

        }


        public void OpenInventory()
        {

            IsOpen =
                true;


            hoveredSlot =
                null;


            rightDragActive =
                false;


            rightDragVisited.Clear();


            if (
                inventoryPanel != null
            )
            {

                inventoryPanel.SetActive(
                    true
                );

            }


            RefreshAll();

        }


        public void CloseInventory()
        {

            rightDragActive =
                false;


            rightDragVisited.Clear();


            if (
                !cursorStack.IsEmpty
            )
            {

                bool dropped =
                    inventory.DropCursorStack(
                        cursorStack
                    );


                if (
                    !dropped
                )
                {

                    return;

                }

            }


            IsOpen =
                false;


            hoveredSlot =
                null;


            if (
                inventoryPanel != null
            )
            {

                inventoryPanel.SetActive(
                    false
                );

            }


            RefreshCursor();


            RefreshAll();

        }


        // =====================================================
        // HOTBAR INPUT
        // =====================================================

        private void HandleHotbarKeys()
        {

            for (
                int i = 0;
                i < PlayerInventory.HotbarSize;
                i++
            )
            {

                KeyCode key =
                    (KeyCode)(
                        (int)KeyCode.Alpha1 +
                        i
                    );


                if (
                    Input.GetKeyDown(
                        key
                    )
                )
                {

                    inventory.SelectHotbarSlot(
                        i
                    );

                }

            }

        }


        private void HandleMouseWheel()
        {

            float wheel =
                Input.mouseScrollDelta.y;


            if (
                Mathf.Abs(
                    wheel
                ) <
                0.01f
            )
            {

                return;

            }


            int index =
                inventory.SelectedHotbarIndex;


            index +=
                wheel > 0f
                    ? -1
                    : 1;


            if (
                index < 0
            )
            {

                index =
                    PlayerInventory.HotbarSize -
                    1;

            }


            if (
                index >=
                PlayerInventory.HotbarSize
            )
            {

                index =
                    0;

            }


            inventory.SelectHotbarSlot(
                index
            );

        }


        // =====================================================
        // LEFT CLICK / SHIFT CLICK
        // =====================================================

        public void LeftClick(
            int slotIndex
        )
        {

            if (
                !IsOpen
            )
            {

                return;

            }


            bool shift =
                Input.GetKey(
                    KeyCode.LeftShift
                )
                ||
                Input.GetKey(
                    KeyCode.RightShift
                );


            // Shift-click работает только,
            // когда на курсоре ничего не висит.
            if (
                shift
                &&
                cursorStack.IsEmpty
            )
            {

                inventory.QuickMove(
                    slotIndex
                );


                return;

            }


            inventory.LeftClickSlot(
                slotIndex,
                cursorStack
            );


            RefreshCursor();

        }


        // =====================================================
        // RIGHT CLICK / START RIGHT DRAG
        // =====================================================

        public void RightPointerDown(
            int slotIndex
        )
        {

            if (
                !IsOpen
            )
            {

                return;

            }


            // Пустой курсор:
            // обычный ПКМ = взять половину.
            if (
                cursorStack.IsEmpty
            )
            {

                inventory.RightClickSlot(
                    slotIndex,
                    cursorStack
                );


                RefreshCursor();


                return;

            }


            // На курсоре есть предмет:
            // начинаем раздачу по одному.
            rightDragActive =
                true;


            rightDragVisited.Clear();


            TryRightDragSlot(
                slotIndex
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
                !Input.GetMouseButton(1)
                ||
                cursorStack.IsEmpty
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


            // Один слот за одно удержание ПКМ
            // посещается только один раз.
            rightDragVisited.Add(
                slotIndex
            );


            inventory.PlaceOneFromCursor(
                slotIndex,
                cursorStack
            );


            RefreshCursor();

        }


        // =====================================================
        // HOVER
        // =====================================================

        public void SetHoveredSlot(
            InventorySlotUI slot
        )
        {

            hoveredSlot =
                slot;


            if (
                rightDragActive
                &&
                Input.GetMouseButton(1)
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
            InventorySlotUI slot
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
        // G DROP
        // =====================================================

        private void HandleDropKey()
        {

            if (
                !Input.GetKeyDown(
                    KeyCode.G
                )
                ||
                hoveredSlot == null
            )
            {

                return;

            }


            inventory.DropOneFromSlot(
                hoveredSlot.SlotIndex
            );

        }


        // =====================================================
        // CURSOR
        // =====================================================

        private void UpdateCursorPosition()
        {

            if (
                cursorItemRoot != null
            )
            {

                cursorItemRoot.position =
                    Input.mousePosition;

            }

        }


        private void RefreshCursor()
        {

            if (
                cursorItemRoot == null
            )
            {

                return;

            }


            bool visible =
                IsOpen
                &&
                !cursorStack.IsEmpty;


            cursorItemRoot.gameObject.SetActive(
                visible
            );


            if (
                !visible
            )
            {

                return;

            }


            if (
                cursorIcon != null
            )
            {

                cursorIcon.sprite =
                    ItemIconProvider.GetIcon(
                        cursorStack.ItemId
                    );


                cursorIcon.enabled =
                    cursorIcon.sprite != null;

            }


            if (
                cursorCount != null
            )
            {

                cursorCount.text =
                    cursorStack.Count > 1
                        ? cursorStack.Count.ToString()
                        : string.Empty;

            }

        }


        // =====================================================
        // REFRESH
        // =====================================================

        private void RefreshAll()
        {

            if (
                inventory == null
            )
            {

                return;

            }


            // =================================================
            // HOTBAR
            // =================================================

            if (
                hotbarSlots != null
            )
            {

                for (
                    int i = 0;
                    i < hotbarSlots.Length
                    &&
                    i < PlayerInventory.HotbarSize;
                    i++
                )
                {

                    InventorySlotUI slot =
                        hotbarSlots[i];


                    if (
                        slot == null
                    )
                    {

                        continue;

                    }


                    bool selected =
                        i ==
                        inventory.SelectedHotbarIndex;


                    slot.ApplyBackground(
                        hotbarSlotSprite,
                        selectedHotbarSlotSprite,
                        selected
                    );


                    slot.Refresh(
                        inventory.GetSlot(
                            i
                        ),
                        selected,
                        selectedHotbarScale
                    );

                }

            }


            // =================================================
            // STORAGE
            // =================================================

            if (
                storageSlots != null
            )
            {

                for (
                    int i = 0;
                    i < storageSlots.Length
                    &&
                    i < PlayerInventory.StorageSize;
                    i++
                )
                {

                    InventorySlotUI slot =
                        storageSlots[i];


                    if (
                        slot == null
                    )
                    {

                        continue;

                    }


                    int inventoryIndex =
                        PlayerInventory.HotbarSize +
                        i;


                    slot.ApplyBackground(
                        storageSlotSprite,
                        null,
                        false
                    );


                    slot.Refresh(
                        inventory.GetSlot(
                            inventoryIndex
                        ),
                        false,
                        1f
                    );

                }

            }


            RefreshCursor();

        }

    }

}
