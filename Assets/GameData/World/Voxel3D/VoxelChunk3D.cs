using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelChunk3D
    {
        public const int Size = 16;
        public readonly Vector3Int Coordinate;
        private readonly ushort[,,] blocks = new ushort[Size, Size, Size];

        public VoxelChunk3D(Vector3Int coordinate) { Coordinate = coordinate; }
        public ushort Get(int x, int y, int z) => InBounds(x,y,z) ? blocks[x,y,z] : (ushort)0;
        public void Set(int x, int y, int z, ushort id) { if (InBounds(x,y,z)) blocks[x,y,z] = id; }
        public static bool InBounds(int x,int y,int z) => x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        public Vector3Int WorldOrigin => Coordinate * Size;
    }
}
