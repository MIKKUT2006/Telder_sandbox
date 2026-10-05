using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Chests.UI;
using Game.Inventory;
using Game.Inventory.UI;

namespace Game.GameplaySystems.Furnace
{
    /// <summary>
    /// Compact furnace container displayed above the normal player inventory.
    /// It deliberately has no title, labels, text status or close button: the
    /// inventory owns E and closing the inventory closes this panel as well.
    /// </summary>
    public sealed class FurnaceUI : MonoBehaviour
    {
        public bool IsOpen =>
            panel != null
            &&
            panel.activeSelf
            &&
            state != null;

        private GameObject panel;
        private FurnaceState state;
        private PlayerInventory inventory;
        private InventoryUI inventoryUI;

        private FurnaceSlotUI inputSlot;
        private FurnaceSlotUI fuelSlot;
        private FurnaceSlotUI outputSlot;

        private FurnaceSlotUI hoveredSlot;
        private bool rightDragActive;

        private readonly HashSet<FurnaceSlotKind>
            rightDragVisited =
                new HashSet<FurnaceSlotKind>();

        private BurnBorderVisual burnBorder;
        private Image smeltProgressFill;

        private string cachedRecipeInputId;
        private float cachedSmeltTime;
        private bool cachedHasRecipe;

        public void Open(
            FurnaceState furnaceState,
            PlayerInventory playerInventory)
        {
            if (furnaceState == null || playerInventory == null)
                return;

            Close();

            state = furnaceState;
            inventory = playerInventory;
            inventoryUI = InventoryUI.Instance;

            if (inventoryUI == null)
            {
                Debug.LogWarning(
                    "FURNACE UI: InventoryUI is required. The furnace is an inventory companion panel."
                );

                state = null;
                inventory = null;
                return;
            }

            // Only one inventory companion should be visible at a time.
            if (ChestUIController.Instance != null &&
                ChestUIController.Instance.IsOpen)
            {
                ChestUIController.Instance.CloseChestVisualOnly();
            }

            Build();

            if (panel == null)
            {
                Debug.LogWarning(
                    "FURNACE UI: InventoryPanel RectTransform was not found."
                );

                state = null;
                inventory = null;
                inventoryUI = null;
                return;
            }

            inventoryUI.OpenInventory();
            panel.SetActive(true);

            hoveredSlot = null;
            rightDragActive = false;
            rightDragVisited.Clear();

            Refresh();
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            // E is intentionally handled only by InventoryUI. Furnace is a
            // companion of that inventory, not a second independent window.
            if (inventoryUI == null || !inventoryUI.IsOpen)
            {
                Close();
                return;
            }

            HandleRightDrag();
            Refresh();
        }

        public void Close()
        {
            rightDragActive = false;
            rightDragVisited.Clear();
            hoveredSlot = null;

            if (panel != null)
                Destroy(panel);

            panel = null;
            inputSlot = null;
            fuelSlot = null;
            outputSlot = null;
            burnBorder = null;
            smeltProgressFill = null;
            cachedRecipeInputId = null;
            cachedSmeltTime = 0f;
            cachedHasRecipe = false;

            state = null;
            inventory = null;
            inventoryUI = null;

            if (FurnaceRuntime.Instance != null)
                FurnaceRuntime.Instance.ForceSave();
        }

        // =====================================================
        // PLAYER INVENTORY -> FURNACE SHIFT CLICK
        // =====================================================

        public bool QuickMoveFromPlayer(int playerSlotIndex)
        {
            if (!IsOpen || inventory == null)
                return false;

            ItemStack source =
                inventory.GetSlot(playerSlotIndex);

            if (source == null ||
                source.IsEmpty ||
                source.HasInstanceData)
            {
                return false;
            }

            bool recipe =
                FurnaceMetadataRegistry.TryRecipe(
                    source.ItemId,
                    out _,
                    out _,
                    out _
                );

            bool fuel =
                FurnaceMetadataRegistry.TryFuel(
                    source.ItemId,
                    out _
                );

            bool moved = false;

            if (recipe)
            {
                moved =
                    MovePlayerStackInto(
                        source,
                        FurnaceSlotKind.Input
                    );
            }

            if (!moved && fuel)
            {
                moved =
                    MovePlayerStackInto(
                        source,
                        FurnaceSlotKind.Fuel
                    );
            }

            if (!moved)
                return false;

            inventory.NotifyExternalChange();
            inventoryUI.RefreshCursorExternal();
            CommitChange();
            return true;
        }

