using System;
using System.Collections.Generic;
using Game.Content;

namespace Game.Fluids
{
    [Serializable]
    public class FluidDefinition
    {
        public ContentID ID;
        public string Name;
        public string Texture;
        public string Color = "#3D8BFFB8";
        public float FlowSpeed = 3f;
        public float Viscosity = 1f;
        public float Damage = 0f;
        public float DamageInterval = 1f;

        // Minecraft-like horizontal reach from one source/falling column.
        // 8 means up to eight cells away from the feeder.
        public int HorizontalFlowDistance = 8;

        // When a falling column reaches a floor it starts a fresh, secondary
        // horizontal run. This is intentionally separate from source spread.
        public int WaterfallHorizontalFlowDistance = 10;

        // Visual foam/splash particles at waterfall impact points.
        public bool WaterfallFoam = false;

        // RGB block-light emission in the same 0..15 scale used by terrain.
        // Lava uses this to illuminate nearby blocks and liquids.
        public byte LightEmissionR = 0;
        public byte LightEmissionG = 0;
        public byte LightEmissionB = 0;

        public bool IsHot;
        public bool IsLiquid = true;
        public List<string> Tags = new List<string>();
    }
}
