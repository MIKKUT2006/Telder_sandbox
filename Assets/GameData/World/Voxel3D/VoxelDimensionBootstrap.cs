using UnityEngine;
using Game.World.Dimensions;

namespace Game.World.Voxel3D
{
    public static class VoxelDimensionBootstrap
    {
        public static bool TryStart3D(GameObject host,Transform player)
        {
            DimensionDefinition d=DimensionTravelRuntime.Current;if(d==null||!d.Is3D)return false;
            VoxelWorld3DRuntime rt=host.GetComponent<VoxelWorld3DRuntime>();if(rt==null)rt=host.AddComponent<VoxelWorld3DRuntime>();rt.Initialize(player);Debug.Log("DIMENSION: started 3D voxel runtime for "+d.Name);return true;
        }
    }
}