        private bool MovePlayerStackInto(
            ItemStack source,
            FurnaceSlotKind kind)
        {
            if (source == null || source.IsEmpty || !Accepts(kind, source))
                return false;

            TryGetStack(kind, out string targetId, out int targetCount);

            if (!string.IsNullOrWhiteSpace(targetId) &&
                !string.Equals(
                    targetId,
                    source.ItemId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }

            int maxStack =
                FurnaceMetadataRegistry.GetMaxStack(
                    source.ItemId
                );

            int free =
                Mathf.Max(
                    0,
                    maxStack - targetCount
                );

            if (free <= 0)
                return false;

            int moved =
                Mathf.Min(
                    free,
                    source.Count
                );

            if (moved <= 0)
                return false;

            SetStack(
                kind,
                source.ItemId,
                targetCount + moved
            );

            source.Count -= moved;
            if (source.Count <= 0)
                source.Clear();

            return true;
        }

        // =====================================================
        // FURNACE SLOT INTERACTION
        // =====================================================

        internal void LeftClick(FurnaceSlotKind kind)
        {
            if (!IsOpen || inventoryUI == null)
                return;

            ItemStack cursor =
                inventoryUI.CursorStack;

            bool shift =
                Input.GetKey(KeyCode.LeftShift)
                ||
                Input.GetKey(KeyCode.RightShift);

            if (shift && cursor.IsEmpty)
            {
                QuickMoveToPlayer(kind);
                return;
            }

            if (LeftClickInternal(kind, cursor))
            {
                inventoryUI.RefreshCursorExternal();
                CommitChange();
            }
        }

        internal void RightPointerDown(FurnaceSlotKind kind)
        {
            if (!IsOpen || inventoryUI == null)
                return;

            ItemStack cursor =
                inventoryUI.CursorStack;

            if (cursor.IsEmpty)
            {
                if (TakeHalf(kind, cursor))
                {
                    inventoryUI.RefreshCursorExternal();
                    CommitChange();
                }

                return;
            }

            rightDragActive = true;
            rightDragVisited.Clear();
            TryRightDragSlot(kind);
        }

        private bool LeftClickInternal(
            FurnaceSlotKind kind,
            ItemStack cursor)
        {
            if (cursor == null)
                return false;

            TryGetStack(
                kind,
                out string slotId,
                out int slotCount
            );

            // Empty cursor takes the complete furnace stack.
            if (cursor.IsEmpty)
            {
                if (slotCount <= 0 || string.IsNullOrWhiteSpace(slotId))
                    return false;

                cursor.Set(slotId, slotCount);
                ClearStack(kind);
                return true;
            }

            if (cursor.HasInstanceData)
                return false;

            // Output is extraction-only. A matching cursor can receive the
            // output stack, but nothing can ever be inserted into it.
            if (kind == FurnaceSlotKind.Output)
            {
                if (slotCount <= 0 ||
                    string.IsNullOrWhiteSpace(slotId) ||
                    !string.Equals(
                        slotId,
                        cursor.ItemId,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return false;
                }

                int maxStack =
                    FurnaceMetadataRegistry.GetMaxStack(
                        cursor.ItemId
                    );

                int free =
                    Mathf.Max(0, maxStack - cursor.Count);

                if (free <= 0)
                    return false;

                int moved =
                    Mathf.Min(free, slotCount);

                cursor.Count += moved;
                slotCount -= moved;
                SetStack(kind, slotId, slotCount);
                return moved > 0;
            }

            if (!Accepts(kind, cursor))
                return false;

            int targetMax =
                FurnaceMetadataRegistry.GetMaxStack(
                    cursor.ItemId
                );

            if (slotCount <= 0 || string.IsNullOrWhiteSpace(slotId))
            {
                int moved =
                    Mathf.Min(
                        targetMax,
                        cursor.Count
                    );

                SetStack(kind, cursor.ItemId, moved);
                cursor.Count -= moved;

                if (cursor.Count <= 0)
                    cursor.Clear();

                return moved > 0;
            }

            if (string.Equals(
                slotId,
                cursor.ItemId,
                StringComparison.OrdinalIgnoreCase))
            {
                int free =
                    Mathf.Max(
                        0,
                        targetMax - slotCount
                    );

                if (free <= 0)
                    return false;

                int moved =
                    Mathf.Min(
                        free,
                        cursor.Count
                    );

                SetStack(
                    kind,
                    slotId,
                    slotCount + moved
                );

                cursor.Count -= moved;
                if (cursor.Count <= 0)
                    cursor.Clear();

                return moved > 0;
            }

            // Swap two valid simple stacks.
            if (cursor.Count > targetMax)
                return false;

            string oldId = slotId;
            int oldCount = slotCount;

            SetStack(
                kind,
                cursor.ItemId,
                cursor.Count
            );

            cursor.Set(oldId, oldCount);
            return true;
        }

        private bool TakeHalf(
            FurnaceSlotKind kind,
            ItemStack cursor)
        {
            TryGetStack(
                kind,
                out string slotId,
                out int slotCount
            );

            if (slotCount <= 0 || string.IsNullOrWhiteSpace(slotId))
                return false;

            int take =
                (slotCount + 1) / 2;

            cursor.Set(slotId, take);
            slotCount -= take;
            SetStack(kind, slotId, slotCount);
            return true;
        }

        private bool PlaceOneFromCursor(
            FurnaceSlotKind kind,
            ItemStack cursor)
        {
            if (kind == FurnaceSlotKind.Output ||
                cursor == null ||
                cursor.IsEmpty ||
                cursor.HasInstanceData ||
                !Accepts(kind, cursor))
            {
                return false;
            }

            TryGetStack(
                kind,
                out string slotId,
                out int slotCount
            );

            int maxStack =
                FurnaceMetadataRegistry.GetMaxStack(
                    cursor.ItemId
                );

            if (slotCount <= 0 || string.IsNullOrWhiteSpace(slotId))
            {
                SetStack(kind, cursor.ItemId, 1);
                cursor.Count--;

                if (cursor.Count <= 0)
                    cursor.Clear();

                return true;
            }

            if (!string.Equals(
                    slotId,
                    cursor.ItemId,
                    StringComparison.OrdinalIgnoreCase) ||
                slotCount >= maxStack)
            {
                return false;
            }

            SetStack(
                kind,
                slotId,
                slotCount + 1
            );

            cursor.Count--;
            if (cursor.Count <= 0)
                cursor.Clear();

            return true;
        }

        private void QuickMoveToPlayer(FurnaceSlotKind kind)
        {
            if (inventory == null)
                return;

            TryGetStack(
                kind,
                out string id,
                out int count
            );

            if (count <= 0 || string.IsNullOrWhiteSpace(id))
                return;

            int remaining =
                inventory.AddItem(
                    id,
                    count
                );

            if (remaining == count)
                return;

            SetStack(kind, id, remaining);
            CommitChange();
        }

        private bool Accepts(
            FurnaceSlotKind kind,
            ItemStack candidate)
        {
            if (candidate == null ||
                candidate.IsEmpty ||
                candidate.HasInstanceData)
            {
                return false;
            }

            if (kind == FurnaceSlotKind.Input)
            {
                return FurnaceMetadataRegistry.TryRecipe(
                    candidate.ItemId,
                    out _,
                    out _,
                    out _
                );
            }

            if (kind == FurnaceSlotKind.Fuel)
            {
                return FurnaceMetadataRegistry.TryFuel(
                    candidate.ItemId,
                    out _
                );
            }

            return false;
        }

        // =====================================================
        // RIGHT-DRAG DISTRIBUTION
        // =====================================================

        private void HandleRightDrag()
        {
            if (!rightDragActive)
                return;

            if (!Input.GetMouseButton(1) ||
                inventoryUI == null ||
                inventoryUI.CursorStack.IsEmpty)
            {
                rightDragActive = false;
                rightDragVisited.Clear();
                return;
            }

            if (hoveredSlot != null)
                TryRightDragSlot(hoveredSlot.Kind);
        }

        private void TryRightDragSlot(FurnaceSlotKind kind)
        {
            if (rightDragVisited.Contains(kind))
                return;

            rightDragVisited.Add(kind);

            if (PlaceOneFromCursor(
                    kind,
                    inventoryUI.CursorStack))
            {
                inventoryUI.RefreshCursorExternal();
                CommitChange();
            }
        }

        internal void SetHoveredSlot(FurnaceSlotUI slot)
        {
            hoveredSlot = slot;

            if (rightDragActive &&
                Input.GetMouseButton(1) &&
                slot != null)
            {
                TryRightDragSlot(slot.Kind);
            }
        }

        internal void ClearHoveredSlot(FurnaceSlotUI slot)
        {
            if (hoveredSlot == slot)
                hoveredSlot = null;
        }

        // =====================================================
        // STATE ACCESS
        // =====================================================

        internal bool TryGetStack(
            FurnaceSlotKind kind,
            out string id,
            out int count)
        {
            id = null;
            count = 0;

            if (state == null)
                return false;

            switch (kind)
            {
                case FurnaceSlotKind.Input:
                    id = state.InputId;
                    count = state.InputCount;
                    break;

                case FurnaceSlotKind.Fuel:
                    id = state.FuelId;
                    count = state.FuelCount;
                    break;

                case FurnaceSlotKind.Output:
                    id = state.OutputId;
                    count = state.OutputCount;
                    break;
            }

            if (count <= 0 || string.IsNullOrWhiteSpace(id))
            {
                id = null;
                count = 0;
                return false;
            }

            return true;
        }

        private void SetStack(
            FurnaceSlotKind kind,
            string id,
            int count)
        {
            if (state == null)
                return;

            if (count <= 0 || string.IsNullOrWhiteSpace(id))
            {
                id = null;
                count = 0;
            }

            switch (kind)
            {
                case FurnaceSlotKind.Input:
                    bool inputChanged =
                        !string.Equals(
                            state.InputId,
                            id,
                            StringComparison.OrdinalIgnoreCase
                        );

                    state.InputId = id;
                    state.InputCount = count;

                    if (inputChanged || count <= 0)
                        state.SmeltProgress = 0f;
                    break;

                case FurnaceSlotKind.Fuel:
                    state.FuelId = id;
                    state.FuelCount = count;
                    break;

                case FurnaceSlotKind.Output:
                    state.OutputId = id;
                    state.OutputCount = count;
                    break;
            }
        }

        private void ClearStack(FurnaceSlotKind kind)
        {
            SetStack(kind, null, 0);
        }

        private void CommitChange()
        {
            if (FurnaceRuntime.Instance != null)
                FurnaceRuntime.Instance.MarkDirty();

            Refresh();
        }

        // =====================================================
        // BUILD / REFRESH
        // =====================================================

        private void Build()
        {
            if (inventoryUI == null || inventoryUI.InventoryPanelRect == null)
                return;

            FurnaceInventoryAppearance appearance =
                inventoryUI.FurnaceAppearance;

            panel = new GameObject(
                "FurnacePanel",
                typeof(RectTransform),
                typeof(Image)
            );

            panel.transform.SetParent(
                inventoryUI.InventoryPanelRect,
                false
            );

            panel.transform.SetAsLastSibling();

            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            // Bottom-center of the furnace panel is attached just above the
            // top-center of InventoryPanel. It therefore follows any future
            // inventory repositioning/resolution scaling automatically.
            panelRect.anchorMin =
                new Vector2(0.5f, 1f);

            panelRect.anchorMax =
                new Vector2(0.5f, 1f);

            panelRect.pivot =
                new Vector2(0.5f, 0f);

            panelRect.sizeDelta =
                appearance.PanelSize;

            panelRect.anchoredPosition =
                new Vector2(
                    appearance.PanelOffset.x,
                    appearance.InventoryGap +
                    appearance.PanelOffset.y
                );

            Image panelImage =
                panel.GetComponent<Image>();

            ApplyImageStyle(
                panelImage,
                appearance.PanelSprite,
                appearance.PanelColor
            );

            inputSlot = CreateSlot(
                panelRect,
                FurnaceSlotKind.Input,
                appearance.InputSlotPosition,
                appearance.InputSlotSprite,
                appearance.InputSlotColor,
                appearance
            );

            fuelSlot = CreateSlot(
                panelRect,
                FurnaceSlotKind.Fuel,
                appearance.FuelSlotPosition,
                appearance.FuelSlotSprite,
                appearance.FuelSlotColor,
                appearance
            );

            outputSlot = CreateSlot(
                panelRect,
                FurnaceSlotKind.Output,
                appearance.OutputSlotPosition,
                appearance.OutputSlotSprite,
                appearance.OutputSlotColor,
                appearance
            );

            smeltProgressFill =
                BuildSmeltProgress(
                    panelRect,
                    appearance
                );

            burnBorder =
                BuildBurnBorder(
                    fuelSlot.transform as RectTransform,
                    appearance
                );
        }

        private FurnaceSlotUI CreateSlot(
            RectTransform parent,
            FurnaceSlotKind kind,
            Vector2 position,
            Sprite customSprite,
            Color color,
            FurnaceInventoryAppearance appearance)
        {
            GameObject slotObject =
                new GameObject(
                    kind + "Slot",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Outline),
                    typeof(FurnaceSlotUI)
                );

            slotObject.transform.SetParent(
                parent,
                false
            );

            RectTransform rect =
                slotObject.GetComponent<RectTransform>();

            rect.anchorMin =
                rect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.sizeDelta =
                Vector2.one *
                appearance.SlotSize;

            rect.anchoredPosition =
                position;

            Image background =
                slotObject.GetComponent<Image>();

            Sprite slotSprite =
                customSprite != null
                    ? customSprite
                    : inventoryUI.StorageSlotSprite;

            ApplyImageStyle(
                background,
                slotSprite,
                color
            );

            Outline outline =
                slotObject.GetComponent<Outline>();

            outline.effectColor =
                appearance.SlotOutlineColor;

            outline.effectDistance =
                new Vector2(
                    appearance.SlotOutlineDistance,
                    -appearance.SlotOutlineDistance
                );

            GameObject iconObject =
                new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(Image)
                );

            iconObject.transform.SetParent(
                slotObject.transform,
                false
            );

            RectTransform iconRect =
                iconObject.GetComponent<RectTransform>();

            iconRect.anchorMin =
                new Vector2(0.12f, 0.12f);

            iconRect.anchorMax =
                new Vector2(0.88f, 0.88f);

            iconRect.offsetMin =
                Vector2.zero;

            iconRect.offsetMax =
                Vector2.zero;

            Image icon =
                iconObject.GetComponent<Image>();

            icon.preserveAspect =
                true;

            icon.color =
                appearance.IconColor;

            icon.raycastTarget =
                false;

            GameObject countObject =
                new GameObject(
                    "Count",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI)
                );

