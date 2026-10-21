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
        // Optional tooltip description. Localized text can override this via language JSON.
        public string Description;
        public string Texture;
        public ItemType Type;
        public int MaxStack = 100;
        [Header("Tool")]
        [Min(1)]
        public int ToolLevel = 1;

        [Header("Held in hand")]
        public float HeldScale = 1f;
        public float HeldRotation = 0f;
        public float HeldOffsetX = 0f;
        public float HeldOffsetY = 0f;

        [Header("Food")]
        public string[] Tags;

        // Used while selected in the hotbar when Tags contains "held_light".
        // Block items can inherit these values from their block definition.
        public byte LightEmissionR;
        public byte LightEmissionG;
        public byte LightEmissionB;

        [Min(0f)]
        public float EatTime = 1f;

        public Game.Gameplay.Effects.FoodEffectDefinition[] Effects;
        public float HungerRestore = 0f;
        public float HealthRestore = 0f;

        [Header("Furnace")]
        [Min(0f)] public float FuelBurnTime = 0f;
        public string SmeltResult;
        public int SmeltResultCount = 1;
        [Min(0.05f)] public float SmeltTime = 5f;

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
