using System;

using UnityEngine;

using Game.Inventory;
using Game.Items;
using Game.Save;


namespace Game.Chests
{

    public sealed class ChestInventoryRuntime
    {

        public const int SlotCount =
            27;


        public readonly int WorldX;

        public readonly int WorldY;


        private readonly ItemStack[] slots =
            new ItemStack[
                SlotCount
            ];


        public event Action Changed;


        public ChestInventoryRuntime(
            int worldX,
            int worldY,
            ChestSaveData savedData = null
        )
        {

            WorldX =
                worldX;


            WorldY =
                worldY;


            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {

                slots[i] =
                    new ItemStack();

            }


            Load(
                savedData
            );

        }


        public ItemStack GetSlot(
            int index
        )
        {

            if (
                index < 0
                ||
                index >=
                SlotCount
            )
            {

                return null;

            }


            return
                slots[index];

        }


        public void Load(
            ChestSaveData data
        )
        {

            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {

                slots[i].Clear();

            }


            if (
                data == null
                ||
                data.Slots == null
            )
            {

                return;

            }


            for (
                int i = 0;
                i < data.Slots.Count;
                i++
            )
            {

                ChestSlotSaveData saved =
                    data.Slots[i];


                if (
                    saved == null
                    ||
                    saved.Slot < 0
                    ||
                    saved.Slot >=
                    SlotCount
                    ||
                    string.IsNullOrWhiteSpace(
                        saved.ItemId
                    )
                    ||
                    saved.Count <= 0
                )
                {

                    continue;

                }


                if (
                    !ItemRegistry.TryGet(
                        saved.ItemId,
                        out ItemDefinition item
                    )
                )
                {

                    continue;

                }


                int maxStack =
                    Mathf.Max(
                        1,
                        item.GetMaxStack()
                    );


                slots[
                    saved.Slot
                ].Set(
                    saved.ItemId,
                    Mathf.Min(
                        saved.Count,
                        maxStack
                    )
                );

            }

        }


        // =====================================================
        // ADD
        // =====================================================

