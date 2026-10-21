using System.Collections.Generic;
using UnityEngine;

namespace Game.World.Voxel3D
{
    public sealed class VoxelWorld3D
    {
        private readonly Dictionary<Vector3Int,VoxelChunk3D> chunks = new Dictionary<Vector3Int,VoxelChunk3D>();
        public IEnumerable<KeyValuePair<Vector3Int,VoxelChunk3D>> Chunks => chunks;

        public bool TryGetChunk(Vector3Int c, out VoxelChunk3D chunk) => chunks.TryGetValue(c,out chunk);
        public void SetChunk(VoxelChunk3D chunk) { if(chunk!=null) chunks[chunk.Coordinate]=chunk; }
        public bool RemoveChunk(Vector3Int c) => chunks.Remove(c);

        public ushort GetBlock(int x,int y,int z)
        {
            Vector3Int cc = WorldToChunk(x,y,z); VoxelChunk3D c;
            if(!chunks.TryGetValue(cc,out c)) return 0;
            Vector3Int l=WorldToLocal(x,y,z); return c.Get(l.x,l.y,l.z);
        }

        public void SetBlock(int x,int y,int z,ushort id)
        {
            Vector3Int cc=WorldToChunk(x,y,z); VoxelChunk3D c;
            if(!chunks.TryGetValue(cc,out c)) return;
            Vector3Int l=WorldToLocal(x,y,z); c.Set(l.x,l.y,l.z,id);
        }

        public static Vector3Int WorldToChunk(int x,int y,int z) => new Vector3Int(FloorDiv(x,16),FloorDiv(y,16),FloorDiv(z,16));
        public static Vector3Int WorldToLocal(int x,int y,int z) => new Vector3Int(Mod(x,16),Mod(y,16),Mod(z,16));
        private static int FloorDiv(int v,int d) { int q=v/d; int r=v%d; if(r!=0 && ((r<0)!=(d<0))) q--; return q; }
        private static int Mod(int v,int d) { int m=v%d; return m<0?m+d:m; }
    }
}
