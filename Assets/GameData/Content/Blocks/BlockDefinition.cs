
using System;
using System.Collections.Generic;

using Game.Content;


namespace Game.Blocks
{

    [Serializable]
    public class BlockDefinition
    {

        public string ID;

        public string Name;

        public string Texture;


        public float Hardness;

        public float ExplosionResistance;


        public bool Solid;

        public bool Transparent;

        public bool BlocksLight;


        public byte LightOpacity;

        public byte LightEmissionR;

        public byte LightEmissionG;

        public byte LightEmissionB;


        public BlockMaterial Material;


        public ContentID Drop;

        public int DropCount;


        public string PlaceSound;

        public string BreakSound;

        public string RequiredTool;


        public List<string> Tags;


        // =====================================================
        // CHEST
        // =====================================================

        // Exact JSON field:
        // "closed": true / false
        public bool closed;


        public bool Closed
        {
            get
            {
                return closed;
            }

            set
            {
                closed =
                    value;
            }
        }


        // =====================================================
        // CRAFTING
        // =====================================================

        public List<CraftIngredientDefinition>
            CraftIngredients;


        public bool CraftWithoutWorkbench;


        public int CraftResultCount;


        public BlockDefinition()
        {

            Hardness =
                1f;


            ExplosionResistance =
                1f;


            Solid =
                true;


            Transparent =
                false;


            BlocksLight =
                true;


            LightOpacity =
                15;


            LightEmissionR =
                0;


            LightEmissionG =
                0;


            LightEmissionB =
                0;


            DropCount =
                1;


            Tags =
                new List<string>();


            closed =
                false;


            CraftIngredients =
                new List<
                    CraftIngredientDefinition
                >();


            CraftWithoutWorkbench =
                false;


            CraftResultCount =
                1;

        }

    }


    [Serializable]
    public class CraftIngredientDefinition
    {

        public string ItemId;


        public int Count =
            1;

    }

}