            countObject.transform.SetParent(
                slotObject.transform,
                false
            );

            RectTransform countRect =
                countObject.GetComponent<RectTransform>();

            countRect.anchorMin =
                Vector2.zero;

            countRect.anchorMax =
                Vector2.one;

            countRect.offsetMin =
                Vector2.zero;

            countRect.offsetMax =
                Vector2.zero;

            TMP_Text countText =
                countObject.GetComponent<TMP_Text>();

            if (inventoryUI.InterfaceFont != null)
                countText.font = inventoryUI.InterfaceFont;

            countText.fontSize =
                15f;

            countText.alignment =
                TextAlignmentOptions.BottomRight;

            countText.margin =
                new Vector4(2f, 2f, 4f, 2f);

            countText.color =
                appearance.CountTextColor;

            countText.raycastTarget =
                false;

            FurnaceSlotUI slot =
                slotObject.GetComponent<FurnaceSlotUI>();

            slot.Configure(
                this,
                kind,
                icon,
                countText
            );

            return slot;
        }

        private void Refresh()
        {
            if (state == null)
                return;

            inputSlot?.Refresh(
                state.InputId,
                state.InputCount
            );

            fuelSlot?.Refresh(
                state.FuelId,
                state.FuelCount
            );

            outputSlot?.Refresh(
                state.OutputId,
                state.OutputCount
            );

            if (!string.Equals(
                    cachedRecipeInputId,
                    state.InputId,
                    StringComparison.OrdinalIgnoreCase))
            {
                cachedRecipeInputId =
                    state.InputId;

                cachedHasRecipe =
                    FurnaceMetadataRegistry.TryRecipe(
                        state.InputId,
                        out _,
                        out _,
                        out cachedSmeltTime
                    );
            }

            float smelt01 =
                cachedHasRecipe && cachedSmeltTime > 0f
                    ? Mathf.Clamp01(
                        state.SmeltProgress /
                        cachedSmeltTime
                    )
                    : 0f;

            SetHorizontalFill(
                smeltProgressFill,
                smelt01
            );

            float burn01 =
                state.BurnTotal > 0f &&
                state.BurnRemaining > 0f
                    ? Mathf.Clamp01(
                        state.BurnRemaining /
                        state.BurnTotal
                    )
                    : 0f;

            burnBorder?.SetProgress(
                burn01
            );
        }

