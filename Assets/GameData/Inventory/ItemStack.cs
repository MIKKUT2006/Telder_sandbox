using System;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private string itemId = "";
        [SerializeField] private int count = 0;
        [SerializeField] private int currentDurability = -1;
        [SerializeField] private int bonusMaxDurability = 0;
        [SerializeField] private string installedUpgradeId = "";

        public string ItemId => itemId;

        public int Count
        {
            get => count;
            set
            {
                count = Mathf.Max(0, value);
                if (count <= 0) Clear();
            }
        }

        public int CurrentDurability
        {
            get => currentDurability;
            set => currentDurability = value;
        }

        public int BonusMaxDurability
        {
            get => bonusMaxDurability;
            set => bonusMaxDurability = Mathf.Max(0, value);
        }

        public string InstalledUpgradeId
        {
            get => installedUpgradeId;
            set => installedUpgradeId = value ?? "";
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(itemId) || count <= 0;

        public bool HasInstanceData =>
            currentDurability >= 0 ||
            bonusMaxDurability > 0 ||
            !string.IsNullOrWhiteSpace(installedUpgradeId);

        public void Set(string newItemId, int newCount)
        {
            itemId = newItemId ?? "";
            count = Mathf.Max(0, newCount);
            currentDurability = -1;
            bonusMaxDurability = 0;
            installedUpgradeId = "";

            if (count <= 0 || string.IsNullOrWhiteSpace(itemId))
                Clear();
        }

        public void Set(ItemStack other)
        {
            if (other == null || other.IsEmpty)
            {
                Clear();
                return;
            }

            itemId = other.itemId;
            count = other.count;
            currentDurability = other.currentDurability;
            bonusMaxDurability = other.bonusMaxDurability;
            installedUpgradeId = other.installedUpgradeId;
        }

        public ItemStack Clone()
        {
            ItemStack clone = new ItemStack();
            clone.Set(this);
            return clone;
        }

        public void Clear()
        {
            itemId = "";
            count = 0;
            currentDurability = -1;
            bonusMaxDurability = 0;
            installedUpgradeId = "";
        }
    }
}
