using System.Collections.Generic;
using Game.Content;
using UnityEngine;

namespace Game.Items
{
    public static class ItemRegistry
    {
        private static readonly Dictionary<ContentID, ItemDefinition> items =
            new Dictionary<ContentID, ItemDefinition>();

        public static void Register(ItemDefinition item)
        {
            if (item == null)
            {
                Debug.LogWarning("ITEM REGISTRY: Tried to register null item.");
                return;
            }

            if (items.ContainsKey(item.ID))
            {
                Debug.LogWarning("ITEM REGISTRY: Item already registered: " + item.ID);
                return;
            }

            items.Add(item.ID, item);
        }

        public static ItemDefinition Get(ContentID id)
        {
            items.TryGetValue(id, out ItemDefinition item);
            return item;
        }

        public static ItemDefinition Get(string id)
        {
            if (!TryParseID(id, out ContentID contentID))
                return null;

            return Get(contentID);
        }

        public static bool TryGet(ContentID id, out ItemDefinition item)
        {
            return items.TryGetValue(id, out item);
        }

        public static bool TryGet(string id, out ItemDefinition item)
        {
            item = null;

            if (!TryParseID(id, out ContentID contentID))
                return false;

            return TryGet(contentID, out item);
        }

        public static bool Contains(ContentID id)
        {
            return items.ContainsKey(id);
        }

        public static bool Contains(string id)
        {
            return TryParseID(id, out ContentID contentID) && Contains(contentID);
        }

        public static IEnumerable<ItemDefinition> GetAll()
        {
            return items.Values;
        }

        public static int GetMaxStack(ContentID id)
        {
            ItemDefinition item = Get(id);
            return item != null ? item.GetMaxStack() : 100;
        }

        public static int GetMaxStack(string id)
        {
            ItemDefinition item = Get(id);
            return item != null ? item.GetMaxStack() : 100;
        }

        public static void Clear()
        {
            items.Clear();
        }

        private static bool TryParseID(string value, out ContentID id)
        {
            id = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                id = ContentID.Parse(value);
                return true;
            }
            catch
            {
                Debug.LogWarning("ITEM REGISTRY: Invalid ContentID: " + value);
                return false;
            }
        }
    }
}
