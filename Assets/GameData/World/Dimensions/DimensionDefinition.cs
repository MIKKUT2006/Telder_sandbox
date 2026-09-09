using System;
using System.Collections.Generic;

namespace Game.World.Dimensions
{
    [Serializable]
    public class DimensionDefinition
    {
        public string Name;
        public int Seed;

        public DimensionTypeDefinition Type;

        public List<DimensionModifierDefinition> Modifiers =
            new List<DimensionModifierDefinition>();


        public DimensionDefinition(
            string name,
            int seed
        )
        {
            Name = name;
            Seed = seed;

            Type =
                DimensionTypeRegistry.GetForSeed(
                    seed
                );

            Modifiers =
                DimensionModifierRegistry.GetForSeed(
                    seed
                );
        }


        public bool HasModifier(
            string modifierId
        )
        {
            for (
                int i = 0;
                i < Modifiers.Count;
                i++
            )
            {
                if (
                    Modifiers[i].Id ==
                    modifierId
                )
                {
                    return true;
                }
            }

            return false;
        }


        public float GravityMultiplier
        {
            get
            {
                float value =
                    1f;

                for (
                    int i = 0;
                    i < Modifiers.Count;
                    i++
                )
                {
                    value *=
                        Modifiers[i]
                            .GravityMultiplier;
                }

                return value;
            }
        }
    }
}