        private Image BuildSmeltProgress(
            RectTransform parent,
            FurnaceInventoryAppearance appearance)
        {
            GameObject backgroundObject =
                new GameObject(
                    "SmeltProgressBackground",
                    typeof(RectTransform),
                    typeof(Image)
                );

            backgroundObject.transform.SetParent(
                parent,
                false
            );

            RectTransform backgroundRect =
                backgroundObject.GetComponent<RectTransform>();

            backgroundRect.anchorMin =
                backgroundRect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            backgroundRect.pivot =
                new Vector2(0.5f, 0.5f);

            backgroundRect.anchoredPosition =
                appearance.SmeltProgressPosition;

            backgroundRect.sizeDelta =
                appearance.SmeltProgressSize;

            Image background =
                backgroundObject.GetComponent<Image>();

            ApplyImageStyle(
                background,
                appearance.SmeltProgressBackgroundSprite,
                appearance.SmeltProgressBackgroundColor
            );

            background.raycastTarget =
                false;

            GameObject fillObject =
                new GameObject(
                    "Fill",
                    typeof(RectTransform),
                    typeof(Image)
                );

            fillObject.transform.SetParent(
                backgroundObject.transform,
                false
            );

            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();

            fillRect.anchorMin =
                Vector2.zero;

            fillRect.anchorMax =
                Vector2.one;

            fillRect.offsetMin =
                Vector2.zero;

            fillRect.offsetMax =
                Vector2.zero;

            Image fill =
                fillObject.GetComponent<Image>();

            ApplyImageStyle(
                fill,
                appearance.SmeltProgressFillSprite,
                appearance.SmeltProgressFillColor
            );

            fill.raycastTarget =
                false;

            SetHorizontalFill(fill, 0f);
            return fill;
        }

