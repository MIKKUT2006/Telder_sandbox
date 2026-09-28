using System.Collections.Generic;
using Game.Blocks;

namespace Game.Crafting
{
    public sealed class CraftRecipe
    {
        public string ResultItemId;
        public string Name;
        public int ResultCount = 1;
        public bool CraftWithoutWorkbench;
        public List<CraftIngredientDefinition> Ingredients =
            new List<CraftIngredientDefinition>();
    }
}
