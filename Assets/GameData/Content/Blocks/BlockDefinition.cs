
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
        // VISUAL / FURNITURE
        // =====================================================

        // Used by the separate furniture SpriteRenderer layer.
        // This makes 17x17, narrow jars, ladders and overhanging sprites possible.
        public float VisualOffsetX = 0f;
        public float VisualOffsetY = 0f;
        public float VisualScale = 1f;


        // =====================================================
        // COLLISION SHAPE
        // =====================================================
        //
        // Friendly JSON string:
        //
        // "CollisionShape": "Full"
        // "CollisionShape": "HalfBottom"
        // "CollisionShape": "HalfTop"
        // "CollisionShape": "StairUpRight"
        // "CollisionShape": "StairUpLeft"
        // "CollisionShape": "Custom"
        // "CollisionShape": "None"
        //
        // Existing solid blocks that do not have this field
        // continue to behave as Full blocks.
        //
        // Custom rectangles are local to the 1x1 block cell.
        // X/Y/Width/Height use normalized block units.
        //
        // =====================================================

        public string CollisionShape =
            "Full";


        public List<BlockCollisionRectDefinition>
            CollisionRects =
            new List<BlockCollisionRectDefinition>();


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


            CollisionShape =
                "Full";


            CollisionRects =
                new List<
                    BlockCollisionRectDefinition
                >();


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
    public class BlockCollisionRectDefinition
    {

        public float X =
            0f;


        public float Y =
            0f;


        public float Width =
            1f;


        public float Height =
            1f;

    }


    [Serializable]
    public class CraftIngredientDefinition
    {

        public string ItemId;


        public int Count =
            1;

    }

}