        private static void SetHorizontalFill(
            Image image,
            float value)
        {
            if (image == null)
                return;

            RectTransform rect =
                image.rectTransform;

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                new Vector2(
                    Mathf.Clamp01(value),
                    1f
                );

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            image.enabled =
                value > 0f;
        }

        private BurnBorderVisual BuildBurnBorder(
            RectTransform fuelRect,
            FurnaceInventoryAppearance appearance)
        {
            if (fuelRect == null)
                return null;

            BurnBorderVisual result =
                new BurnBorderVisual();

            float padding =
                appearance.BurnBorderPadding;

            if (appearance.BurnBorderSprite != null)
            {
                GameObject borderObject =
                    new GameObject(
                        "BurnBorder",
                        typeof(RectTransform),
                        typeof(Image)
                    );

                borderObject.transform.SetParent(
                    fuelRect,
                    false
                );

                RectTransform rect =
                    borderObject.GetComponent<RectTransform>();

                rect.anchorMin =
                    Vector2.zero;

                rect.anchorMax =
                    Vector2.one;

                rect.offsetMin =
                    new Vector2(-padding, -padding);

                rect.offsetMax =
                    new Vector2(padding, padding);

                Image image =
                    borderObject.GetComponent<Image>();

                image.sprite =
                    appearance.BurnBorderSprite;

                image.color =
                    appearance.BurnBorderColor;

                image.type =
                    Image.Type.Filled;

                image.fillMethod =
                    Image.FillMethod.Radial360;

                image.fillOrigin =
                    (int)Image.Origin360.Bottom;

                image.fillClockwise =
                    true;

                image.fillAmount =
                    0f;

                image.raycastTarget =
                    false;

                result.Radial =
                    image;

                return result;
            }

            result.Edges =
                new Image[4];

            // Order is bottom -> right -> top -> left. With 75% fuel the
            // bottom/right/top remain visible while the left side has faded,
            // matching the perimeter behaviour from the reference sketch.
            result.Edges[0] = CreateSolidEdge(
                fuelRect,
                "BurnBottom",
                appearance.BurnBorderColor
            );

            result.Edges[1] = CreateSolidEdge(
                fuelRect,
                "BurnRight",
                appearance.BurnBorderColor
            );

            result.Edges[2] = CreateSolidEdge(
                fuelRect,
                "BurnTop",
                appearance.BurnBorderColor
            );

            result.Edges[3] = CreateSolidEdge(
                fuelRect,
                "BurnLeft",
                appearance.BurnBorderColor
            );

            float half =
                appearance.SlotSize *
                0.5f +
                padding;

            float thickness =
                appearance.BurnBorderThickness;

            ConfigureHorizontalEdge(
                result.Edges[0].rectTransform,
                -half,
                false,
                thickness,
                appearance.SlotSize + padding * 2f
            );

            ConfigureVerticalEdge(
                result.Edges[1].rectTransform,
                half,
                false,
                thickness,
                appearance.SlotSize + padding * 2f
            );

            ConfigureHorizontalEdge(
                result.Edges[2].rectTransform,
                half,
                true,
                thickness,
                appearance.SlotSize + padding * 2f
            );

            ConfigureVerticalEdge(
                result.Edges[3].rectTransform,
                -half,
                true,
                thickness,
                appearance.SlotSize + padding * 2f
            );

            result.FullEdgeLength =
                appearance.SlotSize +
                padding * 2f;

            return result;
        }

