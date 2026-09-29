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
        public float FlowSpeed = 5f;
        public float Viscosity = 1f;
        public float Damage = 0f;
        public float DamageInterval = 1f;
        public bool IsHot;
        public bool IsLiquid = true;
        public List<string> Tags = new List<string>();
    }
}
