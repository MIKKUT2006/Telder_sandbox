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


        public float FlowSpeed;


        public float Viscosity;


        public int Damage;


        public bool IsHot;


        public bool IsLiquid;



        public List<string> Tags;



        public FluidDefinition()
        {

            Tags = new List<string>();

            IsLiquid = true;

        }

    }

}