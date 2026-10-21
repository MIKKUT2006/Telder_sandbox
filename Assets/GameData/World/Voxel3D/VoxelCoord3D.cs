using System;
using UnityEngine;

namespace Game.World.Voxel3D
{
    [Serializable]
    public struct VoxelCoord3D : IEquatable<VoxelCoord3D>
    {
        public int X;
        public int Y;
        public int Z;

        public VoxelCoord3D(int x, int y, int z) { X = x; Y = y; Z = z; }
        public bool Equals(VoxelCoord3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is VoxelCoord3D other && Equals(other);
        public override int GetHashCode() { unchecked { int h = 17; h = h * 31 + X; h = h * 31 + Y; h = h * 31 + Z; return h; } }
        public Vector3Int ToVector3Int() => new Vector3Int(X, Y, Z);
        public static VoxelCoord3D From(Vector3Int v) => new VoxelCoord3D(v.x, v.y, v.z);
    }
}
