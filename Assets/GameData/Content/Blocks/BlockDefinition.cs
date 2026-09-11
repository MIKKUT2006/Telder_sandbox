
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

        // Exact JSON field requested by the content format:
        // "closed": true / false
        public bool closed;

        // Code-friendly alias. JsonUtility serializes fields,
        // not this property.
        public bool Closed
        {
            get
            {
                return closed;
            }

            set
            {
                closed = value;
            }
        }

        public BlockDefinition()
        {
            Hardness = 1f;
            ExplosionResistance = 1f;

            Solid = true;
            Transparent = false;
            BlocksLight = true;

            LightOpacity = 15;
            LightEmissionR = 0;
            LightEmissionG = 0;
            LightEmissionB = 0;

            DropCount = 1;
            Tags = new List<string>();

            closed = false;
        }
    }
}
