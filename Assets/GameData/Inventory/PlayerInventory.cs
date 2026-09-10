using System;

using UnityEngine;

using Game.Items;
using Game.World.Items;


namespace Game.Inventory
{

    public class PlayerInventory :
        MonoBehaviour
    {

        // =====================================================
        // SIZE
        // =====================================================

        public const int HotbarSize =
            9;


        public const int StorageSize =
            36;


        public const int SlotCount =
            HotbarSize +
            StorageSize;


        // =====================================================
        // DROP
        // =====================================================

        [Header("Drop")]

        [SerializeField]
        private Transform dropOrigin;


        // =====================================================
        // DATA
        // =====================================================

        private ItemStack[] slots;


        private int selectedHotbarIndex;


        public int SelectedHotbarIndex =>
            selectedHotbarIndex;


        public event Action Changed;


        // =====================================================
        // UNITY
        // =====================================================

        private void Awake()
        {

            slots =
                new ItemStack[
                    SlotCount
                ];


            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {

                slots[i] =
                    new ItemStack();

            }


            if (
                dropOrigin == null
            )
            {

                dropOrigin =
                    transform;

            }

        }


        // =====================================================
        // SLOT
        // =====================================================

        public ItemStack GetSlot(
            int index
        )
        {

            if (
                index < 0
                ||
                index >= slots.Length
            )
            {

                return null;

            }


            return
                slots[index];

        }


        // =====================================================
        // HOTBAR
        // =====================================================

        public void SelectHotbarSlot(
            int index
        )
        {

            index =
                Mathf.Clamp(
                    index,
                    0,
                    HotbarSize - 1
                );


            if (
                selectedHotbarIndex ==
                index
            )
            {

                return;

            }


            selectedHotbarIndex =
                index;


            NotifyChanged();

        }


        public ItemStack GetSelectedStack()
        {

            return
                slots[
                    selectedHotbarIndex
                ];

        }


        public string GetSelectedItemId()
        {

            ItemStack stack =
                GetSelectedStack();


            if (
                stack == null
                ||
                stack.IsEmpty
            )
            {

                return null;

            }


            return
                stack.ItemId;

        }


        // =====================================================
        // ADD ITEM
        // =====================================================

        public int AddItem(
            string itemId,
            int amount
        )
        {

            if (
                amount <= 0
            )
            {

                return 0;

            }


            if (
                !TryGetItemDefinition(
                    itemId,
                    out ItemDefinition item
                )
            )
            {

                Debug.LogWarning(
                    "INVENTORY: Item not registered: " +
                    itemId
                );


                return
                    amount;

            }


            int maxStack =
                GetMaxStack(
                    item
                );


            int remaining =
                amount;


            bool changed =
                false;


            // =================================================
            // EXISTING STACKS FIRST
            // =================================================

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
                    slot.ItemId != itemId
                    ||
                    slot.Count >= maxStack
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


            // =================================================
            // EMPTY SLOTS
            // =================================================

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
        // QUICK MOVE / SHIFT CLICK
        // =====================================================
        //
        // Hotbar -> Storage
        // Storage -> Hotbar
        //
        // Сначала объединяем со стаками,
        // потом используем пустые слоты.
        //
        // =====================================================

        public bool QuickMove(
            int sourceIndex
        )
        {

            ItemStack source =
                GetSlot(
                    sourceIndex
                );


            if (
                source == null
                ||
                source.IsEmpty
            )
            {

                return false;

            }


            int targetStart;
            int targetEnd;


            if (
                sourceIndex <
                HotbarSize
            )
            {

                targetStart =
                    HotbarSize;


                targetEnd =
                    SlotCount;

            }
            else
            {

                targetStart =
                    0;


                targetEnd =
                    HotbarSize;

            }


            string itemId =
                source.ItemId;


            int remaining =
                source.Count;


            int maxStack =
                GetMaxStack(
                    itemId
                );


            bool changed =
                false;


            // =================================================
            // PASS 1: MERGE
            // =================================================

            for (
                int i = targetStart;
                i < targetEnd
                &&
                remaining > 0;
                i++
            )
            {

                ItemStack target =
                    slots[i];


                if (
                    target.IsEmpty
                    ||
                    target.ItemId != itemId
                    ||
                    target.Count >= maxStack
                )
                {

                    continue;

                }


                int moved =
                    Mathf.Min(
                        maxStack -
                        target.Count,
                        remaining
                    );


                target.Count +=
                    moved;


                remaining -=
                    moved;


                changed =
                    true;

            }


            // =================================================
            // PASS 2: EMPTY SLOTS
            // =================================================

            for (
                int i = targetStart;
                i < targetEnd
                &&
                remaining > 0;
                i++
            )
            {

                ItemStack target =
                    slots[i];


                if (
                    !target.IsEmpty
                )
                {

                    continue;

                }


                int moved =
                    Mathf.Min(
                        maxStack,
                        remaining
                    );


                target.Set(
                    itemId,
                    moved
                );


                remaining -=
                    moved;


                changed =
                    true;

            }


            if (
                !changed
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


            string oldItemId =
                slot.ItemId;


            int oldCount =
                slot.Count;


            slot.Set(
                cursor
            );


            cursor.Set(
                oldItemId,
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


            // Пустой курсор -> взять половину.
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
                    (slot.Count + 1) /
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


        // =====================================================
        // PLACE ONE FROM CURSOR
        // =====================================================
        //
        // Используется обычным ПКМ
        // и ПКМ-drag по нескольким слотам.
        //
        // =====================================================

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
        // CONSUME SELECTED
        // =====================================================

        public bool TryConsumeSelected(
            int amount = 1
        )
        {

            if (
                amount <= 0
            )
            {

                return true;

            }


            ItemStack stack =
                GetSelectedStack();


            if (
                stack == null
                ||
                stack.IsEmpty
                ||
                stack.Count < amount
            )
            {

                return false;

            }


            stack.Count -=
                amount;


            NotifyChanged();


            return true;

        }


        // =====================================================
        // DROP ONE
        // =====================================================

        public bool DropOneFromSlot(
            int index
        )
        {

            ItemStack slot =
                GetSlot(
                    index
                );


            if (
                slot == null
                ||
                slot.IsEmpty
            )
            {

                return false;

            }


            if (
                ItemDropSpawner.Instance ==
                null
            )
            {

                return false;

            }


            bool spawned =
                ItemDropSpawner.Instance
                    .SpawnFromPlayer(
                        slot.ItemId,
                        1,
                        GetDropPosition()
                    );


            if (
                !spawned
            )
            {

                return false;

            }


            slot.Count--;


            NotifyChanged();


            return true;

        }


        // =====================================================
        // DROP CURSOR
        // =====================================================

        public bool DropCursorStack(
            ItemStack cursor
        )
        {

            if (
                cursor == null
                ||
                cursor.IsEmpty
            )
            {

                return true;

            }


            if (
                ItemDropSpawner.Instance ==
                null
            )
            {

                return false;

            }


            bool spawned =
                ItemDropSpawner.Instance
                    .SpawnFromPlayer(
                        cursor.ItemId,
                        cursor.Count,
                        GetDropPosition()
                    );


            if (
                !spawned
            )
            {

                return false;

            }


            cursor.Clear();


            return true;

        }


        // =====================================================
        // HELPERS
        // =====================================================

        private bool TryGetItemDefinition(
            string itemId,
            out ItemDefinition item
        )
        {

            item =
                null;


            if (
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {

                return false;

            }


            return
                ItemRegistry.TryGet(
                    itemId,
                    out item
                );

        }


        private int GetMaxStack(
            string itemId
        )
        {

            if (
                !TryGetItemDefinition(
                    itemId,
                    out ItemDefinition item
                )
            )
            {

                return 100;

            }


            return
                GetMaxStack(
                    item
                );

        }


        private int GetMaxStack(
            ItemDefinition item
        )
        {

            if (
                item == null
            )
            {

                return 100;

            }


            return
                Mathf.Max(
                    1,
                    item.GetMaxStack()
                );

        }


        private Vector2 GetDropPosition()
        {

            Transform origin =
                dropOrigin != null
                    ? dropOrigin
                    : transform;


            return
                (Vector2)origin.position
                +
                Vector2.up *
                0.25f;

        }


        private void NotifyChanged()
        {

            Changed?.Invoke();

        }

    }

}
