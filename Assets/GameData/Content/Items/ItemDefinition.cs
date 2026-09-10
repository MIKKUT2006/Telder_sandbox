using System;
using Game.Content;

namespace Game.Items
{
    [Serializable]
    public class ItemDefinition
    {
        public ContentID ID;
        public string Name;
        public string Texture;
        public ItemType Type;
        public int MaxStack = 100;

        public int GetMaxStack()
        {
            return MaxStack > 0 ? MaxStack : 100;
        }
    }
}
