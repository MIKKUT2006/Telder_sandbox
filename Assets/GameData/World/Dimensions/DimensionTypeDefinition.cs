
using System;

namespace Game.World.Dimensions
{
    public enum DimensionGeometryMode
    {
        TwoD = 0,
        Voxel3D = 1
    }

    [Serializable]
    public class DimensionTypeDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public DimensionGeometryMode GeometryMode;

        // ћинимальный уровень портальной пушки дл€ доступа.
        public int Tier;

        public bool Is3D =>
            GeometryMode == DimensionGeometryMode.Voxel3D;

        public DimensionTypeDefinition(
            string id,
            string displayName,
            string description,
            DimensionGeometryMode geometryMode = DimensionGeometryMode.TwoD,
            int tier = 1)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            GeometryMode = geometryMode;
            Tier = Math.Max(1, tier);
        }
    }
}
