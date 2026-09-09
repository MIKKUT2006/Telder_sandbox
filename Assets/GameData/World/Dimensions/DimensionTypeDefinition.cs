using System;

namespace Game.World.Dimensions
{
    [Serializable]
    public class DimensionTypeDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;

        public DimensionTypeDefinition(
            string id,
            string displayName,
            string description
        )
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
        }
    }
}