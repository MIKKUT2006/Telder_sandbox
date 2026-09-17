using Game.Content;
using System;
using UnityEngine;

namespace Game.Items
{
    [Serializable]
    public class ItemDefinition
    {
        // Основные
        public ContentID ID;
        public string Name;
        public string Texture;
        public ItemType Type;
        public int MaxStack = 100;

        // Для еды
        [Header("Food")]
        public string[] Tags;

        [Min(0f)]
        public float EatTime = 1f;

        public float HungerRestore = 0f;
        public float HealthRestore = 0f;
        public int GetMaxStack()
        {
            return MaxStack > 0 ? MaxStack : 100;
        }
    }
}