        private static Image CreateSolidEdge(
            Transform parent,
            string name,
            Color color)
        {
            GameObject go =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Image)
                );

            go.transform.SetParent(
                parent,
                false
            );

            Image image =
                go.GetComponent<Image>();

            image.color =
                color;

            image.raycastTarget =
                false;

            return image;
        }

        private static void ConfigureHorizontalEdge(
            RectTransform rect,
            float y,
            bool growFromRight,
            float thickness,
            float length)
        {
            rect.anchorMin =
                rect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(
                    growFromRight ? 1f : 0f,
                    0.5f
                );

            rect.anchoredPosition =
                new Vector2(
                    growFromRight
                        ? length * 0.5f
                        : -length * 0.5f,
                    y
                );

            rect.sizeDelta =
                new Vector2(length, thickness);
        }

        private static void ConfigureVerticalEdge(
            RectTransform rect,
            float x,
            bool growFromTop,
            float thickness,
            float length)
        {
            rect.anchorMin =
                rect.anchorMax =
                    new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(
                    0.5f,
                    growFromTop ? 1f : 0f
                );

            rect.anchoredPosition =
                new Vector2(
                    x,
                    growFromTop
                        ? length * 0.5f
                        : -length * 0.5f
                );

            rect.sizeDelta =
                new Vector2(thickness, length);
        }

        private static void ApplyImageStyle(
            Image image,
            Sprite sprite,
            Color color)
        {
            if (image == null)
                return;

            image.sprite =
                sprite;

            image.color =
                color;

            image.raycastTarget =
                true;

            if (sprite != null)
            {
                Vector4 border =
                    sprite.border;

                image.type =
                    border.sqrMagnitude > 0f
                        ? Image.Type.Sliced
                        : Image.Type.Simple;
            }
        }

        private sealed class BurnBorderVisual
        {
            public Image Radial;
            public Image[] Edges;
            public float FullEdgeLength;

            public void SetProgress(float value)
            {
                value =
                    Mathf.Clamp01(value);

                if (Radial != null)
                {
                    Radial.enabled =
                        value > 0f;

                    Radial.fillAmount =
                        value;

                    return;
                }

                if (Edges == null || Edges.Length != 4)
                    return;

                float total =
                    value * 4f;

                for (int i = 0; i < Edges.Length; i++)
                {
                    Image edge = Edges[i];
                    if (edge == null)
                        continue;

                    float part =
                        Mathf.Clamp01(
                            total - i
                        );

                    edge.enabled =
                        part > 0f;

                    RectTransform rect =
                        edge.rectTransform;

                    Vector2 size =
                        rect.sizeDelta;

                    if (i == 0 || i == 2)
                        size.x = FullEdgeLength * part;
                    else
                        size.y = FullEdgeLength * part;

                    rect.sizeDelta =
                        size;
                }
            }
        }
    }
}
