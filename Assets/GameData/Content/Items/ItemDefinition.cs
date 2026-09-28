using Game.Blocks;
using Game.Content;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("Food")]
        public string[] Tags;

        [Min(0f)]
        public float EatTime = 1f;

        public float HungerRestore = 0f;
        public float HealthRestore = 0f;

        [Header("Crafting")]
        public List<CraftIngredientDefinition> CraftIngredients =
            new List<CraftIngredientDefinition>();

        public bool CraftWithoutWorkbench = false;
        public int CraftResultCount = 1;

        public int GetMaxStack()
        {
            return MaxStack > 0 ? MaxStack : 100;
        }
    }
}