        public int AddItem(
            string itemId,
            int amount
        )
        {

            if (
                amount <= 0
                ||
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {

                return
                    Mathf.Max(
                        0,
                        amount
                    );

            }


            int maxStack =
                GetMaxStack(
                    itemId
                );


            int remaining =
                amount;


            bool changed =
                false;


            // Existing stacks.
            for (
                int i = 0;
                i < slots.Length
                &&
                remaining > 0;
                i++
            )
            {

                ItemStack slot =
                    slots[i];


                if (
                    slot.IsEmpty
                    ||
                    slot.ItemId !=
                    itemId
                    ||
                    slot.Count >=
                    maxStack
                )
                {

                    continue;

                }


                int moved =
                    Mathf.Min(
                        maxStack -
                        slot.Count,
                        remaining
                    );


                slot.Count +=
                    moved;


                remaining -=
                    moved;


                changed =
                    true;

            }


            // Empty slots.
            for (
                int i = 0;
                i < slots.Length
                &&
                remaining > 0;
                i++
            )
            {

                ItemStack slot =
                    slots[i];


                if (
                    !slot.IsEmpty
                )
                {

                    continue;

                }


                int moved =
                    Mathf.Min(
                        maxStack,
                        remaining
                    );


                slot.Set(
                    itemId,
                    moved
                );


                remaining -=
                    moved;


                changed =
                    true;

            }


            if (
                changed
            )
            {

                NotifyChanged();

            }


            return
                remaining;

        }


        // =====================================================
        // PLAYER -> CHEST
        // =====================================================

        public bool QuickMoveFromPlayer(
            PlayerInventory inventory,
            int playerSlotIndex
        )
        {

            if (
                inventory == null
            )
            {

                return false;

            }


            ItemStack source =
                inventory.GetSlot(
                    playerSlotIndex
                );


            if (
                source == null
                ||
                source.IsEmpty
            )
            {

                return false;

            }


            int before =
                source.Count;


            int remaining =
                AddItem(
                    source.ItemId,
                    before
                );


            int moved =
                before -
                remaining;


            if (
                moved <= 0
            )
            {

                return false;

            }


            inventory.RemoveFromSlot(
                playerSlotIndex,
                moved
            );


            return true;

        }


        // =====================================================
        // CHEST -> PLAYER
        // =====================================================

        public bool QuickMoveToPlayer(
            int chestSlotIndex,
            PlayerInventory inventory
        )
        {

            if (
                inventory == null
            )
            {

                return false;

            }


            ItemStack source =
                GetSlot(
                    chestSlotIndex
                );


            if (
                source == null
                ||
                source.IsEmpty
            )
            {

                return false;

            }


            int before =
                source.Count;


            int remaining =
                inventory.AddItem(
                    source.ItemId,
                    before
                );


            int moved =
                before -
                remaining;


            if (
                moved <= 0
            )
            {

                return false;

            }


            source.Count =
                remaining;


            NotifyChanged();


            return true;

        }


        // =====================================================
        // LEFT CLICK
        // =====================================================

        public void LeftClickSlot(
            int index,
            ItemStack cursor
        )
        {

            ItemStack slot =
                GetSlot(
                    index
                );


            if (
                slot == null
                ||
                cursor == null
            )
            {

                return;

            }


            if (
                cursor.IsEmpty
            )
            {

                if (
                    slot.IsEmpty
                )
                {

                    return;

                }


                cursor.Set(
                    slot
                );


                slot.Clear();


                NotifyChanged();


                return;

            }


            if (
                slot.IsEmpty
            )
            {

                slot.Set(
                    cursor
                );


                cursor.Clear();


                NotifyChanged();


                return;

            }


            if (
                slot.ItemId ==
                cursor.ItemId
            )
            {

                int maxStack =
                    GetMaxStack(
                        slot.ItemId
                    );


                int free =
                    maxStack -
                    slot.Count;


                if (
                    free <= 0
                )
                {

                    return;

                }


                int moved =
                    Mathf.Min(
                        free,
                        cursor.Count
                    );


                slot.Count +=
                    moved;


                cursor.Count -=
                    moved;


                NotifyChanged();


                return;

            }


            string oldId =
                slot.ItemId;


            int oldCount =
                slot.Count;


            slot.Set(
                cursor
            );


            cursor.Set(
                oldId,
                oldCount
            );


            NotifyChanged();

        }


        // =====================================================
        // RIGHT CLICK
        // =====================================================

        public void RightClickSlot(
            int index,
            ItemStack cursor
        )
        {

            ItemStack slot =
                GetSlot(
                    index
                );


            if (
                slot == null
                ||
                cursor == null
            )
            {

                return;

            }


            if (
                cursor.IsEmpty
            )
            {

                if (
                    slot.IsEmpty
                )
                {

                    return;

                }


                int take =
                    (
                        slot.Count +
                        1
                    ) /
                    2;


                cursor.Set(
                    slot.ItemId,
                    take
                );


                slot.Count -=
                    take;


                NotifyChanged();


                return;

            }


            PlaceOneFromCursor(
                index,
                cursor
            );

        }


        public bool PlaceOneFromCursor(
            int index,
            ItemStack cursor
        )
        {

            if (
                cursor == null
                ||
                cursor.IsEmpty
            )
            {

                return false;

            }


            ItemStack slot =
                GetSlot(
                    index
                );


            if (
                slot == null
            )
            {

                return false;

            }


            if (
                slot.IsEmpty
            )
            {

                slot.Set(
                    cursor.ItemId,
                    1
                );


                cursor.Count--;


                NotifyChanged();


                return true;

            }


            if (
                slot.ItemId !=
                cursor.ItemId
            )
            {

                return false;

            }


            int maxStack =
                GetMaxStack(
                    slot.ItemId
                );


            if (
                slot.Count >=
                maxStack
            )
            {

                return false;

            }


            slot.Count++;


            cursor.Count--;


            NotifyChanged();


            return true;

        }


        // =====================================================
        // SAVE SNAPSHOT
        // =====================================================

        public void BuildSaveArrays(
            out string[] itemIds,
            out int[] counts
        )
        {

            itemIds =
                new string[
                    SlotCount
                ];


            counts =
                new int[
                    SlotCount
                ];


            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {

                ItemStack slot =
                    slots[i];


                if (
                    slot == null
                    ||
                    slot.IsEmpty
                )
                {

                    continue;

                }


                itemIds[i] =
                    slot.ItemId;


                counts[i] =
                    slot.Count;

            }

        }


        private int GetMaxStack(
            string itemId
        )
        {

            if (
                ItemRegistry.TryGet(
                    itemId,
                    out ItemDefinition item
                )
                &&
                item != null
            )
            {

                return
                    Mathf.Max(
                        1,
                        item.GetMaxStack()
                    );

            }


            return 100;

        }


        private void NotifyChanged()
        {

            Changed?.Invoke();

        }

    }

}
