
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

        // Optional nested JSON object for furniture occupying more than one cell.
        // Example: "MultiBlock": { "Width": 2, "Height": 1, "AnchorX": 0, "AnchorY": 0, "Kind": "bed" }
        public BlockMultiBlockDefinition MultiBlock;


        // =====================================================
        // ANIMATED / STATE-BASED VISUALS
        // =====================================================
        //
        // Example:
        // "Animation": {
        //   "Frames": ["torch_0", "torch_1", "torch_2", "torch_3"],
        //   "FPS": 8,
        //   "PingPong": false,
        //   "RandomStart": true
        // }
        //
        // State example:
        // "States": [
        //   { "State": "burning", "Texture": "furnace_burning" }
        // ]
        //
        // A state entry may also contain its own Animation object.
        // =====================================================

        public BlockAnimationDefinition Animation;

        public List<BlockVisualStateDefinition> States =
            new List<BlockVisualStateDefinition>();


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

    
        // [BT-AUTO-COLLISION-FIELDS]
        // Normalized rectangle inside one tile (0..1).
        public bool UseCustomCollision = false;
        public float CollisionOffsetX = 0.5f;
        public float CollisionOffsetY = 0.5f;
        public float CollisionWidth = 1f;
        public float CollisionHeight = 1f;
}


    [Serializable]
    public class BlockMultiBlockDefinition
    {
        public int Width = 1;
        public int Height = 1;
        public int AnchorX = 0;
        public int AnchorY = 0;
        public string Kind;

        // Full-size visual used by furniture-style multi-blocks.
        // File: Assets/GameData/ResourcePacks/Default/textures/multiblocks/<Texture>.png
        // Recommended dimensions: Width*16 x Height*16 pixels.
        public string Texture;

        // If true, every bottom cell of the multi-block must have a
        // foreground block directly below it when placed. Useful for beds.
        public bool RequireFloor = false;
    }


    [Serializable]
    public class BlockAnimationDefinition
    {
        public List<string> Frames =
            new List<string>();

        public float FPS = 6f;

        public bool PingPong = false;

        public bool RandomStart = false;
    }


    [Serializable]
    public class BlockVisualStateDefinition
    {
        public string State;

        // Static texture for this state. If Animation is also configured,
        // animation frames take priority and this is the fallback texture.
        public string Texture;

        public BlockAnimationDefinition Animation;
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
