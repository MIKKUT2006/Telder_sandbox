using Game.Blocks;
using Game.Content;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    public static class ItemTagRegistry
    {
        private static readonly Dictionary<string, HashSet<string>> TagToItems =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, HashSet<string>> ItemToTags =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static bool built;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            built = false;
            TagToItems.Clear();
            ItemToTags.Clear();
        }

        public static void EnsureBuilt()
        {
            if (built) return;
            Rebuild();
        }

        public static void Rebuild()
        {
            built = true;
            TagToItems.Clear();
            ItemToTags.Clear();

            // 1) Теги блоков (блоки = предметы в этой игре).
            foreach (BlockDefinition block in BlockRegistry.GetAll())
            {
                if (block == null || string.IsNullOrWhiteSpace(block.ID))
                    continue;

                if (block.Tags == null) continue;

                for (int i = 0; i < block.Tags.Count; i++)
                    AddTag(block.ID, block.Tags[i]);
            }

            // 2) Если у ItemRegistry есть свои определения с тегами —
            //    добавьте сюда аналогичный цикл. Пример:
            //
            // foreach (ItemDefinition item in ItemRegistry.GetAll())
            // {
            //     if (item == null || string.IsNullOrWhiteSpace(item.ID)) continue;
            //     if (item.Tags == null) continue;
            //     for (int i = 0; i < item.Tags.Count; i++)
            //         AddTag(item.ID, item.Tags[i]);
            // }
        }

        private static void AddTag(string itemId, string tag)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (string.IsNullOrWhiteSpace(tag)) return;

            itemId = itemId.Trim();
            tag = tag.Trim();

            if (!TagToItems.TryGetValue(tag, out HashSet<string> items))
            {
                items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                TagToItems[tag] = items;
            }
            items.Add(itemId);

            if (!ItemToTags.TryGetValue(itemId, out HashSet<string> tags))
            {
                tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ItemToTags[itemId] = tags;
            }
            tags.Add(tag);
        }

        public static bool ItemHasTag(string itemId, string tag)
        {
            if (string.IsNullOrWhiteSpace(itemId) ||
                string.IsNullOrWhiteSpace(tag))
                return false;

            EnsureBuilt();

            return ItemToTags.TryGetValue(itemId.Trim(), out HashSet<string> tags)
                && tags.Contains(tag.Trim());
        }

        public static IEnumerable<string> GetItemsWithTag(string tag)
        {
            EnsureBuilt();

            if (string.IsNullOrWhiteSpace(tag))
                return Array.Empty<string>();

            return TagToItems.TryGetValue(tag.Trim(), out HashSet<string> items)
                ? (IEnumerable<string>)items
                : Array.Empty<string>();
        }

        public static bool TagExists(string tag)
        {
            EnsureBuilt();
            return !string.IsNullOrWhiteSpace(tag)
                && TagToItems.ContainsKey(tag.Trim());
        }
    }
}