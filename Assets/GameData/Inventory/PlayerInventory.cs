using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Game.Items;
using Game.World.Items;
using Game.Items.Durability;

namespace Game.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        public const int HotbarSize = 9;
        public const int StorageSize = 36;
        public const int SlotCount = HotbarSize + StorageSize;

        [Header("Drop")]
        [SerializeField] private Transform dropOrigin;

        private ItemStack[] slots;
        private int selectedHotbarIndex;

        public int SelectedHotbarIndex => selectedHotbarIndex;
        public event Action Changed;

        private void Awake()
        {
            slots = new ItemStack[SlotCount];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = new ItemStack();

            if (dropOrigin == null)
                dropOrigin = transform;
        }

        public ItemStack GetSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public void SelectHotbarSlot(int index)
        {
            index = Mathf.Clamp(index, 0, HotbarSize - 1);
            if (selectedHotbarIndex == index) return;
            selectedHotbarIndex = index;
            NotifyChanged();
        }

        public ItemStack GetSelectedStack() => slots[selectedHotbarIndex];

        public string GetSelectedItemId()
        {
            ItemStack stack = GetSelectedStack();
            return stack == null || stack.IsEmpty ? null : stack.ItemId;
        }

        public int AddItem(string itemId, int amount)
        {
            if (amount <= 0) return 0;
            if (!TryGetItemDefinition(itemId, out ItemDefinition item))
            {
                Debug.LogWarning("INVENTORY: Item not registered: " + itemId);
                return amount;
            }

            int maxStack = GetMaxStack(item);
            int remaining = amount;
            bool changed = false;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack slot = slots[i];
                if (slot.IsEmpty || slot.ItemId != itemId || slot.Count >= maxStack || slot.HasInstanceData)
                    continue;

                int moved = Mathf.Min(maxStack - slot.Count, remaining);
                slot.Count += moved;
                remaining -= moved;
                changed = true;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack slot = slots[i];
                if (!slot.IsEmpty) continue;

                int moved = Mathf.Min(maxStack, remaining);
                slot.Set(itemId, moved);
                remaining -= moved;
                changed = true;
            }

            if (changed) NotifyChanged();
            return remaining;
        }

        // Adds the exact stack and preserves durability/upgrades.
        public bool TryAddStack(ItemStack source)
        {
            if (source == null || source.IsEmpty) return true;
            if (!TryGetItemDefinition(source.ItemId, out ItemDefinition item)) return false;

            int maxStack = GetMaxStack(item);
            bool exactInstance = source.HasInstanceData || ItemDurabilityMetadata.GetDurability(source.ItemId) > 0 || maxStack <= 1;

            if (exactInstance)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty) continue;
                    slots[i].Set(source);
                    source.Clear();
                    NotifyChanged();
                    return true;
                }
                return false;
            }

            int remaining = source.Count;
            bool changed = false;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack target = slots[i];
                if (target.IsEmpty || target.ItemId != source.ItemId || target.Count >= maxStack || target.HasInstanceData)
                    continue;

                int moved = Mathf.Min(maxStack - target.Count, remaining);
                target.Count += moved;
                remaining -= moved;
                changed = true;
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                ItemStack target = slots[i];
                if (!target.IsEmpty) continue;

                int moved = Mathf.Min(maxStack, remaining);
                target.Set(source.ItemId, moved);
                remaining -= moved;
                changed = true;
            }

            source.Count = remaining;
            if (changed) NotifyChanged();
            return remaining <= 0;
        }

        public bool QuickMove(int sourceIndex)
        {
            ItemStack source = GetSlot(sourceIndex);
            if (source == null || source.IsEmpty) return false;

            int targetStart = sourceIndex < HotbarSize ? HotbarSize : 0;
            int targetEnd = sourceIndex < HotbarSize ? SlotCount : HotbarSize;
            int maxStack = GetMaxStack(source.ItemId);
            bool exactInstance = source.HasInstanceData || maxStack <= 1;

            if (exactInstance)
            {
                for (int i = targetStart; i < targetEnd; i++)
                {
                    if (!slots[i].IsEmpty) continue;
                    slots[i].Set(source);
                    source.Clear();
                    NotifyChanged();
                    return true;
                }
                return false;
            }

            string itemId = source.ItemId;
            int remaining = source.Count;
            bool changed = false;

            for (int i = targetStart; i < targetEnd && remaining > 0; i++)
            {
                ItemStack target = slots[i];
                if (target.IsEmpty || target.ItemId != itemId || target.Count >= maxStack || target.HasInstanceData)
                    continue;

                int moved = Mathf.Min(maxStack - target.Count, remaining);
                target.Count += moved;
                remaining -= moved;
                changed = true;
            }

            for (int i = targetStart; i < targetEnd && remaining > 0; i++)
            {
                ItemStack target = slots[i];
                if (!target.IsEmpty) continue;

                int moved = Mathf.Min(maxStack, remaining);
                target.Set(itemId, moved);
                remaining -= moved;
                changed = true;
            }

            if (!changed) return false;
            source.Count = remaining;
            NotifyChanged();
            return true;
        }

        public void LeftClickSlot(int index, ItemStack cursor)
        {
            ItemStack slot = GetSlot(index);
            if (slot == null || cursor == null) return;

            if (cursor.IsEmpty)
            {
                if (slot.IsEmpty) return;
                cursor.Set(slot);
                slot.Clear();
                NotifyChanged();
                return;
            }

            if (slot.IsEmpty)
            {
                slot.Set(cursor);
                cursor.Clear();
                NotifyChanged();
                return;
            }

            if (slot.ItemId == cursor.ItemId && !slot.HasInstanceData && !cursor.HasInstanceData)
            {
                int maxStack = GetMaxStack(slot.ItemId);
                int free = maxStack - slot.Count;
                if (free > 0)
                {
                    int moved = Mathf.Min(free, cursor.Count);
                    slot.Count += moved;
                    cursor.Count -= moved;
                    NotifyChanged();
                    return;
                }
            }

            ItemStack old = slot.Clone();
            slot.Set(cursor);
            cursor.Set(old);
            NotifyChanged();
        }

        public void RightClickSlot(int index, ItemStack cursor)
        {
            ItemStack slot = GetSlot(index);
            if (slot == null || cursor == null) return;

            if (cursor.IsEmpty)
            {
                if (slot.IsEmpty) return;
                int maxStack = GetMaxStack(slot.ItemId);

                if (slot.HasInstanceData || maxStack <= 1)
                {
                    cursor.Set(slot);
                    slot.Clear();
                }
                else
                {
                    int take = (slot.Count + 1) / 2;
                    cursor.Set(slot.ItemId, take);
                    slot.Count -= take;
                }

                NotifyChanged();
                return;
            }

            PlaceOneFromCursor(index, cursor);
        }

        public bool PlaceOneFromCursor(int index, ItemStack cursor)
        {
            if (cursor == null || cursor.IsEmpty) return false;
            ItemStack slot = GetSlot(index);
            if (slot == null) return false;

            int maxStack = GetMaxStack(cursor.ItemId);
            if (cursor.HasInstanceData || maxStack <= 1)
            {
                if (!slot.IsEmpty) return false;
                slot.Set(cursor);
                cursor.Clear();
                NotifyChanged();
                return true;
            }

            if (slot.IsEmpty)
            {
                slot.Set(cursor.ItemId, 1);
                cursor.Count--;
                NotifyChanged();
                return true;
            }

            if (slot.ItemId != cursor.ItemId || slot.HasInstanceData || slot.Count >= maxStack)
                return false;

            slot.Count++;
            cursor.Count--;
            NotifyChanged();
            return true;
        }

        public bool TryConsumeSelected(int amount = 1)
        {
            if (amount <= 0) return true;
            ItemStack stack = GetSelectedStack();
            if (stack == null || stack.IsEmpty || stack.Count < amount) return false;
            stack.Count -= amount;
            NotifyChanged();
            return true;
        }

        public int CountItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return 0;
            int total = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                ItemStack slot = slots[i];
                if (slot != null && !slot.IsEmpty && slot.ItemId == itemId)
                    total += slot.Count;
            }
            return total;
        }

        public bool TryConsumeItem(string itemId, int amount)
        {
            if (amount <= 0) return true;
            if (CountItem(itemId) < amount) return false;

            int remaining = amount;
            for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack slot = slots[i];
                if (slot == null || slot.IsEmpty || slot.ItemId != itemId) continue;

                int take = Mathf.Min(remaining, slot.Count);
                slot.Count -= take;
                remaining -= take;
            }

            NotifyChanged();
            return true;
        }

        public bool DropOneFromSlot(int index)
        {
            ItemStack slot = GetSlot(index);
            if (slot == null || slot.IsEmpty || ItemDropSpawner.Instance == null) return false;

            bool spawned = ItemDropSpawner.Instance.SpawnFromPlayer(slot.ItemId, 1, GetDropPosition());
            if (!spawned) return false;

            slot.Count--;
            NotifyChanged();
            return true;
        }

        public bool DropCursorStack(ItemStack cursor)
        {
            if (cursor == null || cursor.IsEmpty) return true;
            if (ItemDropSpawner.Instance == null) return false;

            bool spawned = ItemDropSpawner.Instance.SpawnFromPlayer(cursor.ItemId, cursor.Count, GetDropPosition());
            if (!spawned) return false;

            cursor.Clear();
            return true;
        }


        // =====================================================
        // COMPATIBILITY API
        // =====================================================
        //
        // These methods existed in newer project systems
        // (crafting / chests / save runtime). The first archive
        // accidentally replaced PlayerInventory with an older
        // reduced API. They are restored here.
        // =====================================================

        public bool HasItem(
            string itemId,
            int amount = 1
        )
        {
            if (
                amount <=
                0
            )
            {
                return true;
            }


            return
                CountItem(
                    itemId
                ) >=
                amount;
        }


        public bool RemoveItem(
            string itemId,
            int amount = 1
        )
        {
            return
                TryConsumeItem(
                    itemId,
                    amount
                );
        }


        public bool RemoveFromSlot(
            int index,
            int amount = 1
        )
        {
            if (
                amount <=
                0
            )
            {
                return true;
            }


            ItemStack slot =
                GetSlot(
                    index
                );


            if (
                slot ==
                null
                ||
                slot.IsEmpty
                ||
                slot.Count <
                amount
            )
            {
                return false;
            }


            slot.Count -=
                amount;


            NotifyChanged();


            return true;
        }


        public bool CanAddItem(
            string itemId,
            int amount = 1
        )
        {
            if (
                amount <=
                0
            )
            {
                return true;
            }


            if (
                !TryGetItemDefinition(
                    itemId,
                    out ItemDefinition item
                )
            )
            {
                return false;
            }


            int maxStack =
                GetMaxStack(
                    item
                );


            int remaining =
                amount;


            // Existing compatible stacks.
            for (
                int i = 0;
                i < slots.Length
                &&
                remaining >
                0;
                i++
            )
            {
                ItemStack slot =
                    slots[i];


                if (
                    slot ==
                    null
                    ||
                    slot.IsEmpty
                    ||
                    slot.ItemId !=
                    itemId
                    ||
                    slot.HasInstanceData
                    ||
                    slot.Count >=
                    maxStack
                )
                {
                    continue;
                }


                remaining -=
                    Mathf.Min(
                        maxStack -
                        slot.Count,
                        remaining
                    );
            }


            // Empty slots.
            for (
                int i = 0;
                i < slots.Length
                &&
                remaining >
                0;
                i++
            )
            {
                ItemStack slot =
                    slots[i];


                if (
                    slot ==
                    null
                    ||
                    !slot.IsEmpty
                )
                {
                    continue;
                }


                remaining -=
                    Mathf.Min(
                        maxStack,
                        remaining
                    );
            }


            return
                remaining <=
                0;
        }


        /// <summary>
        /// Compatibility restore used by the current SaveGameRuntime.
        /// Accepts the project's save payload without taking a compile-time
        /// dependency on its concrete DTO type.
        /// </summary>
        public bool RestoreFromSave(
            object saveData
        )
        {
            EnsureSlotsCreated();


            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {
                slots[i].Clear();
            }


            object collection =
                ExtractCollection(
                    saveData
                );


            if (
                collection is IEnumerable enumerable
            )
            {
                int fallbackIndex =
                    0;


                foreach (
                    object entry
                    in enumerable
                )
                {
                    if (
                        entry ==
                        null
                    )
                    {
                        fallbackIndex++;

                        continue;
                    }


                    int index =
                        ReadIntMember(
                            entry,
                            fallbackIndex,
                            "SlotIndex",
                            "Slot",
                            "Index"
                        );


                    string itemId =
                        ReadStringMember(
                            entry,
                            "ItemId",
                            "ItemID",
                            "ID"
                        );


                    int count =
                        ReadIntMember(
                            entry,
                            0,
                            "Count",
                            "Amount"
                        );


                    if (
                        index >=
                        0
                        &&
                        index <
                        slots.Length
                        &&
                        !string.IsNullOrWhiteSpace(
                            itemId
                        )
                        &&
                        count >
                        0
                    )
                    {
                        slots[index].Set(
                            itemId,
                            count
                        );


                        slots[index].CurrentDurability =
                            ReadIntMember(
                                entry,
                                -1,
                                "CurrentDurability",
                                "Durability"
                            );


                        slots[index].BonusMaxDurability =
                            Mathf.Max(
                                0,
                                ReadIntMember(
                                    entry,
                                    0,
                                    "BonusMaxDurability"
                                )
                            );


                        slots[index].InstalledUpgradeId =
                            ReadStringMember(
                                entry,
                                "InstalledUpgradeId",
                                "UpgradeId",
                                "UpgradeID"
                            );
                    }


                    fallbackIndex++;
                }


                NotifyChanged();


                return true;
            }


            // Unknown/empty save payload: inventory stays empty,
            // but this is still a valid restore operation.
            NotifyChanged();


            return
                saveData ==
                null;
        }


        /// <summary>
        /// Compatibility overload for the current SaveGameRuntime.
        ///
        /// The project currently calls RestoreFromSave with THREE arguments.
        /// The exact save DTO is intentionally not referenced here so this
        /// inventory remains compatible with different save-data versions.
        ///
        /// Supported shapes include:
        /// - itemIds enumerable + counts enumerable + selectedHotbarIndex
        /// - slot DTO collection + selectedHotbarIndex + any extra value
        /// - any three arguments where one contains a slot collection
        /// </summary>
        public bool RestoreFromSave(
            object first,
            object second,
            object third
        )
        {
            EnsureSlotsCreated();


            ClearAllSlotsForRestore();


            bool restored =
                false;


            // Most likely/current legacy format:
            // RestoreFromSave(itemIds, counts, selectedHotbarIndex)
            if (
                TryRestoreParallelArrays(
                    first,
                    second
                )
            )
            {
                restored =
                    true;
            }
            else
            {
                // Newer save DTO variants.
                if (
                    TryRestoreStructuredArgument(
                        first
                    )
                )
                {
                    restored =
                        true;
                }
                else if (
                    TryRestoreStructuredArgument(
                        second
                    )
                )
                {
                    restored =
                        true;
                }
                else if (
                    TryRestoreStructuredArgument(
                        third
                    )
                )
                {
                    restored =
                        true;
                }
            }


            int selected =
                TryReadScalarInt(
                    third,
                    int.MinValue
                );


            if (
                selected ==
                int.MinValue
            )
            {
                selected =
                    TryReadScalarInt(
                        second,
                        int.MinValue
                    );
            }


            if (
                selected ==
                int.MinValue
            )
            {
                selected =
                    TryReadScalarInt(
                        first,
                        int.MinValue
                    );
            }


            if (
                selected !=
                int.MinValue
            )
            {
                selectedHotbarIndex =
                    Mathf.Clamp(
                        selected,
                        0,
                        HotbarSize - 1
                    );
            }


            NotifyChanged();


            return
                restored;
        }


        private void ClearAllSlotsForRestore()
        {
            EnsureSlotsCreated();


            for (
                int i = 0;
                i < slots.Length;
                i++
            )
            {
                slots[i].Clear();
            }
        }


        private bool TryRestoreParallelArrays(
            object first,
            object second
        )
        {
            if (
                !TryToObjectList(
                    first,
                    out System.Collections.Generic.List<object> itemValues
                )
                ||
                !TryToObjectList(
                    second,
                    out System.Collections.Generic.List<object> countValues
                )
            )
            {
                return false;
            }


            if (
                itemValues.Count ==
                0
                &&
                countValues.Count ==
                0
            )
            {
                return true;
            }


            // Do not mistake a DTO collection for parallel item-id data.
            object firstNonNull =
                null;


            for (
                int i = 0;
                i < itemValues.Count;
                i++
            )
            {
                if (
                    itemValues[i] !=
                    null
                )
                {
                    firstNonNull =
                        itemValues[i];

                    break;
                }
            }


            if (
                firstNonNull !=
                null
                &&
                !(firstNonNull is string)
            )
            {
                return false;
            }


            int limit =
                Mathf.Min(
                    SlotCount,
                    Mathf.Min(
                        itemValues.Count,
                        countValues.Count
                    )
                );


            for (
                int i = 0;
                i < limit;
                i++
            )
            {
                string itemId =
                    itemValues[i] !=
                    null
                        ? itemValues[i].ToString()
                        : "";


                int count =
                    TryReadScalarInt(
                        countValues[i],
                        0
                    );


                if (
                    string.IsNullOrWhiteSpace(
                        itemId
                    )
                    ||
                    count <=
                    0
                )
                {
                    continue;
                }


                slots[i].Set(
                    itemId,
                    count
                );
            }


            return true;
        }


        private bool TryRestoreStructuredArgument(
            object value
        )
        {
            object collection =
                ExtractCollection(
                    value
                );


            if (
                !(collection is IEnumerable enumerable)
                ||
                collection is string
            )
            {
                return false;
            }


            int fallbackIndex =
                0;


            bool sawEntry =
                false;


            foreach (
                object entry
                in enumerable
            )
            {
                if (
                    entry ==
                    null
                )
                {
                    fallbackIndex++;

                    continue;
                }


                // A raw string collection is handled by
                // TryRestoreParallelArrays, not here.
                if (
                    entry is string
                )
                {
                    return false;
                }


                sawEntry =
                    true;


                int index =
                    ReadIntMember(
                        entry,
                        fallbackIndex,
                        "SlotIndex",
                        "Slot",
                        "Index"
                    );


                string itemId =
                    ReadStringMember(
                        entry,
                        "ItemId",
                        "ItemID",
                        "ID"
                    );


                int count =
                    ReadIntMember(
                        entry,
                        0,
                        "Count",
                        "Amount"
                    );


                if (
                    index >=
                    0
                    &&
                    index <
                    slots.Length
                    &&
                    !string.IsNullOrWhiteSpace(
                        itemId
                    )
                    &&
                    count >
                    0
                )
                {
                    slots[index].Set(
                        itemId,
                        count
                    );


                    slots[index].CurrentDurability =
                        ReadIntMember(
                            entry,
                            -1,
                            "CurrentDurability",
                            "Durability"
                        );


                    slots[index].BonusMaxDurability =
                        Mathf.Max(
                            0,
                            ReadIntMember(
                                entry,
                                0,
                                "BonusMaxDurability"
                            )
                        );


                    slots[index].InstalledUpgradeId =
                        ReadStringMember(
                            entry,
                            "InstalledUpgradeId",
                            "UpgradeId",
                            "UpgradeID"
                        );
                }


                fallbackIndex++;
            }


            return
                sawEntry;
        }


        private static bool TryToObjectList(
            object value,
            out System.Collections.Generic.List<object> result
        )
        {
            result =
                new System.Collections.Generic.List<object>();


            if (
                value ==
                null
                ||
                value is string
                ||
                !(value is IEnumerable enumerable)
            )
            {
                return false;
            }


            foreach (
                object entry
                in enumerable
            )
            {
                result.Add(
                    entry
                );
            }


            return true;
        }


        private static int TryReadScalarInt(
            object value,
            int fallback
        )
        {
            if (
                value ==
                null
            )
            {
                return fallback;
            }


            if (
                value is int intValue
            )
            {
                return intValue;
            }


            if (
                value is long longValue
            )
            {
                if (
                    longValue <
                    int.MinValue
                    ||
                    longValue >
                    int.MaxValue
                )
                {
                    return fallback;
                }


                return
                    (int)longValue;
            }


            if (
                value is short shortValue
            )
            {
                return shortValue;
            }


            if (
                value is byte byteValue
            )
            {
                return byteValue;
            }


            if (
                int.TryParse(
                    value.ToString(),
                    out int parsed
                )
            )
            {
                return parsed;
            }


            return fallback;
        }


        private void EnsureSlotsCreated()
        {
            if (
                slots !=
                null
                &&
                slots.Length ==
                SlotCount
            )
            {
                for (
                    int i = 0;
                    i < slots.Length;
                    i++
                )
                {
                    if (
                        slots[i] ==
                        null
                    )
                    {
                        slots[i] =
                            new ItemStack();
                    }
                }


                return;
            }


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
        }


        private static object ExtractCollection(
            object saveData
        )
        {
            if (
                saveData ==
                null
            )
            {
                return null;
            }


            if (
                saveData is string
            )
            {
                return null;
            }


            if (
                saveData is IEnumerable
            )
            {
                return saveData;
            }


            string[] candidates =
            {
                "Slots",
                "Inventory",
                "Entries",
                "Items"
            };


            for (
                int i = 0;
                i < candidates.Length;
                i++
            )
            {
                object value =
                    ReadMember(
                        saveData,
                        candidates[i]
                    );


                if (
                    value is IEnumerable
                    &&
                    !(value is string)
                )
                {
                    return value;
                }
            }


            return null;
        }


        private static object ReadMember(
            object instance,
            string name
        )
        {
            if (
                instance ==
                null
            )
            {
                return null;
            }


            Type type =
                instance.GetType();


            FieldInfo field =
                type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.IgnoreCase
                );


            if (
                field !=
                null
            )
            {
                return
                    field.GetValue(
                        instance
                    );
            }


            PropertyInfo property =
                type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.IgnoreCase
                );


            if (
                property !=
                null
                &&
                property.CanRead
            )
            {
                return
                    property.GetValue(
                        instance,
                        null
                    );
            }


            return null;
        }


        private static int ReadIntMember(
            object instance,
            int fallback,
            params string[] names
        )
        {
            for (
                int i = 0;
                i < names.Length;
                i++
            )
            {
                object value =
                    ReadMember(
                        instance,
                        names[i]
                    );


                if (
                    value is int intValue
                )
                {
                    return intValue;
                }


                if (
                    value !=
                    null
                    &&
                    int.TryParse(
                        value.ToString(),
                        out int parsed
                    )
                )
                {
                    return parsed;
                }
            }


            return fallback;
        }


        private static string ReadStringMember(
            object instance,
            params string[] names
        )
        {
            for (
                int i = 0;
                i < names.Length;
                i++
            )
            {
                object value =
                    ReadMember(
                        instance,
                        names[i]
                    );


                if (
                    value !=
                    null
                )
                {
                    string result =
                        value.ToString();


                    if (
                        !string.IsNullOrWhiteSpace(
                            result
                        )
                    )
                    {
                        return result;
                    }
                }
            }


            return "";
        }



        public void NotifyExternalChange() => NotifyChanged();

        private bool TryGetItemDefinition(string itemId, out ItemDefinition item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId)) return false;
            return ItemRegistry.TryGet(itemId, out item);
        }

        private int GetMaxStack(string itemId)
        {
            if (!TryGetItemDefinition(itemId, out ItemDefinition item)) return 100;
            return GetMaxStack(item);
        }

        private int GetMaxStack(ItemDefinition item)
        {
            if (item == null) return 100;
            if (GetDefinitionDurability(item) > 0) return 1;
            return Mathf.Max(1, item.GetMaxStack());
        }


        private static int GetDefinitionDurability(
            ItemDefinition item
        )
        {
            if (
                item ==
                null
            )
            {
                return 0;
            }


            // First use the compatibility metadata layer.
            string id =
                ReadStringMember(
                    item,
                    "ID",
                    "Id"
                );


            if (
                !string.IsNullOrWhiteSpace(
                    id
                )
            )
            {
                int catalogValue =
                    ItemDurabilityMetadata
                        .GetDurability(
                            id
                        );


                if (
                    catalogValue >
                    0
                )
                {
                    return catalogValue;
                }
            }


            return
                ReadIntMember(
                    item,
                    0,
                    "Durability"
                );
        }


        private Vector2 GetDropPosition()
        {
            Transform origin = dropOrigin != null ? dropOrigin : transform;
            return (Vector2)origin.position + Vector2.up * 0.25f;
        }

        private void NotifyChanged() => Changed?.Invoke();
    }
}
